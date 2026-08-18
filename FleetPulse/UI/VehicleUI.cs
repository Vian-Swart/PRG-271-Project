using System;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;
using FleetPulse.Services;

namespace FleetPulse.UI
{
    /// <summary>
    /// Handles user interaction for vehicle management operations in the console UI.
    /// Provides workflows for adding, removing, and viewing vehicles in the dispatch fleet.
    /// </summary>
    public static class VehicleUI
    {
        /// <summary>
        /// Guides the user through a step-by-step console workflow to create and add a new vehicle to the dispatch center.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        /// <returns>A tuple containing a success message or an error message if the flow fails or is canceled.</returns>
        public static (string? success, string? error) AddVehicleFlow(DispatchCenter dispatch)
        {
            string? localError = null;
            VehicleTypeOption vehicleType;
            
            // Step 1: Prompt user to choose the type of vehicle to create
            while (true)
            {
                Console.Clear();
                MenuRender.MenuHeader();
                
                // Display error from previous failed selection attempt, if any
                if (localError != null) MenuRender.PrintError(localError);

                Console.WriteLine("Select Vehicle Type:");
                Console.WriteLine("0. Cancel");
                Console.WriteLine($"{(int)VehicleTypeOption.Truck}. Truck");
                Console.WriteLine($"{(int)VehicleTypeOption.Van}. Van");
                Console.WriteLine($"{(int)VehicleTypeOption.Bus}. Bus");
                Console.Write("Choose an option: ");
                
                string choice = (Console.ReadLine() ?? "").Trim();
                
                // User requested cancellation
                if (choice == "0") return (null, null);

                // Validate menu choice against defined enum values
                if (Enum.TryParse(choice, out vehicleType) && Enum.IsDefined(vehicleType) && choice != "0")
                {
                    break;
                }
                localError = "Invalid option. Please enter 0, 1, 2, or 3.";
            }

            // Step 2: Prompt for license plate string
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

            // Step 3: Delegate type-specific prompts (load, package capacity, or passenger count)
            Vehicle? vehicle = vehicleType switch
            {
                VehicleTypeOption.Truck => PromptTruck(plate),
                VehicleTypeOption.Van => PromptVan(plate),
                VehicleTypeOption.Bus => PromptBus(plate),
                _ => throw new InvalidOperationException("Unexpected vehicle type.")
            };

            // If prompt was canceled at sub-level
            if (vehicle == null) return (null, null);

            // Step 4: Register the new vehicle with the dispatch center
            dispatch.AddVehicle(vehicle);
            return ($"Added {vehicle.GetType().Name} #{vehicle.VehicleId}.\n", null);
        }

        /// <summary>
        /// Prompts the user for Truck-specific properties and instantiates a Truck model.
        /// </summary>
        /// <param name="plate">The license plate number for the truck.</param>
        /// <returns>A new <see cref="Truck"/> instance, or null if canceled.</returns>
        public static Truck? PromptTruck(string plate)
        {
            double load;
            while (true)
            {
                Console.Write("Max load (kg) [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return null;

                // Validate that max load is a positive numeric value
                if (double.TryParse(input, out load) && load > 0)
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid, positive number.");
            }
            return new Truck(plate, load);
        }

        /// <summary>
        /// Prompts the user for Van-specific properties and instantiates a Van model.
        /// </summary>
        /// <param name="plate">The license plate number for the van.</param>
        /// <returns>A new <see cref="Van"/> instance, or null if canceled.</returns>
        public static Van? PromptVan(string plate)
        {
            int cap;
            while (true)
            {
                Console.Write("Package capacity [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return null;

                // Validate that package capacity is a positive integer
                if (int.TryParse(input, out cap) && cap > 0)
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid, positive number.");
            }
            return new Van(plate, cap);
        }

        /// <summary>
        /// Prompts the user for Bus-specific properties and instantiates a Bus model.
        /// </summary>
        /// <param name="plate">The license plate number for the bus.</param>
        /// <returns>A new <see cref="Bus"/> instance, or null if canceled.</returns>
        public static Bus? PromptBus(string plate)
        {
            int cap;
            while (true)
            {
                Console.Write("Passenger capacity [0 to cancel]: ");
                string input = (Console.ReadLine() ?? "").Trim();
                if (input == "0") return null;

                // Validate that passenger capacity is a positive integer
                if (int.TryParse(input, out cap) && cap > 0)
                {
                    break;
                }
                MenuRender.PrintError("Invalid input. Please enter a valid, positive number.");
            }
            return new Bus(plate, cap);
        }

        /// <summary>
        /// Guides the user through a console workflow to locate and remove a vehicle by its ID.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        /// <returns>A tuple containing a success message or an error message if the removal fails or is canceled.</returns>
        public static (string? success, string? error) RemoveVehicleFlow(DispatchCenter dispatch)
        {
            string? localError = null;
            VehicleTypeOption vehicleType;
            
            // Step 1: Select vehicle category to filter the list
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

            // Step 2: Filter existing fleet vehicles by requested type using LINQ pattern matching
            var typeMatchedVehicles = dispatch.Fleet.Where(v => vehicleType switch
            {
                VehicleTypeOption.Truck => v is Truck,
                VehicleTypeOption.Van => v is Van,
                VehicleTypeOption.Bus => v is Bus,
                _ => false
            }).ToList();

            // Early exit if no matching vehicles are currently registered
            if (typeMatchedVehicles.Count == 0)
            {
                return (null, $"No {vehicleType}s found in the fleet.\n");
            }

            // Step 3: Print candidate vehicles to assist user selection
            Console.WriteLine($"\n--- Available {vehicleType}s ---");
            foreach (var v in typeMatchedVehicles)
            {
                v.DisplayInfo();
            }
            Console.WriteLine("------------------------\n");

            // Step 4: Prompt for target vehicle ID
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

            // Step 5: Attempt removal through dispatch service
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

        /// <summary>
        /// Displays information for all vehicles currently registered in the fleet.
        /// </summary>
        /// <param name="dispatch">The active dispatch center instance.</param>
        public static void ViewFleet(DispatchCenter dispatch)
        {
            var fleet = dispatch.Fleet;
            if (fleet.Count == 0)
            {
                Console.WriteLine("Fleet is empty.");
            }
            else
            {
                // Polymorphically invoke DisplayInfo on each vehicle type
                foreach (var v in fleet)
                {
                    v.DisplayInfo();
                }
            }

            // Pause until user acknowledges before clearing/returning to main menu
            MenuRender.WaitForKey();
        }
    }
}