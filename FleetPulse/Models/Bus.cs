namespace FleetPulse.Models
{
    /// <summary>Passenger transport vehicle.</summary>
    public class Bus : Vehicle
    {
        public int PassengerCapacity { get; }

        public Bus(string licensePlate, int passengerCapacity) : base(licensePlate)
        {
            if (passengerCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(passengerCapacity), "Passenger capacity must be positive.");

            PassengerCapacity = passengerCapacity;
            ServiceThresholdKm = 25000; // buses run long-haul routes, serviced least often by distance
        }

        public override double CalculateFuelConsumption(double distanceKm)
        {
            // Consumption scales with passenger capacity (larger bus = heavier = thirstier).
            return distanceKm * (0.28 + PassengerCapacity / 500.0);
        }

        public override void DisplayInfo()
        {
            base.DisplayInfo();
            Console.WriteLine($"      PassengerCapacity: {PassengerCapacity}");
        }
    }
}
