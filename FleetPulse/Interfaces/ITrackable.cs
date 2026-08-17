namespace FleetPulse.Interfaces
{
    /// <summary>
    /// Behaviour contract for any entity whose real-time position matters to dispatch
    /// (currently: vehicles). Kept separate from IMaintainable so a future entity type
    /// (e.g. a mobile depot) could implement location tracking without inheriting
    /// maintenance obligations.
    /// </summary>
    public interface ITrackable
    {
        (double Lat, double Lon) GetCurrentLocation();
        void UpdateLocation(double lat, double lon);
    }
}
