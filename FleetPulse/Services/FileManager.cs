using System.Text.Json;
using FleetPulse.Models;

namespace FleetPulse.Services
{
    // Plain DTOs used only for persistence. Kept deliberately separate from the domain
    // model (Vehicle etc. have private setters and business logic) so save/load stays
    // a pure data-transfer concern.
    public class VehicleSnapshot
    {
        public string VehicleType { get; set; } = "";
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; } = "";
        public double Mileage { get; set; }
        public double FuelLevel { get; set; }
        public string Status { get; set; } = "";

        /// <summary>Holds MaxLoadKg (Truck), PackageCapacity (Van), or PassengerCapacity (Bus).</summary>
        public double ExtraValue { get; set; }
    }

    public class DriverSnapshot
    {
        public int DriverId { get; set; }
        public string Name { get; set; } = "";
        public string LicenseCode { get; set; } = "";
        public double HoursDrivenToday { get; set; }
    }

    public class FleetStateSnapshot
    {
        public List<VehicleSnapshot> Vehicles { get; set; } = new();
        public List<DriverSnapshot> Drivers { get; set; } = new();
        public DateTime SavedAt { get; set; }
    }

    /// <summary>Bonus feature: saves and loads the entire fleet/driver state to/from a JSON file on disk.</summary>
    public static class FileManager
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public static void SaveState(string path, IEnumerable<Vehicle> fleet, IEnumerable<Driver> drivers)
        {
            var snapshot = new FleetStateSnapshot { SavedAt = DateTime.Now };

            foreach (var v in fleet)
            {
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

            string json = JsonSerializer.Serialize(snapshot, Options);
            File.WriteAllText(path, json);
        }

        public static FleetStateSnapshot LoadState(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"No saved state found at '{path}'. Save first (option 9).");

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<FleetStateSnapshot>(json, Options)
                ?? throw new InvalidDataException("Saved state file is empty or corrupted.");
        }
    }
}
