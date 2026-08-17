namespace FleetPulse.Models
{
    /// <summary>An immutable log entry recording a completed maintenance action on a vehicle.</summary>
    public class MaintenanceRecord
    {
        public int VehicleId { get; }
        public DateTime Date { get; }
        public string Description { get; }
        public decimal Cost { get; }

        public MaintenanceRecord(int vehicleId, string description, decimal cost)
        {
            VehicleId = vehicleId;
            Date = DateTime.Now;
            Description = string.IsNullOrWhiteSpace(description) ? "General service" : description;
            Cost = cost;
        }

        public override string ToString() =>
            $"{Date:yyyy-MM-dd HH:mm}  Vehicle#{VehicleId}: {Description}  (Cost: R{Cost:F2})";
    }
}
