using System;

namespace FleetPulse.Models
{
    /// <summary>
    /// An immutable log entry recording a completed maintenance action performed on a fleet vehicle.
    /// Captures timestamps, work descriptions, and financial cost details for auditing and operational tracking.
    /// </summary>
    public class MaintenanceRecord
    {
        /// <summary>Gets the unique identifier of the vehicle that underwent servicing.</summary>
        public int VehicleId { get; }

        /// <summary>Gets the exact date and timestamp when the maintenance service was logged.</summary>
        public DateTime Date { get; }

        /// <summary>Gets the detailed summary description of the work performed during servicing.</summary>
        public string Description { get; }

        /// <summary>Gets the total financial cost incurred for the maintenance service.</summary>
        public decimal Cost { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaintenanceRecord"/> class with vehicle details, service summary, and cost.
        /// Sets the log entry timestamp to the current system date and time (<see cref="DateTime.Now"/>).
        /// </summary>
        /// <param name="vehicleId">The unique ID of the serviced vehicle.</param>
        /// <param name="description">Summary of maintenance work performed. Defaults to "General service" if empty or whitespace.</param>
        /// <param name="cost">The total financial cost incurred for the service.</param>
        public MaintenanceRecord(int vehicleId, string description, decimal cost)
        {
            VehicleId = vehicleId;

            // Automatically stamp record creation time
            Date = DateTime.Now;

            // Fall back to default description if none was specified
            Description = string.IsNullOrWhiteSpace(description) ? "General service" : description;

            Cost = cost;
        }

        /// <summary>
        /// Generates a formatted log string displaying timestamp, vehicle ID, service description, and monetary cost.
        /// </summary>
        /// <returns>A formatted string representation of the maintenance log entry.</returns>
        public override string ToString() =>
            $"{Date:yyyy-MM-dd HH:mm}  Vehicle#{VehicleId}: {Description}  (Cost: R{Cost:F2})";
    }
}