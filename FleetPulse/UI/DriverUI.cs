using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

namespace FleetPulse.UI
{
    public static class DriverUI
    {
        public static (string? success, string? error) AddDriverFlow(DispatchCenter dispatch)
        {
            Console.Write("Driver name [0 to cancel]: ");
            string name = (Console.ReadLine() ?? "").Trim();
            if (name == "0") return (null, null);
            if (string.IsNullOrWhiteSpace(name)) name = "Unnamed";

            Console.Write("License code [0 to cancel]: ");
            string code = (Console.ReadLine() ?? "").Trim();
            if (code == "0") return (null, null);
            if (string.IsNullOrWhiteSpace(code)) code = "N/A";

            var d = new Driver(name, code);
            dispatch.AddDriver(d);
            return ($"Added driver #{d.DriverId}.\n", null);
        }

        public static void ViewDrivers(DispatchCenter dispatch)
        {
            var drivers = dispatch.Drivers;
            if (drivers.Count == 0)
            {
                Console.WriteLine("No drivers on record.");
            }
            else
            {
                foreach (var d in drivers) Console.WriteLine(d);
            }

            MenuRender.WaitForKey();
        }
    }
}        