namespace FleetPulse.Models
{
    /// <summary>A driver who can be assigned to route jobs, subject to a daily hour limit.</summary>
    public class Driver
    {
        private static int _nextId = 500;

        /// <summary>Domain rule: no driver may be scheduled past this many driving hours in one day.</summary>
        public const double MaxDailyHours = 10.0;

        public int DriverId { get; }
        public string Name { get; }
        public string LicenseCode { get; }
        public double HoursDrivenToday { get; private set; }

        public Driver(string name, string licenseCode)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Driver name cannot be empty.", nameof(name));

            DriverId = _nextId++;
            Name = name;
            LicenseCode = string.IsNullOrWhiteSpace(licenseCode) ? "N/A" : licenseCode;
            HoursDrivenToday = 0;
        }

        public void LogHours(double hours)
        {
            if (hours < 0)
                throw new ArgumentOutOfRangeException(nameof(hours), "Hours cannot be negative.");
            HoursDrivenToday += hours;
        }

        public void ResetDailyHours() => HoursDrivenToday = 0;

        public override string ToString() =>
            $"[{DriverId}] {Name} (License:{LicenseCode})  Hours today: {HoursDrivenToday:F1}h / {MaxDailyHours:F0}h";
    }
}
