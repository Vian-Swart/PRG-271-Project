using System;
using System.Collections.Generic;
using System.Linq;
using FleetPulse.Enums;
using FleetPulse.Models;

namespace FleetPulse.Services
{
    /// <summary>
    /// Provides read-only LINQ analytical queries over fleet and route collections 
    /// to generate management reports and operational summaries.
    /// </summary>
    public static class ReportService
    {
        /// <summary>
        /// Filters and displays vehicles whose current fuel percentage falls below a specified threshold.
        /// </summary>
        /// <param name="fleet">The collection of fleet vehicles to analyze.</param>
        /// <param name="threshold">The fuel percentage cutoff value (default is 20%).</param>
        public static void PrintLowFuelVehicles(IEnumerable<Vehicle> fleet, double threshold = 20)
        {
            // LINQ: Filter vehicles under fuel threshold and sort by lowest fuel first
            var lowFuel = fleet.Where(v => v.FuelLevel < threshold)
                               .OrderBy(v => v.FuelLevel)
                               .ToList();

            Console.WriteLine($"--- Vehicles below {threshold:F0}% fuel ---");
            
            // Empty result check
            if (lowFuel.Count == 0) 
            { 
                Console.WriteLine("None."); 
                return; 
            }

            foreach (var v in lowFuel) v.DisplayInfo();
        }

        /// <summary>
        /// Identifies and renders all fleet vehicles that meet or exceed their scheduled service intervals.
        /// </summary>
        /// <param name="fleet">The collection of fleet vehicles to evaluate.</param>
        public static void PrintMaintenanceDue(IEnumerable<Vehicle> fleet)
        {
            // LINQ: Filter by maintenance status and sort by highest mileage first
            var due = fleet.Where(v => v.IsDueForService())
                           .OrderByDescending(v => v.Mileage)
                           .ToList();

            Console.WriteLine("--- Vehicles due for maintenance ---");
            
            // Empty result check
            if (due.Count == 0) 
            { 
                Console.WriteLine("None."); 
                return; 
            }

            foreach (var v in due) v.DisplayInfo();
        }

        /// <summary>
        /// Generates an aggregated breakdown of fleet composition grouped by vehicle type,
        /// including total counts and average fuel levels per category.
        /// </summary>
        /// <param name="fleet">The collection of fleet vehicles to summarize.</param>
        public static void PrintFleetSummary(IEnumerable<Vehicle> fleet)
        {
            // LINQ: Group runtime concrete types and project aggregate metrics (Count, Average)
            var byType = fleet.GroupBy(v => v.GetType().Name)
                              .Select(g => new { Type = g.Key, Count = g.Count(), AvgFuel = g.Average(v => v.FuelLevel) })
                              .OrderBy(g => g.Type)
                              .ToList();

            Console.WriteLine("--- Fleet summary by type ---");
            
            // Empty result check
            if (byType.Count == 0) 
            { 
                Console.WriteLine("Fleet is empty."); 
                return; 
            }

            foreach (var g in byType)
                Console.WriteLine($"{g.Type,-6}: {g.Count} vehicle(s), avg fuel {g.AvgFuel:F1}%");
        }

        /// <summary>
        /// Displays registered routes ordered primarily by priority level (highest urgency first)
        /// and secondarily by route ID.
        /// </summary>
        /// <param name="routes">The collection of route jobs to display.</param>
        public static void PrintRoutesByPriority(IEnumerable<RouteJob> routes)
        {
            // LINQ: Multi-level sort (Descending Priority -> Ascending Route ID)
            var ordered = routes.OrderByDescending(r => r.Priority)
                                .ThenBy(r => r.RouteId)
                                .ToList();

            Console.WriteLine("--- Routes (highest priority first) ---");
            
            // Empty result check
            if (ordered.Count == 0) 
            { 
                Console.WriteLine("None."); 
                return; 
            }

            foreach (var r in ordered) Console.WriteLine(r);
        }

        /// <summary>
        /// Summarizes metrics for completed route jobs, including total completed count 
        /// and cumulative distance traveled.
        /// </summary>
        /// <param name="routes">The collection of route jobs to aggregate.</param>
        public static void PrintCompletedRoutesReport(IEnumerable<RouteJob> routes)
        {
            // LINQ: Filter completed routes and calculate sum of distances driven
            var completed = routes.Where(r => r.Status == RouteStatus.Completed).ToList();
            double totalKm = completed.Sum(r => r.DistanceKm);

            Console.WriteLine($"--- Completed routes: {completed.Count} ---");
            Console.WriteLine($"Total distance covered: {totalKm:F1} km");
        }
    }
}