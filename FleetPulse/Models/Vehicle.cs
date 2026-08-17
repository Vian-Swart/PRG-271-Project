using FleetPulse.Interfaces;

namespace FleetPulse.Models
{
    /// <summary>
    /// Abstract base for every vehicle type in the fleet.
    /// Demonstrates: Abstraction (abstract class + abstract method), Encapsulation
    /// (private setters, controlled mutation via methods), and provides the seam for
    /// Polymorphism (CalculateFuelConsumption / DisplayInfo overridden per subtype).
    /// </summary>
    public abstract class Vehicle : ITrackable, IMaintainable
    {
        private static int _nextId = 1000;

        public int VehicleId { get; }
        public string LicensePlate { get; set; }
        public double Mileage { get; private set; }
        public double FuelLevel { get; private set; } // percentage, 0-100
        public VehicleStatus Status { get; set; }
        public double CurrentLatitude { get; private set; }
        public double CurrentLongitude { get; private set; }
        public DateTime? LastServiceDate { get; private set; }

        /// <summary>Mileage (km) at which this vehicle becomes due for a service. Set per subtype.</summary>
        public double ServiceThresholdKm { get; protected set; } = 15000;

        protected Vehicle(string licensePlate)
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                throw new ArgumentException("License plate cannot be empty.", nameof(licensePlate));

            VehicleId = _nextId++;
            LicensePlate = licensePlate;
            Mileage = 0;
            FuelLevel = 100;
            Status = VehicleStatus.Idle;
            LastServiceDate = DateTime.Now;
        }

        /// <summary>
        /// Fuel used (as a percentage of tank) to cover the given distance.
        /// Each subtype defines its own formula - this is the polymorphic core of the sim.
        /// </summary>
        public abstract double CalculateFuelConsumption(double distanceKm);

        /// <summary>Applies a completed trip's distance and fuel cost to this vehicle.</summary>
        public virtual void Drive(double distanceKm)
        {
            if (distanceKm < 0)
                throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance cannot be negative.");

            double consumption = CalculateFuelConsumption(distanceKm);
            FuelLevel = Math.Max(0, FuelLevel - consumption);
            Mileage += distanceKm;
        }

        public void AdjustFuel(double delta) => FuelLevel = Math.Clamp(FuelLevel + delta, 0, 100);

        // ---- ITrackable ----
        public (double Lat, double Lon) GetCurrentLocation() => (CurrentLatitude, CurrentLongitude);

        public void UpdateLocation(double lat, double lon)
        {
            CurrentLatitude = lat;
            CurrentLongitude = lon;
        }

        // ---- IMaintainable ----
        public bool IsDueForService() => Mileage >= ServiceThresholdKm;

        public void ScheduleMaintenance()
        {
            LastServiceDate = DateTime.Now;
            Mileage = 0;
            Status = VehicleStatus.Maintenance;
        }

        /// <summary>
        /// Rehydrates a vehicle's mutable state after loading from a saved snapshot.
        /// Not used during normal operation - only by FileManager/DispatchCenter on load,
        /// since Mileage/FuelLevel otherwise only change through Drive/AdjustFuel.
        /// </summary>
        public void RestoreState(double mileage, double fuelLevel, VehicleStatus status)
        {
            Mileage = Math.Max(0, mileage);
            FuelLevel = Math.Clamp(fuelLevel, 0, 100);
            Status = status;
        }

        public virtual void DisplayInfo()
        {
            Console.WriteLine(
                $"[{VehicleId}] {GetType().Name,-6} Plate:{LicensePlate,-9} " +
                $"Mileage:{Mileage,8:F1}km  Fuel:{FuelLevel,5:F1}%  Status:{Status}");
        }
    }
}
