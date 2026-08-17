namespace FleetPulse.Models
{
    /// <summary>A single transport/delivery job between two points, tracked through its lifecycle.</summary>
    public class RouteJob
    {
        private static int _nextId = 1;

        public int RouteId { get; }
        public string Origin { get; }
        public string Destination { get; }
        public double DistanceKm { get; }
        public Priority Priority { get; }
        public RouteStatus Status { get; set; }
        public Vehicle? AssignedVehicle { get; set; }
        public Driver? AssignedDriver { get; set; }

        public RouteJob(string origin, string destination, double distanceKm, Priority priority)
        {
            if (distanceKm <= 0)
                throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance must be positive.");

            RouteId = _nextId++;
            Origin = origin;
            Destination = destination;
            DistanceKm = distanceKm;
            Priority = priority;
            Status = RouteStatus.Pending;
        }

        public override string ToString()
        {
            string vehiclePart = AssignedVehicle != null ? $" Vehicle#{AssignedVehicle.VehicleId}" : "";
            string driverPart = AssignedDriver != null ? $" Driver#{AssignedDriver.DriverId}" : "";
            return $"Route#{RouteId}  {Origin} -> {Destination}  ({DistanceKm:F0}km)  " +
                   $"Priority:{Priority}  Status:{Status}{vehiclePart}{driverPart}";
        }
    }
}
