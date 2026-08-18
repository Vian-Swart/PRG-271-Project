using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

namespace FleetPulse.UI
{
    public static class RouteUI
    {
        public static readonly string[] PresetPlaces =
        {
            "Johannesburg",
            "Pretoria",
            "Durban",
            "Cape Town",
            "Bloemfontein",
            "Polokwane"
        };

        public static (string? success, string? error) CreateRouteFlow(DispatchCenter dispatch, string[] PresetPlaces)
        {
            string? origin = PromptPlace("Select origin");
            if (origin == null) return (null, null);

            string? dest = PromptPlace("Select destination", origin);
            if (dest == null) return (null, null);

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

            var priority = PromptPriority();
            if (priority == null) return (null, null);

            var route = dispatch.CreateRoute(origin, dest, dist, priority.Value);
            return ($"Created {route}\n", null);
        }

        public static string? PromptPlace(string prompt, string? excludedPlace = null)
        {
            string? localError = null;
            while (true)
            {
                Console.Clear();
                MenuRender.MenuHeader();

                if (localError != null) MenuRender.PrintError(localError);

                Console.WriteLine($"{prompt}:");
                Console.WriteLine("0. Cancel");
                for (int i = 0; i < PresetPlaces.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {PresetPlaces[i]}");
                }

                Console.Write("Choose an option: ");
                string input = (Console.ReadLine() ?? "").Trim();
                
                if (input == "0") return null;

                if (!int.TryParse(input, out int choice) || choice < 1 || choice > PresetPlaces.Length)
                {
                    localError = "Invalid option. Please choose a valid place number.";
                    continue;
                }

                string selectedPlace = PresetPlaces[choice - 1];
                if (excludedPlace != null && string.Equals(selectedPlace, excludedPlace, StringComparison.OrdinalIgnoreCase))
                {
                    localError = "Destination cannot be the same as origin. Please choose a different place.";
                    continue;
                }

                return selectedPlace;
            }
        }

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

                if (!int.TryParse(input, out int choice) || choice < 1 || choice > 4)
                {
                    localError = "Invalid option. Please choose 1, 2, 3, or 4.";
                    continue;
                }

                return (Priority)(choice - 1);
            }
        }

        public static (string? success, string? error) AssignRouteFlow(DispatchCenter dispatch)
        {
            var availableRoutes = dispatch.Routes.Where(r => r.Status == RouteStatus.Pending).ToList();
            if (availableRoutes.Count == 0)
            {
                return (null, "No pending routes available for assignment.\n");
            }

            var availableDrivers = dispatch.Drivers.ToList();
            if (availableDrivers.Count == 0)
            {
                return (null, "No drivers available for assignment.\n");
            }

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

            dispatch.AssignRoute(routeId, vehicleId, driverId);
            return ($"Route#{routeId} assigned to Vehicle#{vehicleId} / Driver#{driverId}.\n", null);
        }

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

            MenuRender.WaitForKey();
        }

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

            MenuRender.WaitForKey();
        }
    }
}