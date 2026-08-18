using System;

namespace FleetPulse.Models
{
    /// <summary>
    /// Passenger transport vehicle designed for public transit or intercity routes.
    /// Features capacity-scaled fuel consumption and an extended service threshold interval.
    /// </summary>
    public class Bus : Vehicle
    {
        /// <summary>
        /// Gets the maximum seating/passenger capacity rating for this bus.
        /// </summary>
        public int PassengerCapacity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bus"/> class with a license plate and passenger capacity limit.
        /// Sets the maintenance service threshold to 25,000 km.
        /// </summary>
        /// <param name="licensePlate">The unique license plate identifier for the bus.</param>
        /// <param name="passengerCapacity">The maximum number of passengers the bus can carry. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="passengerCapacity"/> is less than or equal to zero.</exception>
        public Bus(string licensePlate, int passengerCapacity) : base(licensePlate)
        {
            if (passengerCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(passengerCapacity), "Passenger capacity must be positive.");

            PassengerCapacity = passengerCapacity;

            // Buses run long-haul routes and are serviced least often by distance (25,000 km threshold)
            ServiceThresholdKm = 25000;
        }

        /// <summary>
        /// Calculates total fuel consumption for a given trip distance based on passenger capacity scaling.
        /// </summary>
        /// <param name="distanceKm">The distance traveled in kilometers.</param>
        /// <returns>The estimated fuel consumed in liters, scaling higher with larger seating capacity.</returns>
        public override double CalculateFuelConsumption(double distanceKm)
        {
            // Consumption scales with passenger capacity (larger bus = heavier = thirstier).
            return distanceKm * (0.28 + PassengerCapacity / 500.0);
        }

        /// <summary>
        /// Outputs base vehicle attributes along with bus-specific passenger seating capacity details to the console.
        /// </summary>
        public override void DisplayInfo()
        {
            base.DisplayInfo();
            Console.WriteLine($"      PassengerCapacity: {PassengerCapacity}");
        }
    }
}