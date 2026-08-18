using System;

namespace FleetPulse.Models
{
    /// <summary>
    /// Represents a single transport or delivery job between two locations, tracked through its full lifecycle.
    /// Manages route metadata, priority levels, status updates, and assigned fleet assets.
    /// </summary>
    public class RouteJob
    {
        /// <summary>
        /// Static auto-incrementing sequence counter for generating unique route job IDs across the application domain.
        /// </summary>
        private static int _nextId = 1;

        /// <summary>Gets the unique identifier for this route job.</summary>
        public int RouteId { get; }

        /// <summary>Gets the starting location or address.</summary>
        public string Origin { get; }

        /// <summary>Gets the target destination location or address.</summary>
        public string Destination { get; }

        /// <summary>Gets the total trip distance in kilometers.</summary>
        public double DistanceKm { get; }

        /// <summary>Gets the priority rating assigned to the route job.</summary>
        public Priority Priority { get; }

        /// <summary>Gets or sets the current lifecycle status of the route job.</summary>
        public RouteStatus Status { get; set; }

        /// <summary>Gets or sets the vehicle assigned to carry out this route job, or <c>null</c> if unassigned.</summary>
        public Vehicle? AssignedVehicle { get; set; }

        /// <summary>Gets or sets the driver assigned to execute this route job, or <c>null</c> if unassigned.</summary>
        public Driver? AssignedDriver { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteJob"/> class with route endpoints, distance, and priority rating.
        /// Assigns an auto-incremented <see cref="RouteId"/> and sets the initial state to <see cref="RouteStatus.Pending"/>.
        /// </summary>
        /// <param name="origin">The starting location address or city name.</param>
        /// <param name="destination">The target destination address or city name.</param>
        /// <param name="distanceKm">The total route distance in kilometers. Must be greater than zero.</param>
        /// <param name="priority">The urgency level assigned to the job.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="distanceKm"/> is less than or equal to zero.</exception>
        public RouteJob(string origin, string destination, double distanceKm, Priority priority)
        {
            if (distanceKm <= 0)
                throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance must be positive.");

            // Auto-increment and assign domain ID
            RouteId = _nextId++;
            Origin = origin;
            Destination = destination;
            DistanceKm = distanceKm;
            Priority = priority;

            // Route jobs default to Pending until assigned via DispatchCenter
            Status = RouteStatus.Pending;
        }

        /// <summary>
        /// Generates a formatted summary string detailing the route job ID, endpoints, distance, priority, status, and active asset assignments.
        /// </summary>
        /// <returns>A formatted summary string representing the current state of the route job.</returns>
        public override string ToString()
        {
            // Append asset assignment metadata when present
            string vehiclePart = AssignedVehicle != null ? $" Vehicle#{AssignedVehicle.VehicleId}" : "";
            string driverPart = AssignedDriver != null ? $" Driver#{AssignedDriver.DriverId}" : "";

            return $"Route#{RouteId}  {Origin} -> {Destination}  ({DistanceKm:F0}km)  " +
                   $"Priority:{Priority}  Status:{Status}{vehiclePart}{driverPart}";
        }
    }
}