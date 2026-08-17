using System;
using System.Linq;
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
        private const string SaveFilePath = "fleetpulse_state.json";

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

            // Store the startup message to pass to the menu
            string startupMsg = "Background monitoring started (fuel, breakdowns, maintenance, auto-deliveries).\n";
            RunMenu(startupMsg);

            Dispatch.StopMonitoring();
            Console.WriteLine("Shutting down FleetPulse. Goodbye!");
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
            Console.ReadKey(true); // 'true' hides the pressed key character
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
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [{tag}] {message}");
            Console.ForegroundColor = prev;
        }

        private static void SeedRandomData()
        {
            Dispatch.AddVehicle(new Truck($"TRK-{Rng.Next(100, 999)}", Rng.Next(3000, 12000)));
            Dispatch.AddVehicle(new Van($"VAN-{Rng.Next(100, 999)}", Rng.Next(20, 80)));
            Dispatch.AddVehicle(new Bus($"BUS-{Rng.Next(100, 999)}", Rng.Next(30, 60)));

            Dispatch.AddDriver(new Driver("Thabo Nkosi", "C1-EL"));
            Dispatch.AddDriver(new Driver("Amanda van Wyk", "EC1"));

            string[] places = { "Johannesburg", "Pretoria", "Durban", "Cape Town", "Bloemfontein", "Polokwane" };
            for (int i = 0; i < 3; i++)
            {
                string origin = places[Rng.Next(places.Length)];
                string dest;
                do 
                { 

                    dest = places[Rng.Next(places.Length)];

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
                PrintMenu(currentMsg, errorMsg);
                currentMsg = null; // Clear it so the message only shows on the very first run
                errorMsg = null;

                string? choice = Console.ReadLine();

                try
                {
                    if (Enum.TryParse(choice, out MainMenuOption menuOption) && Enum.IsDefined(menuOption))
                    {
                        // Clear console and show header for all options except Exit
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
                                Console.Clear();
                                break;

                            case MainMenuOption.RemoveVehicle: 
                                (currentMsg, errorMsg) = RemoveVehicleFlow(); 
                                Console.Clear();
                                break;

                            case MainMenuOption.AddDriver: 
                                (currentMsg, errorMsg) = AddDriverFlow(); 
                                Console.Clear();
                                break;

                            case MainMenuOption.CreateRoute: 
                                (currentMsg, errorMsg) = CreateRouteFlow(); 
                                Console.Clear();
                                break;

                            case MainMenuOption.AssignRoute: 
                                (currentMsg, errorMsg) = AssignRouteFlow(); 
                                Console.Clear();
                                break;

                            case MainMenuOption.ViewRoutes: 
                                ViewRoutes(); 
                                break;

                            case MainMenuOption.ViewReports: 
                                ViewReports(); 
                                break;

                            case MainMenuOption.SaveState: 
                                (currentMsg, errorMsg) = SaveStateFlow(); 
                                Console.Clear();
                                break;

                            case MainMenuOption.LoadState: 
                                (currentMsg, errorMsg) = LoadStateFlow(); 
                                Console.Clear();
                                break;

                            case MainMenuOption.ToggleMonitoring: 
                                (currentMsg, errorMsg) = ToggleMonitoring(); 
                                Console.Clear();
                                break;

                            case MainMenuOption.ViewDrivers: 
                                ViewDrivers(); 
                                break;

                            case MainMenuOption.Exit: 
                                running = false; 
                                break;

                            default:
                                break;
                        }
                    }
                    else
                    {
                        errorMsg = "Invalid option, please choose again.\n";
                        Console.Clear();
                    }
                }
                catch (FleetCapacityExceededException ex)
                {
                    errorMsg = $"[Capacity Error] {ex.Message}\n";
                    Console.Clear();
                }
                catch (DriverHourLimitExceededException ex)
                {
                    errorMsg = $"[Driver Hours Error] {ex.Message}\n";
                    Console.Clear();
                }
                catch (InvalidRouteAssignmentException ex)
                {
                    errorMsg = $"[Assignment Error] {ex.Message}\n";
                    Console.Clear();
                }
                catch (FileNotFoundException ex)
                {
                    errorMsg = $"[File Error] {ex.Message}\n";
                    Console.Clear();
                }
                catch (FormatException)
                {
                    errorMsg = "[Input Error] Please enter a valid number where a number is expected.\n";
                    Console.Clear();
                }
                catch (Exception ex)
                {
                    // Last line of defence so a single bad action never crashes the whole console app.
                    errorMsg = $"[Unexpected Error] {ex.Message}\n";
                    Console.Clear();
                }
                finally
                {
                    // Adding spacing unless we're about to clear the console for a menu render
                    if (string.IsNullOrEmpty(currentMsg) && string.IsNullOrEmpty(errorMsg))
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        private static void PrintMenu(string? successMsg = null, string? errorMsg = null)
        {
            MenuHeader();

            // Print the startup message or returned success messages in green
            if (!string.IsNullOrEmpty(successMsg))
            {
                PrintSuccess(successMsg);
            }
            
            // Print top-level returning errors in red
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
            Console.WriteLine("Select Vehicle Type:");
            Console.WriteLine($"{(int)VehicleTypeOption.Truck}. Truck");
            Console.WriteLine($"{(int)VehicleTypeOption.Van}. Van");
            Console.WriteLine($"{(int)VehicleTypeOption.Bus}. Bus");
            
            VehicleTypeOption vehicleType;
            while (true)
            {
                Console.Write("Choose an option: ");
                string choice = (Console.ReadLine() ?? "").Trim();
                
                if (Enum.TryParse(choice, out vehicleType) && Enum.IsDefined(vehicleType))
                {
                    break;
                }
                PrintError("Invalid option. Please enter 1, 2, or 3.");
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
            Console.WriteLine("Select Vehicle Type to remove:");
            Console.WriteLine($"{(int)VehicleTypeOption.Truck}. Truck");
            Console.WriteLine($"{(int)VehicleTypeOption.Van}. Van");
            Console.WriteLine($"{(int)VehicleTypeOption.Bus}. Bus");
            
            VehicleTypeOption vehicleType;
            while (true)
            {
                Console.Write("Choose an option: ");
                string choice = (Console.ReadLine() ?? "").Trim();
                
                if (Enum.TryParse(choice, out vehicleType) && Enum.IsDefined(vehicleType))
                {
                    break;
                }
                PrintError("Invalid option. Please enter 1, 2, or 3.");
            }

            // Filter the fleet to only include the chosen vehicle type
            var typeMatchedVehicles = Dispatch.Fleet.Where(v => vehicleType switch
            {
                VehicleTypeOption.Truck => v is Truck,
                VehicleTypeOption.Van => v is Van,
                VehicleTypeOption.Bus => v is Bus,
                _ => false
            }).ToList();

            if (typeMatchedVehicles.Count == 0)
            {
                // Returns an error message back to the top of the menu
                return (null, $"No {vehicleType}s found in the fleet.\n");
            }

            Console.WriteLine($"\n--- Available {vehicleType}s ---");
            foreach (var v in typeMatchedVehicles)
            {
                v.DisplayInfo(); // This will print details including the Vehicle ID
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
                // Returns a success message back to the top of the menu
                return ($"Vehicle#{id} removed.\n", null);
            }
            else
            {
                // Returns an error message back to the top of the menu
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
            Console.Write("Origin: ");
            string origin = Console.ReadLine() ?? "Origin";
            Console.Write("Destination: ");
            string dest = Console.ReadLine() ?? "Destination";
            Console.Write("Distance (km): ");
            double dist = double.Parse(Console.ReadLine() ?? "100");
            Console.Write("Priority (0=Low, 1=Medium, 2=High, 3=Critical): ");
            var priority = (Priority)int.Parse(Console.ReadLine() ?? "1");

            var route = Dispatch.CreateRoute(origin, dest, dist, priority);
            return ($"Created {route}\n", null);
        }

        private static (string? success, string? error) AssignRouteFlow()
        {
            Console.Write("Route ID: ");
            int routeId = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("Vehicle ID: ");
            int vehicleId = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("Driver ID: ");
            int driverId = int.Parse(Console.ReadLine() ?? "0");

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