using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

namespace FleetPulse.UI
{
    /// <summary>
    /// Provides console UI rendering helper methods for standard headers, 
    /// colored status messages, keyboard key-pause handling, and main menu options.
    /// </summary>
    public static class MenuRender
    {
        /// <summary>
        /// Clears the console frame context by displaying the standardized application ASCII header banner
        /// and flushing queued background logs.
        /// </summary>
        public static void MenuHeader()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("   FleetPulse - Smart Fleet Dispatch     ");
            Console.WriteLine("=========================================\n");
            
            // Flush any asynchronous background logs to avoid output collision with UI elements
            Program.FlushLogs(); 
        }

        /// <summary>
        /// Outputs an error message to the console in bright red text while preserving the user's previous foreground color state.
        /// </summary>
        /// <param name="message">The text message to print in red.</param>
        public static void PrintError(string message)
        {
            // Store current color state to avoid altering global terminal properties
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = prev; // Restore original foreground color
        }

        /// <summary>
        /// Outputs a success message to the console in bright green text while preserving the user's previous foreground color state.
        /// </summary>
        /// <param name="message">The text message to print in green.</param>
        public static void PrintSuccess(string message)
        {
            // Store current color state to avoid altering global terminal properties
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = prev; // Restore original foreground color
        }

        /// <summary>
        /// Displays a pause prompt, waits for a keypress without echoing the character, and clears the console screen.
        /// </summary>
        public static void WaitForKey()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true); // Suppress key visibility in console output
            Console.Clear();
        }

        /// <summary>
        /// Renders the primary navigation menu options using integer values mapped from <see cref="MainMenuOption"/>.
        /// </summary>
        /// <param name="successMsg">Optional success status message to display above the menu.</param>
        /// <param name="errorMsg">Optional error status message to display above the menu.</param>
        public static void PrintMenu(string? successMsg = null, string? errorMsg = null)
        {
            // Render application banner
            MenuHeader();

            // Display active success message if provided
            if (!string.IsNullOrEmpty(successMsg))
            {
                PrintSuccess(successMsg);
            }

            // Display active error message if provided
            if (!string.IsNullOrEmpty(errorMsg))
            {
                PrintError(errorMsg);
            }

            // Dynamically cast enum values to maintain alignment with input options
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine($"{(int)MainMenuOption.ViewFleet}.  View Fleet");
            Console.WriteLine($"{(int)MainMenuOption.AddVehicle}.  Add Vehicle");
            Console.WriteLine($"{(int)MainMenuOption.RemoveVehicle}.  Remove Vehicle");
            Console.WriteLine($"{(int)MainMenuOption.AddDriver}.  Add Driver");
            Console.WriteLine($"{(int)MainMenuOption.CreateRoute}.  Create Route");
            Console.WriteLine($"{(int)MainMenuOption.AssignRoute}.  Assign Route to Vehicle & Driver");
            Console.WriteLine($"{(int)MainMenuOption.ViewRoutes}.  View Routes");
            Console.WriteLine($"{(int)MainMenuOption.ViewReports}.  Reports (LINQ)");
            Console.WriteLine($"{(int)MainMenuOption.SaveState}.  Save Fleet State (JSON)");
            Console.WriteLine($"{(int)MainMenuOption.LoadState}. Load Fleet State (JSON)");
            Console.WriteLine($"{(int)MainMenuOption.ToggleMonitoring}. Toggle Background Monitoring");
            Console.WriteLine($"{(int)MainMenuOption.ViewDrivers}. View Drivers");
            Console.WriteLine($"{(int)MainMenuOption.Exit}.  Exit");
            Console.WriteLine("---------------------------------------------");
            Console.Write("Choose an option: ");
        }
    }
}