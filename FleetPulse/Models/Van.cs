using System;

namespace FleetPulse.Models
{
    /// <summary>
    /// Light delivery vehicle used for parcel/package runs.
    /// Derived from <see cref="Vehicle"/> with a fixed service threshold and package volume capacity.
    /// </summary>
    public class Van : Vehicle
    {
        /// <summary>
        /// Gets the maximum package storage capacity for this van.
        /// </summary>
        public int PackageCapacity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Van"/> class with a license plate and package capacity limit.
        /// Sets the maintenance service threshold to 15,000 km.
        /// </summary>
        /// <param name="licensePlate">The unique license plate identifier for the van.</param>
        /// <param name="packageCapacity">The maximum number of packages the van can transport. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="packageCapacity"/> is less than or equal to zero.</exception>
        public Van(string licensePlate, int packageCapacity) : base(licensePlate)
        {
            if (packageCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(packageCapacity), "Package capacity must be positive.");

            PackageCapacity = packageCapacity;
            
            // Set domain specific maintenance interval limit (15,000 km for vans)
            ServiceThresholdKm = 15000;
        }

        /// <summary>
        /// Calculates total fuel consumption for a given trip distance using the van's fuel efficiency formula.
        /// </summary>
        /// <param name="distanceKm">The distance traveled in kilometers.</param>
        /// <returns>The estimated fuel consumed in liters (0.22 L/km flat rate).</returns>
        public override double CalculateFuelConsumption(double distanceKm)
        {
            // Vans are the most fuel-efficient class in the fleet - flat rate per km.
            return distanceKm * 0.22;
        }

        /// <summary>
        /// Outputs base vehicle attributes along with van-specific capacity details to the console.
        /// </summary>
        public override void DisplayInfo()
        {
            base.DisplayInfo();
            Console.WriteLine($"      PackageCapacity: {PackageCapacity}");
        }
    }
}