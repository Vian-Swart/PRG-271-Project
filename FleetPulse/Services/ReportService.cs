using FleetPulse.Models;

namespace FleetPulse.Services
{
    /// <summary>Bonus feature: read-only LINQ queries over fleet/route data for management reports.</summary>
    public static class ReportService
    {
        public static void PrintLowFuelVehicles(IEnumerable<Vehicle> fleet, double threshold = 20)
        {
            var lowFuel = fleet.Where(v => v.FuelLevel < threshold)
                                .OrderBy(v => v.FuelLevel)
                                .ToList();

            Console.WriteLine($"--- Vehicles below {threshold:F0}% fuel ---");
            if (lowFuel.Count == 0) { Console.WriteLine("None."); return; }
            foreach (var v in lowFuel) v.DisplayInfo();
        }

        public static void PrintMaintenanceDue(IEnumerable<Vehicle> fleet)
        {
            var due = fleet.Where(v => v.IsDueForService())
                            .OrderByDescending(v => v.Mileage)
                            .ToList();

            Console.WriteLine("--- Vehicles due for maintenance ---");
            if (due.Count == 0) { Console.WriteLine("None."); return; }
            foreach (var v in due) v.DisplayInfo();
        }

        public static void PrintFleetSummary(IEnumerable<Vehicle> fleet)
        {
            var byType = fleet.GroupBy(v => v.GetType().Name)
                               .Select(g => new { Type = g.Key, Count = g.Count(), AvgFuel = g.Average(v => v.FuelLevel) })
                               .OrderBy(g => g.Type)
                               .ToList();

            Console.WriteLine("--- Fleet summary by type ---");
            if (byType.Count == 0) { Console.WriteLine("Fleet is empty."); return; }
            foreach (var g in byType)
                Console.WriteLine($"{g.Type,-6}: {g.Count} vehicle(s), avg fuel {g.AvgFuel:F1}%");
        }

        public static void PrintRoutesByPriority(IEnumerable<RouteJob> routes)
        {
            var ordered = routes.OrderByDescending(r => r.Priority)
                                 .ThenBy(r => r.RouteId)
                                 .ToList();

            Console.WriteLine("--- Routes (highest priority first) ---");
            if (ordered.Count == 0) { Console.WriteLine("None."); return; }
            foreach (var r in ordered) Console.WriteLine(r);
        }

        public static void PrintCompletedRoutesReport(IEnumerable<RouteJob> routes)
        {
            var completed = routes.Where(r => r.Status == RouteStatus.Completed).ToList();
            double totalKm = completed.Sum(r => r.DistanceKm);

            Console.WriteLine($"--- Completed routes: {completed.Count} ---");
            Console.WriteLine($"Total distance covered: {totalKm:F1} km");
        }
    }
}
