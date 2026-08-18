using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FleetPulse.Models;

namespace FleetPulse.Services
{
    /// <summary>
    /// Data Transfer Object (DTO) representing the persistent state of a <see cref="Vehicle"/>.
    /// Separated from domain models to keep persistence independent of domain validation and private setters.
    /// </summary>
    public class VehicleSnapshot
    {
        /// <summary>Gets or sets the concrete class name of the vehicle (e.g., Truck, Van, Bus).</summary>
        public string VehicleType { get; set; } = "";

        /// <summary>Gets or sets the unique identifier for the vehicle.</summary>
        public int VehicleId { get; set; }

        /// <summary>Gets or sets the vehicle license plate string.</summary>
        public string LicensePlate { get; set; } = "";

        /// <summary>Gets or sets the total odometer reading in kilometers.</summary>
        public double Mileage { get; set; }

        /// <summary>Gets or sets the current fuel level percentage (0 to 100).</summary>
        public double FuelLevel { get; set; }

        /// <summary>Gets or sets the string representation of the <see cref="Enums.VehicleStatus"/> enum.</summary>
        public string Status { get; set; } = "";

        /// <summary>
        /// Gets or sets sub-class specific metric data:
        /// MaxLoadKg for Truck, PackageCapacity for Van, or PassengerCapacity for Bus.
        /// </summary>
        public double ExtraValue { get; set; }
    }

    /// <summary>
    /// Data Transfer Object (DTO) representing the persistent state of a <see cref="Driver"/>.
    /// </summary>
    public class DriverSnapshot
    {
        /// <summary>Gets or sets the unique identifier for the driver.</summary>
        public int DriverId { get; set; }

        /// <summary>Gets or sets the full name of the driver.</summary>
        public string Name { get; set; } = "";

        /// <summary>Gets or sets the driver's license certification code.</summary>
        public string LicenseCode { get; set; } = "";

        /// <summary>Gets or sets the cumulative hours driven today for shift limit enforcement.</summary>
        public double HoursDrivenToday { get; set; }
    }

    /// <summary>
    /// Root Data Transfer Object aggregating the state of all fleet vehicles, drivers, and export metadata.
    /// </summary>
    public class FleetStateSnapshot
    {
        /// <summary>Gets or sets the list of serialized vehicle snapshots.</summary>
        public List<VehicleSnapshot> Vehicles { get; set; } = new();

        /// <summary>Gets or sets the list of serialized driver snapshots.</summary>
        public List<DriverSnapshot> Drivers { get; set; } = new();

        /// <summary>Gets or sets the timestamp when the state snapshot was persisted.</summary>
        public DateTime SavedAt { get; set; }
    }

    /// <summary>
    /// Handles file persistence by saving and loading entire system snapshots to/from JSON storage.
    /// </summary>
    public static class FileManager
    {
        /// <summary>JSON serialization configuration options (formatted with indented JSON spacing).</summary>
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        /// <summary>
        /// Maps domain models (<see cref="Vehicle"/>, <see cref="Driver"/>) to DTO snapshots and writes them to a JSON file on disk.
        /// </summary>
        /// <param name="path">The target file system path for saving the JSON snapshot.</param>
        /// <param name="fleet">Collection of active fleet vehicles to serialize.</param>
        /// <param name="drivers">Collection of active driver entities to serialize.</param>
        public static void SaveState(string path, IEnumerable<Vehicle> fleet, IEnumerable<Driver> drivers)
        {
            // Initialize container snapshot with current system timestamp
            var snapshot = new FleetStateSnapshot { SavedAt = DateTime.Now };

            // Map each vehicle entity to its corresponding DTO snapshot
            foreach (var v in fleet)
            {
                // Pattern match on concrete sub-types to extract specialized property capacity values
                double extra = v switch
                {
                    Truck t => t.MaxLoadKg,
                    Van vn => vn.PackageCapacity,
                    Bus b => b.PassengerCapacity,
                    _ => 0
                };

                snapshot.Vehicles.Add(new VehicleSnapshot
                {
                    VehicleType = v.GetType().Name,
                    VehicleId = v.VehicleId,
                    LicensePlate = v.LicensePlate,
                    Mileage = v.Mileage,
                    FuelLevel = v.FuelLevel,
                    Status = v.Status.ToString(),
                    ExtraValue = extra
                });
            }

            // Map each driver entity to its corresponding DTO snapshot
            foreach (var d in drivers)
            {
                snapshot.Drivers.Add(new DriverSnapshot
                {
                    DriverId = d.DriverId,
                    Name = d.Name,
                    LicenseCode = d.LicenseCode,
                    HoursDrivenToday = d.HoursDrivenToday
                });
            }

            // Serialize snapshot object model into JSON string and save to disk
            string json = JsonSerializer.Serialize(snapshot, Options);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Reads and deserializes a saved JSON state snapshot from disk back into system DTOs.
        /// </summary>
        /// <param name="path">The file system path where the JSON snapshot is located.</param>
        /// <returns>The populated <see cref="FleetStateSnapshot"/> object hierarchy.</returns>
        /// <exception cref="FileNotFoundException">Thrown if no file exists at the specified path.</exception>
        /// <exception cref="InvalidDataException">Thrown if the target file is empty or cannot be parsed.</exception>
        public static FleetStateSnapshot LoadState(string path)
        {
            // Verify file exists on target path before reading
            if (!File.Exists(path))
                throw new FileNotFoundException($"No saved state found at '{path}'. Save first (option 9).");

            // Read contents and convert JSON text into Snapshot hierarchy
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<FleetStateSnapshot>(json, Options)
                ?? throw new InvalidDataException("Saved state file is empty or corrupted.");
        }
    }
}