using System;
using System.Linq;
using System.Collections.Concurrent;
using FleetPulse.Exceptions;
using FleetPulse.Models;
using FleetPulse.Services;

namespace FleetPulse
{
    public enum MainMenuOption
    {
        Exit = 0,
        ViewFleet = 1,
        AddVehicle = 2,
        RemoveVehicle = 3,
        AddDriver = 4,
        CreateRoute = 5,
        AssignRoute = 6,
        ViewRoutes = 7,
        ViewReports = 8,
        SaveState = 9,
        LoadState = 10,
        ToggleMonitoring = 11,
        ViewDrivers = 12
    }

    public enum VehicleTypeOption
    {
        Truck = 1,
        Van = 2,
        Bus = 3
    }

    public static class Program
    {
        private static readonly DispatchCenter Dispatch = new();
        private static readonly Random Rng = new();
        private static readonly string[] PresetPlaces =
        {
            "Johannesburg",
            "Pretoria",
            "Durban",
            "Cape Town",
            "Bloemfontein",
            "Polokwane"
        };
        private const string SaveFilePath = "fleetpulse_state.json";

        // FIX 1: Use a thread-safe queue to hold logs so they don't print while the user is typing
        private static readonly ConcurrentQueue<(ConsoleColor color, string tag, string message)> EventLogs = new();

        public static void MenuHeader()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("   FleetPulse - Smart Fleet Dispatch     ");
            Console.WriteLine("=========================================\n");
        }

        public static void Main(string[] args)
        {
            SubscribeToEvents();
            SeedRandomData();
            Dispatch.StartMonitoring();

            string startupMsg = "Background monitoring started (fuel, breakdowns, maintenance, auto-deliveries).\n";
            RunMenu(startupMsg);

            Dispatch.StopMonitoring();
            Console.Clear();
            PrintSuccess("Shutting down FleetPulse. Goodbye!");
        }

        // ---------------- Console Helper Methods ----------------

        private static void PrintError(string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }

        private static void PrintSuccess(string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }

        private static void WaitForKey()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
            Console.Clear();
        }

        private static void SubscribeToEvents()
        {
            Dispatch.BreakdownDetected += (v, msg) => LogEvent(ConsoleColor.Red, "BREAKDOWN", msg);
            Dispatch.FuelLowWarning += (v, msg) => LogEvent(ConsoleColor.Yellow, "FUEL LOW", msg);
            Dispatch.MaintenanceDue += (v, msg) => LogEvent(ConsoleColor.Magenta, "MAINTENANCE", msg);
            Dispatch.DeliveryCompleted += route =>
                LogEvent(ConsoleColor.Green, "DELIVERED",
                    $"Route#{route.RouteId} ({route.Origin} -> {route.Destination}) completed.");
        }

        private static void LogEvent(ConsoleColor color, string tag, string message)
        {
            // Instead of printing immediately, enqueue it.
            EventLogs.Enqueue((color, tag, message));
        }

        private static void FlushLogs()
        {
            bool hasLogs = false;
            while (EventLogs.TryDequeue(out var log))
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = log.color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{log.tag}] {log.message}");
                Console.ForegroundColor = prev;
                hasLogs = true;
            }

            if (hasLogs)
            {
                Console.WriteLine(); // Add spacing after logs
            }
        }

        private static void SeedRandomData()
        {
            Dispatch.AddVehicle(new Truck($"TRK-{Rng.Next(100, 999)}", Rng.Next(3000, 12000)));
            Dispatch.AddVehicle(new Van($"VAN-{Rng.Next(100, 999)}", Rng.Next(20, 80)));
            Dispatch.AddVehicle(new Bus($"BUS-{Rng.Next(100, 999)}", Rng.Next(30, 60)));

            Dispatch.AddDriver(new Driver("Thabo Nkosi", "C1-EL"));
            Dispatch.AddDriver(new Driver("Amanda van Wyk", "EC1"));

            for (int i = 0; i < 3; i++)
            {
                string origin = PresetPlaces[Rng.Next(PresetPlaces.Length)];
                string dest;
                do
                {
                    dest = PresetPlaces[Rng.Next(PresetPlaces.Length)];
                } while (dest == origin);

                var priority = (Priority)Rng.Next(0, 4);
                Dispatch.CreateRoute(origin, dest, Rng.Next(50, 600), priority);
            }
        }

        // ---------------- Menu loop ----------------

        private static void RunMenu(string? startupMessage = null)
        {
            bool running = true;
            string? currentMsg = startupMessage;
            string? errorMsg = null;

            while (running)
            {
                Console.Clear();
                
                // Flush background logs before drawing the menu so they appear at the top cleanly
                FlushLogs();
                
                PrintMenu(currentMsg, errorMsg);
                currentMsg = null;
                errorMsg = null;

                string? choice = Console.ReadLine();

                try
                {
                    if (Enum.TryParse(choice, out MainMenuOption menuOption) && Enum.IsDefined(menuOption))
                    {
                        if (menuOption != MainMenuOption.Exit)
                        {
                            Console.Clear();
                            MenuHeader();
                        }

                        switch (menuOption)
                        {
                            case MainMenuOption.ViewFleet:
                                ViewFleet();
                                break;
                            case MainMenuOption.AddVehicle:
                                (currentMsg, errorMsg) = AddVehicleFlow();
                                break;
                            case MainMenuOption.RemoveVehicle:
                                (currentMsg, errorMsg) = RemoveVehicleFlow();
                                break;
                            case MainMenuOption.AddDriver:
                                (currentMsg, errorMsg) = AddDriverFlow();
                                break;
                            case MainMenuOption.CreateRoute:
                                (currentMsg, errorMsg) = CreateRouteFlow();
                                break;
                            case MainMenuOption.AssignRoute:
                                (currentMsg, errorMsg) = AssignRouteFlow();
                                break;
                            case MainMenuOption.ViewRoutes:
                                ViewRoutes();
                                break;
                            case MainMenuOption.ViewReports:
                                ViewReports();
                                break;
                            case MainMenuOption.SaveState:
                                (currentMsg, errorMsg) = SaveStateFlow();
                                break;
                            case MainMenuOption.LoadState:
                                (currentMsg, errorMsg) = LoadStateFlow();
                                break;
                            case MainMenuOption.ToggleMonitoring:
                                (currentMsg, errorMsg) = ToggleMonitoring();
                                break;
                            case MainMenuOption.ViewDrivers:
                                ViewDrivers();
                                break;
                            case MainMenuOption.Exit:
                                running = false;
                                break;
                        }
                    }
                    else
                    {
                        errorMsg = "Invalid option, please choose again.\n";
                    }
                }
                catch (FleetCapacityExceededException ex)
                {
                    errorMsg = $"[Capacity Error] {ex.Message}\n";
                }
                catch (DriverHourLimitExceededException ex)
                {
                    errorMsg = $"[Driver Hours Error] {ex.Message}\n";
                }
                catch (InvalidRouteAssignmentException ex)
                {
                    errorMsg = $"[Assignment Error] {ex.Message}\n";
                }
                catch (FileNotFoundException ex)
                {
                    errorMsg = $"[File Error] {ex.Message}\n";
                }
                catch (FormatException)
                {
                    errorMsg = "[Input Error] Please enter a valid number where a number is expected.\n";
                }
                catch (Exception ex)
                {
                    errorMsg = $"[Unexpected Error] {ex.Message}\n";
                }
            }
        }

        private static void PrintMenu(string? successMsg = null, string? errorMsg = null)
        {
            MenuHeader();

            if (!string.IsNullOrEmpty(successMsg))
            {
                PrintSuccess(successMsg);
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                PrintError(errorMsg);
            }

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

        // ---------------- Menu actions ----------------

        private static void ViewFleet()
        {
            var fleet = Dispatch.Fleet;
            if (fleet.Count == 0)
            {
                Console.WriteLine("Fleet is empty.");
            }
            else
            {
                foreach (var v in fleet)
                {
                    v.DisplayInfo();
                }
            }

            WaitForKey();
        }

        private static void ViewDrivers()
        {
            var drivers = Dispatch.Drivers;
            if (drivers.Count == 0)
            {
                Console.WriteLine("No drivers on record.");
            }
            else
            {
                foreach (var d in drivers) Console.WriteLine(d);
            }

            WaitForKey();
        }

        private static (string? success, string? error) AddVehicleFlow()
        {
            string? localError = null;
            VehicleTypeOption vehicleType;
            
            while (true)
            {
                Console.Clear();
                MenuHeader();
                
                if (localError != null) PrintError(localError);

                Console.WriteLine("Select Vehicle Type:");
                Console.WriteLine($"{(int)VehicleTypeOption.Truck}. Truck");
                Console.WriteLine($"{(int)VehicleTypeOption.Van}. Van");
                Console.WriteLine($"{(int)VehicleTypeOption.Bus}. Bus");
                Console.Write("Choose an option: ");
                
                string choice = (Console.ReadLine() ?? "").Trim();

                if (Enum.TryParse(choice, out vehicleType) && Enum.IsDefined(vehicleType))
                {
                    break;
                }
                localError = "Invalid option. Please enter 1, 2, or 3.";
            }

            string plate;
            while (true)
            {
                Console.Write("License plate: ");
                plate = (Console.ReadLine() ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(plate))
                {
                    break;
                }
                PrintError("License plate cannot be empty. Please enter a valid license plate.");
            }

            Vehicle vehicle = vehicleType switch
            {
                VehicleTypeOption.Truck => PromptTruck(plate),
                VehicleTypeOption.Van => PromptVan(plate),
                VehicleTypeOption.Bus => PromptBus(plate),
                _ => throw new InvalidOperationException("Unexpected vehicle type.")
            };

            Dispatch.AddVehicle(vehicle);
            return ($"Added {vehicle.GetType().Name} #{vehicle.VehicleId}.\n", null);
        }

        private static Truck PromptTruck(string plate)
        {
            double load;
            while (true)
            {
                Console.Write("Max load (kg): ");
                if (double.TryParse(Console.ReadLine(), out load) && load > 0)
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid, positive number for max load.");
            }
            return new Truck(plate, load);
        }

        private static Van PromptVan(string plate)
        {
            int cap;
            while (true)
            {
                Console.Write("Package capacity: ");
                if (int.TryParse(Console.ReadLine(), out cap) && cap > 0)
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid, positive number for package capacity.");
            }
            return new Van(plate, cap);
        }

        private static Bus PromptBus(string plate)
        {
            int cap;
            while (true)
            {
                Console.Write("Passenger capacity: ");
                if (int.TryParse(Console.ReadLine(), out cap) && cap > 0)
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid, positive number for passenger capacity.");
            }
            return new Bus(plate, cap);
        }

        private static (string? success, string? error) RemoveVehicleFlow()
        {
            string? localError = null;
            VehicleTypeOption vehicleType;
            
            while (true)
            {
                Console.Clear();
                MenuHeader();
                
                if (localError != null) PrintError(localError);

                Console.WriteLine("Select Vehicle Type to remove:");
                Console.WriteLine($"{(int)VehicleTypeOption.Truck}. Truck");
                Console.WriteLine($"{(int)VehicleTypeOption.Van}. Van");
                Console.WriteLine($"{(int)VehicleTypeOption.Bus}. Bus");
                Console.Write("Choose an option: ");
                
                string choice = (Console.ReadLine() ?? "").Trim();

                if (Enum.TryParse(choice, out vehicleType) && Enum.IsDefined(vehicleType))
                {
                    break;
                }
                localError = "Invalid option. Please enter 1, 2, or 3.";
            }

            var typeMatchedVehicles = Dispatch.Fleet.Where(v => vehicleType switch
            {
                VehicleTypeOption.Truck => v is Truck,
                VehicleTypeOption.Van => v is Van,
                VehicleTypeOption.Bus => v is Bus,
                _ => false
            }).ToList();

            if (typeMatchedVehicles.Count == 0)
            {
                return (null, $"No {vehicleType}s found in the fleet.\n");
            }

            Console.WriteLine($"\n--- Available {vehicleType}s ---");
            foreach (var v in typeMatchedVehicles)
            {
                v.DisplayInfo();
            }
            Console.WriteLine("------------------------\n");

            int id;
            while (true)
            {
                Console.Write("Vehicle ID to remove: ");
                if (int.TryParse(Console.ReadLine(), out id))
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid numerical ID.");
            }

            bool removed = Dispatch.RemoveVehicle(id);
            if (removed)
            {
                return ($"Vehicle#{id} removed.\n", null);
            }
            else
            {
                return (null, $"Vehicle#{id} not found.\n");
            }
        }

        private static (string? success, string? error) AddDriverFlow()
        {
            Console.Write("Driver name: ");
            string name = Console.ReadLine() ?? "Unnamed";
            Console.Write("License code: ");
            string code = Console.ReadLine() ?? "N/A";

            var d = new Driver(name, code);
            Dispatch.AddDriver(d);
            return ($"Added driver #{d.DriverId}.\n", null);
        }

        private static (string? success, string? error) CreateRouteFlow()
        {
            string origin = PromptPlace("Select origin");
            string dest = PromptPlace("Select destination", origin);

            // FIX 3: Replaced hardcoded default with proper validation loop
            double dist;
            while (true)
            {
                Console.Write("Distance (km): ");
                if (double.TryParse(Console.ReadLine(), out dist) && dist > 0)
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid, positive number for distance.");
            }

            var priority = PromptPriority();

            var route = Dispatch.CreateRoute(origin, dest, dist, priority);
            return ($"Created {route}\n", null);
        }

        // FIX 2: Clear screen on bad inputs to avoid infinite terminal scrolling
        private static string PromptPlace(string prompt, string? excludedPlace = null)
        {
            string? localError = null;
            while (true)
            {
                Console.Clear();
                MenuHeader();

                if (localError != null) PrintError(localError);

                Console.WriteLine($"{prompt}:");
                for (int i = 0; i < PresetPlaces.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {PresetPlaces[i]}");
                }

                Console.Write("Choose a place: ");
                if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > PresetPlaces.Length)
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

        private static Priority PromptPriority()
        {
            string? localError = null;
            while (true)
            {
                Console.Clear();
                MenuHeader();

                if (localError != null) PrintError(localError);

                Console.WriteLine("Select urgency:");
                Console.WriteLine("1. Low");
                Console.WriteLine("2. Medium");
                Console.WriteLine("3. High");
                Console.WriteLine("4. Critical");
                Console.Write("Choose urgency: ");

                if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > 4)
                {
                    localError = "Invalid option. Please choose 1, 2, 3, or 4.";
                    continue;
                }

                return (Priority)(choice - 1);
            }
        }

        private static (string? success, string? error) AssignRouteFlow()
        {
            var availableRoutes = Dispatch.Routes.Where(r => r.Status == RouteStatus.Pending).ToList();
            if (availableRoutes.Count == 0)
            {
                return (null, "No pending routes available for assignment.\n");
            }

            var availableDrivers = Dispatch.Drivers.ToList();
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
                Console.Write("\nRoute ID: ");
                if (int.TryParse(Console.ReadLine(), out routeId))
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid numerical Route ID.");
            }

            int driverId;
            while (true)
            {
                Console.Write("Driver ID: ");
                if (int.TryParse(Console.ReadLine(), out driverId))
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid numerical Driver ID.");
            }

            var idleVehicles = Dispatch.Fleet.Where(v => v.Status == VehicleStatus.Idle).ToList();
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
                Console.Write("Vehicle ID: ");
                if (int.TryParse(Console.ReadLine(), out vehicleId))
                {
                    break;
                }
                PrintError("Invalid input. Please enter a valid numerical Vehicle ID.");
            }

            Dispatch.AssignRoute(routeId, vehicleId, driverId);
            return ($"Route#{routeId} assigned to Vehicle#{vehicleId} / Driver#{driverId}.\n", null);
        }

        private static void ViewRoutes()
        {
            var routes = Dispatch.Routes;
            if (routes.Count == 0)
            {
                Console.WriteLine("No routes yet.");
            }
            else
            {
                foreach (var r in routes) Console.WriteLine(r);
            }

            WaitForKey();
        }

        private static void ViewReports()
        {
            ReportService.PrintFleetSummary(Dispatch.Fleet);
            Console.WriteLine();
            ReportService.PrintLowFuelVehicles(Dispatch.Fleet);
            Console.WriteLine();
            ReportService.PrintMaintenanceDue(Dispatch.Fleet);
            Console.WriteLine();
            ReportService.PrintRoutesByPriority(Dispatch.Routes);
            Console.WriteLine();
            ReportService.PrintCompletedRoutesReport(Dispatch.Routes);

            WaitForKey();
        }

        private static (string? success, string? error) SaveStateFlow()
        {
            FileManager.SaveState(SaveFilePath, Dispatch.Fleet, Dispatch.Drivers);
            return ($"State saved to {SaveFilePath}.\n", null);
        }

        private static (string? success, string? error) LoadStateFlow()
        {
            var snapshot = FileManager.LoadState(SaveFilePath);
            Dispatch.LoadFromSnapshot(snapshot);
            return ($"State loaded from {SaveFilePath} (saved at {snapshot.SavedAt}).\n", null);
        }

        private static (string? success, string? error) ToggleMonitoring()
        {
            if (Dispatch.IsMonitoring)
            {
                Dispatch.StopMonitoring();
                return ("Background monitoring stopped.\n", null);
            }
            else
            {
                Dispatch.StartMonitoring();
                return ("Background monitoring started.\n", null);
            }
        }
    }
}