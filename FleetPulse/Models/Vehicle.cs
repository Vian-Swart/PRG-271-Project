using System;

namespace FleetPulse.Models
{
    public abstract class Vehicle
    {
        // Static counter to auto-generate unique Vehicle IDs
        private static int _nextId = 1;

        // Core properties
        public int VehicleId { get; protected set; }
        public string LicensePlate { get; protected set; }
        public double Mileage { get; protected set; }
        public double FuelLevel { get; protected set; }
        public VehicleStatus Status { get; set; }
        
        // The property your child classes are looking for
        public double ServiceThresholdKm { get; protected set; }

        // Constructor expected by Bus, Truck, and Van
        protected Vehicle(string licensePlate)
        {
            VehicleId = _nextId++;
            LicensePlate = licensePlate;
            FuelLevel = 100.0; // Default starting fuel
            Status = VehicleStatus.Idle;
        }

        // Abstract method that child classes MUST override
        public abstract double CalculateFuelConsumption(double distance);

        // Virtual DisplayInfo so child classes can call base.DisplayInfo()
        public virtual void DisplayInfo()
        {
            Console.WriteLine($"ID: {VehicleId} | Plate: {LicensePlate} | Mileage: {Mileage:F1}km | Fuel: {FuelLevel:F1}% | Status: {Status}");
        }

        // Concrete methods
        public virtual void Drive(double distance)
        {
            Mileage += distance;
            double fuelUsed = CalculateFuelConsumption(distance);
            FuelLevel -= fuelUsed;
            if (FuelLevel < 0) FuelLevel = 0;
        }

        public virtual void AdjustFuel(double amount)
        {
            FuelLevel += amount;
            if (FuelLevel > 100) FuelLevel = 100;
            if (FuelLevel < 0) FuelLevel = 0;
        }

        public virtual bool IsDueForService()
        {
            // Compares current mileage against the threshold set by child classes
            return Mileage >= ServiceThresholdKm;
        }

        public virtual void ScheduleMaintenance()
        {
            // Bumps the service threshold up for the next service cycle
            // (You can adjust this logic if your original code did it differently)
            ServiceThresholdKm += 15000; 
        }

        public virtual void RestoreState(double mileage, double fuelLevel, VehicleStatus status)
        {
            Mileage = mileage;
            FuelLevel = fuelLevel;
            Status = status;
        }
    }
}