using System;

namespace FleetPulse.Models
{
    /// <summary>
    /// Heavy-duty freight vehicle designed for long-haul cargo transport.
    /// Features higher fuel consumption scaled by load capacity and an extended service interval threshold.
    /// </summary>
    public class Truck : Vehicle
    {
        /// <summary>
        /// Gets the maximum payload weight capacity in kilograms that this truck is rated to haul.
        /// </summary>
        public double MaxLoadKg { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Truck"/> class with a license plate and payload threshold.
        /// Sets the maintenance service threshold to 20,000 km.
        /// </summary>
        /// <param name="licensePlate">The unique license plate identifier for the truck.</param>
        /// <param name="maxLoadKg">The maximum payload weight capacity in kilograms. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLoadKg"/> is less than or equal to zero.</exception>
        public Truck(string licensePlate, double maxLoadKg) : base(licensePlate)
        {
            if (maxLoadKg <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLoadKg), "Max load must be positive.");

            MaxLoadKg = maxLoadKg;

            // Trucks are built for higher-mileage service cycles (20,000 km threshold)
            ServiceThresholdKm = 20000;
        }

        /// <summary>
        /// Calculates total fuel consumption for a given trip distance using a weight-adjusted consumption rate.
        /// </summary>
        /// <param name="distanceKm">The distance traveled in kilometers.</param>
        /// <returns>The estimated fuel consumed in liters, scaling higher with heavier load capacity ratings.</returns>
        public override double CalculateFuelConsumption(double distanceKm)
        {
            // Heavier rated load capacity => proportionally higher consumption per km.
            return distanceKm * (0.35 + MaxLoadKg / 100000.0);
        }

        /// <summary>
        /// Outputs base vehicle attributes along with truck-specific maximum payload details to the console.
        /// </summary>
        public override void DisplayInfo()
        {
            base.DisplayInfo();
            Console.WriteLine($"      MaxLoad: {MaxLoadKg:F0} kg");
        }
    }
}