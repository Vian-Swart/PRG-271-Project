using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

namespace FleetPulse.UI
{
    /// <summary>
    /// Handles user interaction for driver management operations in the console UI.
    /// Provides workflows for adding new driver records and viewing registered drivers.
    /// </summary>
    public static class DriverUI
    {
        /// <summary>
        /// Guides the user through a console prompt workflow to register a new driver in the system.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        /// <returns>A tuple containing a success message or null if the operation is canceled.</returns>
        public static (string? success, string? error) AddDriverFlow(DispatchCenter dispatch)
        {
            // Prompt and process driver name input
            Console.Write("Driver name [0 to cancel]: ");
            string name = (Console.ReadLine() ?? "").Trim();
            if (name == "0") return (null, null); // User canceled
            if (string.IsNullOrWhiteSpace(name)) name = "Unnamed"; // Default fallback for empty input

            // Prompt and process driver license code input
            Console.Write("License code [0 to cancel]: ");
            string code = (Console.ReadLine() ?? "").Trim();
            if (code == "0") return (null, null); // User canceled
            if (string.IsNullOrWhiteSpace(code)) code = "N/A"; // Default fallback for empty input

            // Instantiate driver model and add to dispatch records
            var d = new Driver(name, code);
            dispatch.AddDriver(d);
            return ($"Added driver #{d.DriverId}.\n", null);
        }

        /// <summary>
        /// Displays all registered drivers currently recorded in the dispatch system.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        public static void ViewDrivers(DispatchCenter dispatch)
        {
            var drivers = dispatch.Drivers;
            
            // Render driver list or empty fallback message
            if (drivers.Count == 0)
            {
                Console.WriteLine("No drivers on record.");
            }
            else
            {
                foreach (var d in drivers) Console.WriteLine(d);
            }

            // Pause execution for key press before returning to main loop
            MenuRender.WaitForKey();
        }
    }
}