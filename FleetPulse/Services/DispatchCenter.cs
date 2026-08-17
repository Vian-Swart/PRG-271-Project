using FleetPulse.Exceptions;
using FleetPulse.Models;

namespace FleetPulse.Services
{
    // Custom delegate types (explicit, rather than relying only on built-in Action<T>)
    // to satisfy the "use delegates to notify subscribers" requirement clearly.
    public delegate void VehicleEventHandler(Vehicle vehicle, string message);
    public delegate void RouteEventHandler(RouteJob route);

    /// <summary>
    /// The central orchestrator: owns the fleet, drivers and routes, enforces domain
    /// rules, raises events on meaningful state changes, and runs the background
    /// monitoring thread that simulates real-time fleet activity.
    /// </summary>
    public class DispatchCenter
    {
        private readonly List<Vehicle> _fleet = new();
        private readonly List<Driver> _drivers = new();
        private readonly List<RouteJob> _routes = new();
        private readonly List<MaintenanceRecord> _maintenanceLog = new();

        // Guards every mutation of the four collections above so the background
        // monitoring thread and the main (user input) thread never corrupt shared state.
        private readonly object _lock = new();

        private CancellationTokenSource? _cts;
        private Task? _monitorTask;

        /// <summary>Domain rule: the dispatch centre will not track more than this many active routes at once.</summary>
        public const int MaxConcurrentActiveRoutes = 10;

        // ---- Events (publisher side of the publisher-subscriber model) ----
        public event VehicleEventHandler? BreakdownDetected;
        public event VehicleEventHandler? FuelLowWarning;
        public event VehicleEventHandler? MaintenanceDue;
        public event RouteEventHandler? DeliveryCompleted;

        public IReadOnlyList<Vehicle> Fleet { get { lock (_lock) { return _fleet.ToList(); } } }
        public IReadOnlyList<Driver> Drivers { get { lock (_lock) { return _drivers.ToList(); } } }
        public IReadOnlyList<RouteJob> Routes { get { lock (_lock) { return _routes.ToList(); } } }
        public IReadOnlyList<MaintenanceRecord> MaintenanceLog { get { lock (_lock) { return _maintenanceLog.ToList(); } } }

        public bool IsMonitoring => _monitorTask != null && !_monitorTask.IsCompleted;

        public void AddVehicle(Vehicle vehicle)
        {
            lock (_lock) { _fleet.Add(vehicle); }
        }

        public bool RemoveVehicle(int vehicleId)
        {
            lock (_lock)
            {
                var v = _fleet.FirstOrDefault(x => x.VehicleId == vehicleId);
                if (v == null) return false;

                if (v.Status == VehicleStatus.EnRoute)
                    throw new InvalidRouteAssignmentException(
                        $"Cannot remove Vehicle#{vehicleId}: it is currently en route.");

                return _fleet.Remove(v);
            }
        }

        public void AddDriver(Driver driver)
        {
            lock (_lock) { _drivers.Add(driver); }
        }

        public RouteJob CreateRoute(string origin, string destination, double distanceKm, Priority priority)
        {
            lock (_lock)
            {
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
        /// Assigns a vehicle and driver to a pending route, enforcing two domain-specific
        /// rules: a vehicle overdue for service cannot be dispatched, and a driver cannot
        /// be scheduled past their daily hour limit.
        /// </summary>
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

                route.AssignedVehicle = vehicle;
                route.AssignedDriver = driver;
                route.Status = RouteStatus.InProgress;
                vehicle.Status = VehicleStatus.EnRoute;
                driver.LogHours(estimatedHours);
            }
        }

        /// <summary>Marks a route complete, applies the trip to the vehicle, and raises DeliveryCompleted.</summary>
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

        /// <summary>Starts the background task that simulates live fleet activity independently of user input.</summary>
        public void StartMonitoring()
        {
            if (IsMonitoring) return;
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cts.Token), _cts.Token);
        }

        public void StopMonitoring() => _cts?.Cancel();

        private void MonitorLoop(CancellationToken token)
        {
            var rng = new Random();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(3000);
                    if (token.IsCancellationRequested) break;

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

        /// <summary>Replaces current fleet/driver state with a previously saved snapshot (see FileManager).</summary>
        public void LoadFromSnapshot(FleetStateSnapshot snapshot)
        {
            lock (_lock)
            {
                _fleet.Clear();
                _drivers.Clear();

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
