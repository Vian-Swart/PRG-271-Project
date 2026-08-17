namespace FleetPulse.Models
{
    /// <summary>Heavy-duty freight vehicle. Higher fuel use, longer service intervals.</summary>
    public class Truck : Vehicle
    {
        public double MaxLoadKg { get; }

        public Truck(string licensePlate, double maxLoadKg) : base(licensePlate)
        {
            if (maxLoadKg <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLoadKg), "Max load must be positive.");

            MaxLoadKg = maxLoadKg;
            ServiceThresholdKm = 20000; // trucks are built for higher-mileage service cycles
        }

        public override double CalculateFuelConsumption(double distanceKm)
        {
            // Heavier rated load capacity => proportionally higher consumption per km.
            return distanceKm * (0.35 + MaxLoadKg / 100000.0);
        }

        public override void DisplayInfo()
        {
            base.DisplayInfo();
            Console.WriteLine($"      MaxLoad: {MaxLoadKg:F0} kg");
        }
    }
}
