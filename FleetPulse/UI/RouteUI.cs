using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

namespace FleetPulse.UI
{
    /// <summary>
    /// Handles user interaction for route management operations in the console UI.
    /// Provides workflows for creating, assigning, viewing, and generating reports on delivery routes.
    /// </summary>
    public static class RouteUI
    {
        /// <summary>
        /// Default list of pre-configured South African cities available for quick route creation.
        /// </summary>
        public static readonly string[] PresetPlaces =
        {
            "Johannesburg",
            "Pretoria",
            "Durban",
            "Cape Town",
            "Bloemfontein",
            "Polokwane"
        };

        /// <summary>
        /// Guides the user through a console workflow to create a new delivery route.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        /// <param name="PresetPlaces">Array of location names available for selection.</param>
        /// <returns>A tuple containing a success message or an error message if the creation is canceled.</returns>
        public static (string? success, string? error) CreateRouteFlow(DispatchCenter dispatch, string[] PresetPlaces)
        {
            // Step 1: Prompt for origin location
            string? origin = PromptPlace("Select origin");
            if (origin == null) return (null, null); // User canceled

            // Step 2: Prompt for destination location (excluding origin to prevent self-routes)
            string? dest = PromptPlace("Select destination", origin);
            if (dest == null) return (null, null); // User canceled

            // Step 3: Prompt and validate route distance in kilometers
            double dist;
            while (true)
            {
                Console.Write("Distance in km [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return (null, null);

                if (double.TryParse(input, out dist) && dist > 0)
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid, positive number.");
            }

            // Step 4: Prompt for route priority/urgency level
            var priority = PromptPriority();
            if (priority == null) return (null, null); // User canceled

            // Step 5: Instantiate and register the route in the dispatch system
            var route = dispatch.CreateRoute(origin, dest, dist, priority.Value);
            return ($"Created {route}\n", null);
        }

        /// <summary>
        /// Displays a selectable menu of places from <see cref="PresetPlaces"/> and returns the chosen location string.
        /// </summary>
        /// <param name="prompt">The text header to display above the location list.</param>
        /// <param name="excludedPlace">An optional place name that cannot be selected (e.g., origin city).</param>
        /// <returns>The selected place string, or null if canceled.</returns>
        public static string? PromptPlace(string prompt, string? excludedPlace = null)
        {
            string? localError = null;
            while (true)
            {
                Console.Clear();
                MenuRender.MenuHeader();

                if (localError != null) MenuRender.PrintError(localError);

                // Render option list
                Console.WriteLine($"{prompt}:");
                Console.WriteLine("0. Cancel");
                for (int i = 0; i < PresetPlaces.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {PresetPlaces[i]}");
                }

                Console.Write("Choose an option: ");
                string input = (Console.ReadLine() ?? "").Trim();
                
                if (input == "0") return null;

                // Validate numerical choice within range
                if (!int.TryParse(input, out int choice) || choice < 1 || choice > PresetPlaces.Length)
                {
                    localError = "Invalid option. Please choose a valid place number.";
                    continue;
                }

                // Check for duplicate origin/destination constraint
                string selectedPlace = PresetPlaces[choice - 1];
                if (excludedPlace != null && string.Equals(selectedPlace, excludedPlace, StringComparison.OrdinalIgnoreCase))
                {
                    localError = "Destination cannot be the same as origin. Please choose a different place.";
                    continue;
                }

                return selectedPlace;
            }
        }

        /// <summary>
        /// Displays an urgency selection menu and converts user choice into a <see cref="Priority"/> enum value.
        /// </summary>
        /// <returns>The selected <see cref="Priority"/> level, or null if canceled.</returns>
        public static Priority? PromptPriority()
        {
            string? localError = null;
            while (true)
            {
                Console.Clear();
                MenuRender.MenuHeader();

                if (localError != null) MenuRender.PrintError(localError);

                Console.WriteLine("Select urgency:");
                Console.WriteLine("0. Cancel");
                Console.WriteLine("1. Low");
                Console.WriteLine("2. Medium");
                Console.WriteLine("3. High");
                Console.WriteLine("4. Critical");
                Console.Write("Choose urgency: ");

                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return null;

                // Validate range (1-4)
                if (!int.TryParse(input, out int choice) || choice < 1 || choice > 4)
                {
                    localError = "Invalid option. Please choose 1, 2, 3, or 4.";
                    continue;
                }

                // Map 1-4 menu selections to zero-indexed Priority enum (0=Low, 1=Medium, 2=High, 3=Critical)
                return (Priority)(choice - 1);
            }
        }

        /// <summary>
        /// Interactive workflow to assign a pending route to an available driver and an idle vehicle.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        /// <returns>A tuple containing a success message or an error message if resources are unavailable or invalid.</returns>
        public static (string? success, string? error) AssignRouteFlow(DispatchCenter dispatch)
        {
            // Pre-check 1: Filter pending routes
            var availableRoutes = dispatch.Routes.Where(r => r.Status == RouteStatus.Pending).ToList();
            if (availableRoutes.Count == 0)
            {
                return (null, "No pending routes available for assignment.\n");
            }

            // Pre-check 2: Ensure drivers exist
            var availableDrivers = dispatch.Drivers.ToList();
            if (availableDrivers.Count == 0)
            {
                return (null, "No drivers available for assignment.\n");
            }

            // Render available candidate lists for user selection
            Console.WriteLine("--- Available Pending Routes ---");
            foreach (var route in availableRoutes)
            {
                Console.WriteLine(route);
            }

            Console.WriteLine("\n--- Available Drivers ---");
            foreach (var driver in availableDrivers)
            {
                Console.WriteLine(driver);
            }

            // Prompt for Target Route ID
            int routeId;
            while (true)
            {
                Console.Write("\nRoute ID [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return (null, null);

                if (int.TryParse(input, out routeId))
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid numerical Route ID.");
            }

            // Prompt for Target Driver ID
            int driverId;
            while (true)
            {
                Console.Write("Driver ID [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return (null, null);

                if (int.TryParse(input, out driverId))
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid numerical Driver ID.");
            }

            // Pre-check 3: Ensure at least one idle vehicle exists
            var idleVehicles = dispatch.Fleet.Where(v => v.Status == VehicleStatus.Idle).ToList();
            if (idleVehicles.Count == 0)
            {
                return (null, "No idle vehicles available for assignment.\n");
            }

            Console.WriteLine("\n--- Available Idle Vehicles ---");
            foreach (var vehicle in idleVehicles)
            {
                vehicle.DisplayInfo();
            }

            // Prompt for Target Vehicle ID
            int vehicleId;
            while (true)
            {
                Console.Write("Vehicle ID [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return (null, null);

                if (int.TryParse(input, out vehicleId))
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid numerical Vehicle ID.");
            }

            // Execute assignment logic via dispatch service
            dispatch.AssignRoute(routeId, vehicleId, driverId);
            return ($"Route#{routeId} assigned to Vehicle#{vehicleId} / Driver#{driverId}.\n", null);
        }

        /// <summary>
        /// Displays all routes currently registered in the system.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        public static void ViewRoutes(DispatchCenter dispatch)
        {
            var routes = dispatch.Routes;
            if (routes.Count == 0)
            {
                Console.WriteLine("No routes yet.");
            }
            else
            {
                foreach (var r in routes) Console.WriteLine(r);
            }

            // Pause for user key press before returning to main loop
            MenuRender.WaitForKey();
        }

        /// <summary>
        /// Aggregates and prints comprehensive fleet and operational reports using <see cref="ReportService"/>.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        public static void ViewReports(DispatchCenter dispatch)
        {
            ReportService.PrintFleetSummary(dispatch.Fleet);
            Console.WriteLine();
            ReportService.PrintLowFuelVehicles(dispatch.Fleet);
            Console.WriteLine();
            ReportService.PrintMaintenanceDue(dispatch.Fleet);
            Console.WriteLine();
            ReportService.PrintRoutesByPriority(dispatch.Routes);
            Console.WriteLine();
            ReportService.PrintCompletedRoutesReport(dispatch.Routes);

            // Pause for user key press before returning to main loop
            MenuRender.WaitForKey();
        }
    }
}