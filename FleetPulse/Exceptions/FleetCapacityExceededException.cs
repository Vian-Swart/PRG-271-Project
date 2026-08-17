namespace FleetPulse.Exceptions
{
    /// <summary>
    /// Thrown when an operation would exceed a defined fleet-wide capacity constraint,
    /// e.g. too many routes active at once for the dispatch centre to safely track.
    /// </summary>
    public class FleetCapacityExceededException : Exception
    {
        public FleetCapacityExceededException() { }

        public FleetCapacityExceededException(string message) : base(message) { }

        public FleetCapacityExceededException(string message, Exception inner) : base(message, inner) { }
    }
}
