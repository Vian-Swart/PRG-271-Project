namespace FleetPulse.Interfaces
{
    /// <summary>
    /// Behaviour contract for any entity that requires scheduled servicing.
    /// Implemented by Vehicle so DispatchCenter can check service due-ness
    /// through the interface rather than a concrete type.
    /// </summary>
    public interface IMaintainable
    {
        DateTime? LastServiceDate { get; }
        bool IsDueForService();
        void ScheduleMaintenance();
    }
}
