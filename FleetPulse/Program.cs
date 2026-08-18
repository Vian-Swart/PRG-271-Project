using System;
using System.Collections.Concurrent;
using System.IO;
using FleetPulse.Enums;
using FleetPulse.Exceptions;
using FleetPulse.Models;
using FleetPulse.Services;
using FleetPulse.UI;

namespace FleetPulse
{
    public static class Program
    {
        private static readonly DispatchCenter Dispatch = new();
        private static readonly Random Rng = new();
        
        private const string SaveFilePath = "fleetpulse_state.json";

        private static readonly ConcurrentQueue<(ConsoleColor color, string tag, string message)> EventLogs = new();

        public static void Main(string[] args)
        {
            SubscribeToEvents();
            SeedRandomData();
            Dispatch.StartMonitoring();

            string startupMsg = "Background monitoring started (fuel, breakdowns, maintenance, auto-deliveries).\n";
            RunMenu(startupMsg);

            Dispatch.StopMonitoring();
            Console.Clear();
            MenuRender.PrintSuccess("Shutting down FleetPulse. Goodbye!");
        }

        // ---------------- Console Helper Methods ----------------

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
            EventLogs.Enqueue((color, tag, message));
        }

        public static void FlushLogs()
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
                Console.WriteLine(); 
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
                string origin = RouteUI.PresetPlaces[Rng.Next(RouteUI.PresetPlaces.Length)];
                string dest;
                do
                {
                    dest = RouteUI.PresetPlaces[Rng.Next(RouteUI.PresetPlaces.Length)];
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
                
                MenuRender.PrintMenu(currentMsg, errorMsg);
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
                            MenuRender.MenuHeader();
                        }

                        switch (menuOption)
                        {
                            case MainMenuOption.ViewFleet:
                                VehicleUI.ViewFleet(Dispatch);
                                break;
                            case MainMenuOption.AddVehicle:
                                (currentMsg, errorMsg) = VehicleUI.AddVehicleFlow(Dispatch);
                                break;
                            case MainMenuOption.RemoveVehicle:
                                (currentMsg, errorMsg) = VehicleUI.RemoveVehicleFlow(Dispatch);
                                break;
                            case MainMenuOption.AddDriver:
                                (currentMsg, errorMsg) = DriverUI.AddDriverFlow(Dispatch);
                                break;
                            case MainMenuOption.CreateRoute:
                                (currentMsg, errorMsg) = RouteUI.CreateRouteFlow(Dispatch, RouteUI.PresetPlaces);
                                break;
                            case MainMenuOption.AssignRoute:
                                (currentMsg, errorMsg) = RouteUI.AssignRouteFlow(Dispatch);
                                break;
                            case MainMenuOption.ViewRoutes:
                                RouteUI.ViewRoutes(Dispatch);
                                break;
                            case MainMenuOption.ViewReports:
                                RouteUI.ViewReports(Dispatch);
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
                                DriverUI.ViewDrivers(Dispatch);
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

        // ---------------- Menu actions ----------------

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