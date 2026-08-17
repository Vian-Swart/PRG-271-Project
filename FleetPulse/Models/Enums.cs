namespace FleetPulse.Models
{
    /// <summary>Lifecycle status of a vehicle in the fleet.</summary>
    public enum VehicleStatus
    {
        Idle,
        EnRoute,
        Maintenance,
        Broken
    }

    /// <summary>Lifecycle status of a delivery/transport route job.</summary>
    public enum RouteStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>Business priority of a route job, used for dispatch ordering and reporting.</summary>
    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
