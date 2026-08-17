namespace FleetPulse.Models
{
    /// <summary>Light delivery vehicle used for parcel/package runs.</summary>
    public class Van : Vehicle
    {
        public int PackageCapacity { get; }

        public Van(string licensePlate, int packageCapacity) : base(licensePlate)
        {
            if (packageCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(packageCapacity), "Package capacity must be positive.");

            PackageCapacity = packageCapacity;
            ServiceThresholdKm = 15000;
        }

        public override double CalculateFuelConsumption(double distanceKm)
        {
            // Vans are the most fuel-efficient class in the fleet - flat rate per km.
            return distanceKm * 0.22;
        }

        public override void DisplayInfo()
        {
            base.DisplayInfo();
            Console.WriteLine($"      PackageCapacity: {PackageCapacity}");
        }
    }
}
