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
        // Internal state storage collections for core domain entities
        private readonly List<Vehicle> _fleet = new();
        private readonly List<Driver> _drivers = new();
        private readonly List<RouteJob> _routes = new();
        private readonly List<MaintenanceRecord> _maintenanceLog = new();

        /// <summary>
        /// Lock object used to synchronize access to internal state collections across threads.
        /// Guards every mutation and read operation to prevent race conditions between main UI operations 
        /// and the background simulation worker.
        /// </summary>
        private readonly object _lock = new();

        // Handles for managing the background monitoring thread lifetime
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

        /// <summary>
        /// Gets a thread-safe snapshot copy of tracked fleet vehicles.
        /// Clones the collection into a new list under lock to prevent collection modification exceptions during external enumeration.
        /// </summary>
        public IReadOnlyList<Vehicle> Fleet { get { lock (_lock) { return _fleet.ToList(); } } }

        /// <summary>
        /// Gets a thread-safe snapshot copy of all registered drivers.
        /// Clones the collection under lock for safe concurrent reading.
        /// </summary>
        public IReadOnlyList<Driver> Drivers { get { lock (_lock) { return _drivers.ToList(); } } }

        /// <summary>
        /// Gets a thread-safe snapshot copy of all tracked route jobs.
        /// Clones the collection under lock to ensure thread safety during UI binding or listing.
        /// </summary>
        public IReadOnlyList<RouteJob> Routes { get { lock (_lock) { return _routes.ToList(); } } }

        /// <summary>
        /// Gets a thread-safe snapshot copy of all recorded maintenance entries.
        /// </summary>
        public IReadOnlyList<MaintenanceRecord> MaintenanceLog { get { lock (_lock) { return _maintenanceLog.ToList(); } } }

        /// <summary>
        /// Gets a value indicating whether the background monitoring worker task is actively running.
        /// </summary>
        public bool IsMonitoring => _monitorTask != null && !_monitorTask.IsCompleted;

        /// <summary>
        /// Adds a vehicle to the central fleet in a thread-safe manner.
        /// </summary>
        /// <param name="vehicle">The vehicle instance to add to the fleet.</param>
        public void AddVehicle(Vehicle vehicle)
        {
            // Lock ensures exclusive write access during collection modification
            lock (_lock) { _fleet.Add(vehicle); }
        }

        /// <summary>
        /// Removes a vehicle from the fleet by ID if it exists and is not currently en route.
        /// </summary>
        /// <param name="vehicleId">The unique ID of the vehicle to remove.</param>
        /// <returns><c>true</c> if the vehicle was successfully removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidRouteAssignmentException">Thrown if the target vehicle is currently active on a route.</exception>
        public bool RemoveVehicle(int vehicleId)
        {
            lock (_lock)
            {
                // Locate target vehicle within the protected collection
                var v = _fleet.FirstOrDefault(x => x.VehicleId == vehicleId);
                if (v == null) return false;

                // Enforce domain restriction: active vehicles cannot be decommissioned mid-route
                if (v.Status == VehicleStatus.EnRoute)
                    throw new InvalidRouteAssignmentException(
                        $"Cannot remove Vehicle#{vehicleId}: it is currently en route.");

                // Perform safe removal from memory list
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
        /// <exception cref="FleetCapacityExceededException">Thrown when max concurrent active routes threshold is reached.</exception>
        public RouteJob CreateRoute(string origin, string destination, double distanceKm, Priority priority)
        {
            lock (_lock)
            {
                // Count current in-progress routes to enforce global system capacity constraints
                int active = _routes.Count(r => r.Status == RouteStatus.InProgress);
                if (active >= MaxConcurrentActiveRoutes)
                    throw new FleetCapacityExceededException(
                        $"Cannot create route: the {MaxConcurrentActiveRoutes} concurrent active route limit has been reached.");

                // Instantiate new domain model and append to route registry
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
        /// <exception cref="InvalidRouteAssignmentException">Thrown when entity lookups or status checks fail.</exception>
        /// <exception cref="DriverHourLimitExceededException">Thrown when projected route duration violates regulatory driving limits.</exception>
        public void AssignRoute(int routeId, int vehicleId, int driverId)
        {
            lock (_lock)
            {
                // Verify all target entities exist in state
                var route = _routes.FirstOrDefault(r => r.RouteId == routeId)
                    ?? throw new InvalidRouteAssignmentException($"Route#{routeId} not found.");
                var vehicle = _fleet.FirstOrDefault(v => v.VehicleId == vehicleId)
                    ?? throw new InvalidRouteAssignmentException($"Vehicle#{vehicleId} not found.");
                var driver = _drivers.FirstOrDefault(d => d.DriverId == driverId)
                    ?? throw new InvalidRouteAssignmentException($"Driver#{driverId} not found.");

                // Validate route current status
                if (route.Status != RouteStatus.Pending)
                    throw new InvalidRouteAssignmentException($"Route#{routeId} is not pending (status: {route.Status}).");

                // Validate vehicle current status
                if (vehicle.Status != VehicleStatus.Idle)
                    throw new InvalidRouteAssignmentException($"Vehicle#{vehicleId} is not idle (status: {vehicle.Status}).");

                // Domain rule 1: overdue-for-service vehicles cannot be dispatched
                if (vehicle.IsDueForService())
                    throw new InvalidRouteAssignmentException(
                        $"Vehicle#{vehicleId} is overdue for maintenance ({vehicle.Mileage:F0}km) and cannot be dispatched.");

                // Domain rule 2: validate driver shift limit based on an assumed average fleet speed of 60 km/h
                double estimatedHours = route.DistanceKm / 60.0;
                if (driver.HoursDrivenToday + estimatedHours > Driver.MaxDailyHours)
                    throw new DriverHourLimitExceededException(
                        $"Driver#{driverId} would exceed the {Driver.MaxDailyHours:F0}h daily limit " +
                        $"(already at {driver.HoursDrivenToday:F1}h, route needs ~{estimatedHours:F1}h).");

                // Update entity properties to reflect active assignment
                route.AssignedVehicle = vehicle;
                route.AssignedDriver = driver;
                route.Status = RouteStatus.InProgress;
                vehicle.Status = VehicleStatus.EnRoute;

                // Record scheduled driving hours against driver's daily log
                driver.LogHours(estimatedHours);
            }
        }

        /// <summary>
        /// Marks an active route complete, updates vehicle mileage/fuel state, and triggers event notifications.
        /// </summary>
        /// <param name="routeId">The unique ID of the completed route.</param>
        public void CompleteRoute(int routeId)
        {
            RouteJob? route;
            lock (_lock)
            {
                // Find matching active route
                route = _routes.FirstOrDefault(r => r.RouteId == routeId);
                if (route == null || route.Status != RouteStatus.InProgress) return;

                // Update vehicle distance traveled and fuel consumption
                route.AssignedVehicle?.Drive(route.DistanceKm);
                
                // Return vehicle to idle dispatch pool
                if (route.AssignedVehicle != null)
                    route.AssignedVehicle.Status = VehicleStatus.Idle;

                // Update route state
                route.Status = RouteStatus.Completed;
            }

            // Raising events outside lock prevents subscribers from causing deadlocks if they call back into DispatchCenter
            DeliveryCompleted?.Invoke(route);
        }

        /// <summary>
        /// Registers a maintenance record for a vehicle and resets its mileage servicing threshold counter.
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

                // Reset service tracking counters on the vehicle domain model
                v.ScheduleMaintenance();

                // Append maintenance entry to audit log
                _maintenanceLog.Add(new MaintenanceRecord(vehicleId, description, cost));
            }
        }

        // ---------------- Background monitoring thread ----------------

        /// <summary>
        /// Starts the background asynchronous monitoring thread that simulates active fleet events.
        /// </summary>
        public void StartMonitoring()
        {
            // Prevent spawning multiple parallel background monitor loops
            if (IsMonitoring) return;

            _cts = new CancellationTokenSource();

            // Run the simulation loop asynchronously on a ThreadPool worker
            _monitorTask = Task.Run(() => MonitorLoop(_cts.Token), _cts.Token);
        }

        /// <summary>
        /// Sends a cancellation signal to gracefully stop the background monitoring loop.
        /// </summary>
        public void StopMonitoring() => _cts?.Cancel();

        /// <summary>
        /// Background worker loop running periodically to simulate real-time vehicle fuel consumption, random failures, and route completions.
        /// </summary>
        /// <param name="token">Cancellation token used to break the worker loop on shutdown.</param>
        private void MonitorLoop(CancellationToken token)
        {
            var rng = new Random();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Delay execution for 3 seconds per simulation pulse
                    Thread.Sleep(3000);
                    if (token.IsCancellationRequested) break;

                    // Safely copy current fleet list to iterate without keeping thread lock held during event invocations
                    List<Vehicle> snapshot;
                    lock (_lock) { snapshot = _fleet.ToList(); }

                    foreach (var v in snapshot)
                    {
                        // Simulate active en-route updates
                        if (v.Status == VehicleStatus.EnRoute)
                        {
                            // Reduce fuel randomly by 1% to 3% each tick
                            v.AdjustFuel(-rng.Next(1, 4));

                            // Raise fuel warning event when fuel hits low reserve threshold
                            if (v.FuelLevel <= 15 && v.FuelLevel > 0)
                                FuelLowWarning?.Invoke(v, $"Vehicle#{v.VehicleId} fuel is low: {v.FuelLevel:F1}%.");

                            // Simulate random breakdown probability (3% chance per pulse tick)
                            if (rng.Next(0, 100) < 3)
                            {
                                lock (_lock) { v.Status = VehicleStatus.Broken; }
                                BreakdownDetected?.Invoke(v, $"Vehicle#{v.VehicleId} has broken down mid-route!");
                            }
                        }

                        // Check maintenance status against mileage limits
                        if (v.IsDueForService())
                            MaintenanceDue?.Invoke(v, $"Vehicle#{v.VehicleId} is due for scheduled maintenance.");
                    }

                    // Randomly select an active route to auto-complete (40% chance per pulse) to advance simulation state
                    RouteJob? toComplete = null;
                    lock (_lock)
                    {
                        var inProgress = _routes.Where(r => r.Status == RouteStatus.InProgress).ToList();
                        if (inProgress.Count > 0 && rng.Next(0, 100) < 40)
                            toComplete = inProgress[rng.Next(inProgress.Count)];
                    }

                    // Complete the randomly selected route outside the lock
                    if (toComplete != null) CompleteRoute(toComplete.RouteId);
                }
            }
            catch (Exception ex)
            {
                // Ensure unexpected exceptions on background thread are logged without unhandled crashes
                Console.WriteLine($"\n[Monitor thread error] {ex.Message}");
            }
        }

        /// <summary>
        /// Restores system state in memory using an imported snapshot object.
        /// Reconstructs polymorphic vehicle sub-types and restores driver historical stats.
        /// </summary>
        /// <param name="snapshot">The source state snapshot container loaded from file storage.</param>
        /// <exception cref="InvalidDataException">Thrown if an unrecognized vehicle type name is encountered in the snapshot data.</exception>
        public void LoadFromSnapshot(FleetStateSnapshot snapshot)
        {
            lock (_lock)
            {
                // Clear existing volatile state prior to restoring from disk snapshot
                _fleet.Clear();
                _drivers.Clear();

                // Reconstruct concrete polymorphic sub-types based on class name stored in snapshot DTO
                foreach (var vs in snapshot.Vehicles)
                {
                    Vehicle vehicle = vs.VehicleType switch
                    {
                        "Truck" => new Truck(vs.LicensePlate, vs.ExtraValue),
                        "Van" => new Van(vs.LicensePlate, (int)vs.ExtraValue),
                        "Bus" => new Bus(vs.LicensePlate, (int)vs.ExtraValue),
                        _ => throw new InvalidDataException($"Unknown vehicle type in save file: '{vs.VehicleType}'.")
                    };

                    // Safely parse enum string or fallback to default state
                    var status = Enum.TryParse<VehicleStatus>(vs.Status, out var parsed) ? parsed : VehicleStatus.Idle;

                    // Restore state properties via internal model method
                    vehicle.RestoreState(vs.Mileage, vs.FuelLevel, status);
                    _fleet.Add(vehicle);
                }

                // Reconstruct driver domain entities and log cumulative shift hours
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