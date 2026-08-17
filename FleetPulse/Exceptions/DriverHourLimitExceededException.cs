namespace FleetPulse.Exceptions
{
    /// <summary>
    /// Domain-specific rule violation: a driver cannot legally/safely be assigned a route
    /// that would push their cumulative driving hours for the day past the allowed limit.
    /// </summary>
    public class DriverHourLimitExceededException : Exception
    {
        public DriverHourLimitExceededException() { }

        public DriverHourLimitExceededException(string message) : base(message) { }

        public DriverHourLimitExceededException(string message, Exception inner) : base(message, inner) { }
    }
}
