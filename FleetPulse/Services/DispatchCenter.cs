using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FleetPulse.Exceptions;
using FleetPulse.Models;

namespace FleetPulse.Services
{
    /// <summary>
    /// Represents a delegate for handling vehicle-related domain events.
    /// </summary>
    /// <param name="vehicle">The vehicle instance triggering the event.</param>
    /// <param name="message">A descriptive notification message explaining the event.</param>
    public delegate void VehicleEventHandler(Vehicle vehicle, string message);

    /// <summary>
    /// Represents a delegate for handling route execution events.
    /// </summary>
    /// <param name="route">The route job entity associated with the event.</param>
    public delegate void RouteEventHandler(RouteJob route);

    /// <summary>
    /// The central orchestrator: owns the fleet, drivers, and routes, enforces domain
    /// rules, raises events on meaningful state changes, and runs the background
    /// monitoring thread that simulates real-time fleet activity.
    /// </summary>
    public class DispatchCenter
    {
        private readonly List<Vehicle> _fleet = new();
        private readonly List<Driver> _drivers = new();
        private readonly List<RouteJob> _routes = new();
        private readonly List<MaintenanceRecord> _maintenanceLog = new();

        /// <summary>
        /// Guards every mutation of the underlying collections so the background
        /// monitoring thread and the main thread never corrupt shared state.
        /// </summary>
        private readonly object _lock = new();

        private CancellationTokenSource? _cts;
        private Task? _monitorTask;

        /// <summary>Domain rule: the dispatch centre will not track more than this many active routes at once.</summary>
        public const int MaxConcurrentActiveRoutes = 10;

        // ---- Events (publisher side of the publisher-subscriber model) ----

        /// <summary>Occurs when an in-transit breakdown is detected during background monitoring.</summary>
        public event VehicleEventHandler? BreakdownDetected;

        /// <summary>Occurs when a vehicle's fuel level drops to or below the low fuel threshold.</summary>
        public event VehicleEventHandler? FuelLowWarning;

        /// <summary>Occurs when a vehicle reaches or exceeds its maintenance mileage threshold.</summary>
        public event VehicleEventHandler? MaintenanceDue;

        /// <summary>Occurs when a route job is successfully completed and processed.</summary>
        public event RouteEventHandler? DeliveryCompleted;

        /// <summary>Gets a thread-safe snapshot copy of the tracked fleet vehicles.</summary>
        public IReadOnlyList<Vehicle> Fleet { get { lock (_lock) { return _fleet.ToList(); } } }

        /// <summary>Gets a thread-safe snapshot copy of all registered drivers.</summary>
        public IReadOnlyList<Driver> Drivers { get { lock (_lock) { return _drivers.ToList(); } } }

        /// <summary>Gets a thread-safe snapshot copy of all tracked route jobs.</summary>
        public IReadOnlyList<RouteJob> Routes { get { lock (_lock) { return _routes.ToList(); } } }

        /// <summary>Gets a thread-safe snapshot copy of all recorded maintenance entries.</summary>
        public IReadOnlyList<MaintenanceRecord> MaintenanceLog { get { lock (_lock) { return _maintenanceLog.ToList(); } } }

        /// <summary>Gets a value indicating whether the background monitoring task is actively running.</summary>
        public bool IsMonitoring => _monitorTask != null && !_monitorTask.IsCompleted;

        /// <summary>
        /// Adds a vehicle to the central fleet in a thread-safe manner.
        /// </summary>
        /// <param name="vehicle">The vehicle instance to add to the fleet.</param>
        public void AddVehicle(Vehicle vehicle)
        {
            lock (_lock) { _fleet.Add(vehicle); }
        }

        /// <summary>
        /// Removes a vehicle from the fleet by ID if it is not currently en route.
        /// </summary>
        /// <param name="vehicleId">The unique ID of the vehicle to remove.</param>
        /// <returns><c>true</c> if the vehicle was successfully removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidRouteAssignmentException">Thrown if the target vehicle is active on a route.</exception>
        public bool RemoveVehicle(int vehicleId)
        {
            lock (_lock)
            {
                var v = _fleet.FirstOrDefault(x => x.VehicleId == vehicleId);
                if (v == null) return false;

                // Enforce domain restriction: active vehicles cannot be decommissioned mid-route
                if (v.Status == VehicleStatus.EnRoute)
                    throw new InvalidRouteAssignmentException(
                        $"Cannot remove Vehicle#{vehicleId}: it is currently en route.");

                return _fleet.Remove(v);
            }
        }

        /// <summary>
        /// Adds a driver to the dispatch roster in a thread-safe manner.
        /// </summary>
        /// <param name="driver">The driver entity to register.</param>
        public void AddDriver(Driver driver)
        {
            lock (_lock) { _drivers.Add(driver); }
        }

        /// <summary>
        /// Creates a new route job if system capacity allows.
        /// </summary>
        /// <param name="origin">Starting point address or city.</param>
        /// <param name="destination">Target destination address or city.</param>
        /// <param name="distanceKm">Distance of the trip in kilometers.</param>
        /// <param name="priority">Priority rating assigned to the job.</param>
        /// <returns>The generated <see cref="RouteJob"/> instance.</returns>
        /// <exception cref="FleetCapacityExceededException">Thrown when max concurrent active routes is reached.</exception>
        public RouteJob CreateRoute(string origin, string destination, double distanceKm, Priority priority)
        {
            lock (_lock)
            {
                // Enforce concurrent active route limit across the dispatch system
                int active = _routes.Count(r => r.Status == RouteStatus.InProgress);
                if (active >= MaxConcurrentActiveRoutes)
                    throw new FleetCapacityExceededException(
                        $"Cannot create route: the {MaxConcurrentActiveRoutes} concurrent active route limit has been reached.");

                var route = new RouteJob(origin, destination, distanceKm, priority);
                _routes.Add(route);
                return route;
            }
        }

        /// <summary>
        /// Assigns a vehicle and driver to a pending route, enforcing vehicle service status and driver daily hour limits.
        /// </summary>
        /// <param name="routeId">The unique ID of the route job.</param>
        /// <param name="vehicleId">The unique ID of the vehicle to assign.</param>
        /// <param name="driverId">The unique ID of the driver to assign.</param>
        /// <exception cref="InvalidRouteAssignmentException">Thrown when route/vehicle state validation fails.</exception>
        /// <exception cref="DriverHourLimitExceededException">Thrown when trip time would push driver past daily limits.</exception>
        public void AssignRoute(int routeId, int vehicleId, int driverId)
        {
            lock (_lock)
            {
                var route = _routes.FirstOrDefault(r => r.RouteId == routeId)
                    ?? throw new InvalidRouteAssignmentException($"Route#{routeId} not found.");
                var vehicle = _fleet.FirstOrDefault(v => v.VehicleId == vehicleId)
                    ?? throw new InvalidRouteAssignmentException($"Vehicle#{vehicleId} not found.");
                var driver = _drivers.FirstOrDefault(d => d.DriverId == driverId)
                    ?? throw new InvalidRouteAssignmentException($"Driver#{driverId} not found.");

                if (route.Status != RouteStatus.Pending)
                    throw new InvalidRouteAssignmentException($"Route#{routeId} is not pending (status: {route.Status}).");

                if (vehicle.Status != VehicleStatus.Idle)
                    throw new InvalidRouteAssignmentException($"Vehicle#{vehicleId} is not idle (status: {vehicle.Status}).");

                // Domain rule 1: overdue-for-service vehicles cannot be dispatched.
                if (vehicle.IsDueForService())
                    throw new InvalidRouteAssignmentException(
                        $"Vehicle#{vehicleId} is overdue for maintenance ({vehicle.Mileage:F0}km) and cannot be dispatched.");

                // Domain rule 2: driver daily hour limit (assumes an average 60km/h for estimation).
                double estimatedHours = route.DistanceKm / 60.0;
                if (driver.HoursDrivenToday + estimatedHours > Driver.MaxDailyHours)
                    throw new DriverHourLimitExceededException(
                        $"Driver#{driverId} would exceed the {Driver.MaxDailyHours:F0}h daily limit " +
                        $"(already at {driver.HoursDrivenToday:F1}h, route needs ~{estimatedHours:F1}h).");

                // Mutate entities to active transit status
                route.AssignedVehicle = vehicle;
                route.AssignedDriver = driver;
                route.Status = RouteStatus.InProgress;
                vehicle.Status = VehicleStatus.EnRoute;
                driver.LogHours(estimatedHours);
            }
        }

        /// <summary>
        /// Marks a route as completed, applies distance updates to the assigned vehicle, and raises <see cref="DeliveryCompleted"/>.
        /// </summary>
        /// <param name="routeId">The unique ID of the completed route.</param>
        public void CompleteRoute(int routeId)
        {
            RouteJob? route;
            lock (_lock)
            {
                route = _routes.FirstOrDefault(r => r.RouteId == routeId);
                if (route == null || route.Status != RouteStatus.InProgress) return;

                route.AssignedVehicle?.Drive(route.DistanceKm);
                if (route.AssignedVehicle != null)
                    route.AssignedVehicle.Status = VehicleStatus.Idle;

                route.Status = RouteStatus.Completed;
            }

            // Raised outside the lock so subscribers can't deadlock against dispatch operations.
            DeliveryCompleted?.Invoke(route);
        }

        /// <summary>
        /// Records a maintenance entry and resets the target vehicle's maintenance tracking flags.
        /// </summary>
        /// <param name="vehicleId">The unique ID of the serviced vehicle.</param>
        /// <param name="description">Summary of maintenance work performed.</param>
        /// <param name="cost">Cost incurred for the maintenance service.</param>
        /// <exception cref="InvalidRouteAssignmentException">Thrown if the target vehicle ID is missing.</exception>
        public void LogMaintenance(int vehicleId, string description, decimal cost)
        {
            lock (_lock)
            {
                var v = _fleet.FirstOrDefault(x => x.VehicleId == vehicleId)
                    ?? throw new InvalidRouteAssignmentException($"Vehicle#{vehicleId} not found.");

                v.ScheduleMaintenance();
                _maintenanceLog.Add(new MaintenanceRecord(vehicleId, description, cost));
            }
        }

        // ---------------- Background monitoring thread ----------------

        /// <summary>
        /// Starts the background task that simulates live fleet activity independently of user input.
        /// </summary>
        public void StartMonitoring()
        {
            if (IsMonitoring) return;
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cts.Token), _cts.Token);
        }

        /// <summary>
        /// Signals cancellation to stop the background fleet monitoring worker task.
        /// </summary>
        public void StopMonitoring() => _cts?.Cancel();

        /// <summary>
        /// Primary worker loop executed on a background thread to simulate live fleet status changes.
        /// </summary>
        /// <param name="token">Cancellation token monitored to gracefully exit the loop.</param>
        private void MonitorLoop(CancellationToken token)
        {
            var rng = new Random();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Simulation pulse interval (3 seconds)
                    Thread.Sleep(3000);
                    if (token.IsCancellationRequested) break;

                    // Snapshot vehicles under lock to prevent iteration modification errors
                    List<Vehicle> snapshot;
                    lock (_lock) { snapshot = _fleet.ToList(); }

                    foreach (var v in snapshot)
                    {
                        if (v.Status == VehicleStatus.EnRoute)
                        {
                            v.AdjustFuel(-rng.Next(1, 4));

                            if (v.FuelLevel <= 15 && v.FuelLevel > 0)
                                FuelLowWarning?.Invoke(v, $"Vehicle#{v.VehicleId} fuel is low: {v.FuelLevel:F1}%.");

                            // Small chance of an in-transit breakdown each tick.
                            if (rng.Next(0, 100) < 3)
                            {
                                lock (_lock) { v.Status = VehicleStatus.Broken; }
                                BreakdownDetected?.Invoke(v, $"Vehicle#{v.VehicleId} has broken down mid-route!");
                            }
                        }

                        if (v.IsDueForService())
                            MaintenanceDue?.Invoke(v, $"Vehicle#{v.VehicleId} is due for scheduled maintenance.");
                    }

                    // Occasionally auto-complete an in-progress route to keep the simulation moving.
                    RouteJob? toComplete = null;
                    lock (_lock)
                    {
                        var inProgress = _routes.Where(r => r.Status == RouteStatus.InProgress).ToList();
                        if (inProgress.Count > 0 && rng.Next(0, 100) < 40)
                            toComplete = inProgress[rng.Next(inProgress.Count)];
                    }
                    if (toComplete != null) CompleteRoute(toComplete.RouteId);
                }
            }
            catch (Exception ex)
            {
                // The background thread must never crash the app silently.
                Console.WriteLine($"\n[Monitor thread error] {ex.Message}");
            }
        }

        /// <summary>
        /// Replaces current system state in memory with reconstructed entities from a state snapshot object.
        /// </summary>
        /// <param name="snapshot">The source snapshot containing serialized vehicle and driver states.</param>
        /// <exception cref="InvalidDataException">Thrown if an unrecognized vehicle type name is encountered.</exception>
        public void LoadFromSnapshot(FleetStateSnapshot snapshot)
        {
            lock (_lock)
            {
                _fleet.Clear();
                _drivers.Clear();

                // Reconstruct polymorphic vehicle types using pattern matching on type strings
                foreach (var vs in snapshot.Vehicles)
                {
                    Vehicle vehicle = vs.VehicleType switch
                    {
                        "Truck" => new Truck(vs.LicensePlate, vs.ExtraValue),
                        "Van" => new Van(vs.LicensePlate, (int)vs.ExtraValue),
                        "Bus" => new Bus(vs.LicensePlate, (int)vs.ExtraValue),
                        _ => throw new InvalidDataException($"Unknown vehicle type in save file: '{vs.VehicleType}'.")
                    };

                    var status = Enum.TryParse<VehicleStatus>(vs.Status, out var parsed) ? parsed : VehicleStatus.Idle;
                    vehicle.RestoreState(vs.Mileage, vs.FuelLevel, status);
                    _fleet.Add(vehicle);
                }

                // Reconstruct driver domain entities
                foreach (var ds in snapshot.Drivers)
                {
                    var driver = new Driver(ds.Name, ds.LicenseCode);
                    driver.LogHours(ds.HoursDrivenToday);
                    _drivers.Add(driver);
                }
            }
        }
    }
}