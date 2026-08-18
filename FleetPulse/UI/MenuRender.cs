using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

 namespace FleetPulse.UI
 {
    public static class MenuRender
    {
        public static void MenuHeader()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("   FleetPulse - Smart Fleet Dispatch     ");
            Console.WriteLine("=========================================\n");
            
            Program.FlushLogs(); 
        }

        public static void PrintError(string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }

        public static void PrintSuccess(string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }

        public static void WaitForKey()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
            Console.Clear();
        }

        public static void PrintMenu(string? successMsg = null, string? errorMsg = null)
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
    }
 }