namespace FleetPulse.Exceptions
{
    /// <summary>
    /// Thrown for logically invalid dispatch operations that are not the fault of the
    /// runtime (e.g. assigning a busy vehicle, removing a vehicle mid-route, referencing
    /// an unknown route/vehicle/driver ID).
    /// </summary>
    public class InvalidRouteAssignmentException : Exception
    {
        public InvalidRouteAssignmentException() { }

        public InvalidRouteAssignmentException(string message) : base(message) { }

        public InvalidRouteAssignmentException(string message, Exception inner) : base(message, inner) { }
    }
}
