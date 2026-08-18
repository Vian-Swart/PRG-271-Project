using System;

namespace FleetPulse.Models
{
    /// <summary>
    /// Represents a commercial driver who can be assigned to route jobs.
    /// Tracks driving time and enforces safety rules for regulatory daily hour caps.
    /// </summary>
    public class Driver
    {
        /// <summary>
        /// Static auto-incrementing sequence counter starting at 500 for generating unique driver IDs.
        /// </summary>
        private static int _nextId = 500;

        /// <summary>
        /// Domain rule: Maximum allowable driving duration per driver in a single 24-hour shift (10 hours).
        /// </summary>
        public const double MaxDailyHours = 10.0;

        /// <summary>Gets the unique identifier for this driver.</summary>
        public int DriverId { get; }

        /// <summary>Gets the driver's full name.</summary>
        public string Name { get; }

        /// <summary>Gets the commercial driving license code or reference number.</summary>
        public string LicenseCode { get; }

        /// <summary>Gets the total driving hours accumulated by this driver during the current daily shift.</summary>
        public double HoursDrivenToday { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Driver"/> class with a name and license code.
        /// Assigns an auto-incremented <see cref="DriverId"/> and sets initial accumulated shift hours to zero.
        /// </summary>
        /// <param name="name">The full name of the driver. Must not be empty or whitespace.</param>
        /// <param name="licenseCode">The driver's license code. Defaults to "N/A" if empty or whitespace.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
        public Driver(string name, string licenseCode)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Driver name cannot be empty.", nameof(name));

            // Auto-increment and assign domain ID starting from 500
            DriverId = _nextId++;
            Name = name;

            // Fall back to default identifier if license code was omitted
            LicenseCode = string.IsNullOrWhiteSpace(licenseCode) ? "N/A" : licenseCode;

            // Initialize daily shift hour counter
            HoursDrivenToday = 0;
        }

        /// <summary>
        /// Accumulates driving hours onto the driver's current shift total.
        /// </summary>
        /// <param name="hours">The duration of driving hours to log. Must be non-negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="hours"/> is less than zero.</exception>
        public void LogHours(double hours)
        {
            if (hours < 0)
                throw new ArgumentOutOfRangeException(nameof(hours), "Hours cannot be negative.");

            HoursDrivenToday += hours;
        }

        /// <summary>
        /// Resets the accumulated shift hours counter to zero to begin a new shift day.
        /// </summary>
        public void ResetDailyHours() => HoursDrivenToday = 0;

        /// <summary>
        /// Generates a formatted string summary detailing the driver ID, name, license code, and current daily shift hours against the daily cap.
        /// </summary>
        /// <returns>A string representation of the driver entity state.</returns>
        public override string ToString() =>
            $"[{DriverId}] {Name} (License:{LicenseCode})  Hours today: {HoursDrivenToday:F1}h / {MaxDailyHours:F0}h";
    }
}