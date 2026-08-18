using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

 namespace FleetPulse.UI
 {
    public static class VehicleUI
    {
        public static (string? success, string? error) AddVehicleFlow(DispatchCenter dispatch)
        {
            string? localError = null;
            VehicleTypeOption vehicleType;
            
            while (true)
            {
                Console.Clear();
                MenuRender.MenuHeader();
                
                if (localError != null) MenuRender.PrintError(localError);

                Console.WriteLine("Select Vehicle Type:");
                Console.WriteLine("0. Cancel");
                Console.WriteLine($"{(int)VehicleTypeOption.Truck}. Truck");
                Console.WriteLine($"{(int)VehicleTypeOption.Van}. Van");
                Console.WriteLine($"{(int)VehicleTypeOption.Bus}. Bus");
                Console.Write("Choose an option: ");
                
                string choice = (Console.ReadLine() ?? "").Trim();
                if (choice == "0") return (null, null);

                if (Enum.TryParse(choice, out vehicleType) && Enum.IsDefined(vehicleType) && choice != "0")
                {
                    break;
                }
                localError = "Invalid option. Please enter 0, 1, 2, or 3.";
            }

            string plate;
            while (true)
            {
                Console.Write("License plate [0 to cancel]: ");
                plate = (Console.ReadLine() ?? "").Trim();
                
                if (plate == "0") return (null, null);
                
                if (!string.IsNullOrWhiteSpace(plate))
                {
                    break;
                }
                MenuRender.PrintError("License plate cannot be empty.");
            }

            Vehicle? vehicle = vehicleType switch
            {
                VehicleTypeOption.Truck => PromptTruck(plate),
                VehicleTypeOption.Van => PromptVan(plate),
                VehicleTypeOption.Bus => PromptBus(plate),
                _ => throw new InvalidOperationException("Unexpected vehicle type.")
            };

            if (vehicle == null) return (null, null);

            dispatch.AddVehicle(vehicle);
            return ($"Added {vehicle.GetType().Name} #{vehicle.VehicleId}.\n", null);
        }

        public static Truck? PromptTruck(string plate)
        {
            double load;
            while (true)
            {
                Console.Write("Max load (kg) [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return null;

                if (double.TryParse(input, out load) && load > 0)
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid, positive number.");
            }
            return new Truck(plate, load);
        }

        public static Van? PromptVan(string plate)
        {
            int cap;
            while (true)
            {
                Console.Write("Package capacity [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return null;

                if (int.TryParse(input, out cap) && cap > 0)
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid, positive number.");
            }
            return new Van(plate, cap);
        }

        public static Bus? PromptBus(string plate)
        {
            int cap;
            while (true)
            {
                Console.Write("Passenger capacity [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return null;

                if (int.TryParse(input, out cap) && cap > 0)
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid, positive number.");
            }
            return new Bus(plate, cap);
        }

        public static (string? success, string? error) RemoveVehicleFlow(DispatchCenter dispatch)
        {
            string? localError = null;
            VehicleTypeOption vehicleType;
            
            while (true)
            {
                Console.Clear();
                MenuRender.MenuHeader();
                
                if (localError != null) MenuRender.PrintError(localError);

                Console.WriteLine("Select Vehicle Type to remove:");
                Console.WriteLine("0. Cancel");
                Console.WriteLine($"{(int)VehicleTypeOption.Truck}. Truck");
                Console.WriteLine($"{(int)VehicleTypeOption.Van}. Van");
                Console.WriteLine($"{(int)VehicleTypeOption.Bus}. Bus");
                Console.Write("Choose an option: ");
                
                string choice = (Console.ReadLine() ?? "").Trim();
                if (choice == "0") return (null, null);

                if (Enum.TryParse(choice, out vehicleType) && Enum.IsDefined(vehicleType) && choice != "0")
                {
                    break;
                }
                localError = "Invalid option. Please enter 0, 1, 2, or 3.";
            }

            var typeMatchedVehicles = dispatch.Fleet.Where(v => vehicleType switch
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
                Console.Write("Vehicle ID to remove [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return (null, null);

                if (int.TryParse(input, out id))
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid numerical ID.");
            }

            bool removed = dispatch.RemoveVehicle(id);
            if (removed)
            {
                return ($"Vehicle#{id} removed.\n", null);
            }
            else
            {
                return (null, $"Vehicle#{id} not found.\n");
            }
        }

        public static void ViewFleet(DispatchCenter dispatch)
        {
            var fleet = dispatch.Fleet;
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

            MenuRender.WaitForKey();
        }
    }
 }        