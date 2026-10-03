// Sales Data Tracker Application
// Author: [Evan Barrett]

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace SalesTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            // Menu runs on a loop until option 4 is selected
            for (bool running = true; running; )
            {
                Console.WriteLine("\n=== SALES TRACKER MENU ===");
                Console.WriteLine("1. Create New Goal Save File");
                Console.WriteLine("2. Log Today's Sales & Hours");
                Console.WriteLine("3. View Analytics & Pacing");
                Console.WriteLine("4. Quit");
                Console.Write("Select an option (1-4): ");
                
                string choice = Console.ReadLine()?.Trim() ?? "";
                switch (choice)
                {
                    case "1": CreateNewSaveFile(); break;
                    case "2": LogDailyEntry(); break;
                    case "3": ViewAnalytics(); break;
                    case "4": running = false; break;
                    default: Console.WriteLine("Invalid option. Please try again."); break;
                }
            }
            Console.WriteLine("Go crush those goals!");
        }

        // Creates a new save file with user's goals and working days
        static void CreateNewSaveFile()
        {
            Console.Write("\nEnter file name (e.g., summer_goal.csv): ");
            string fileName = Console.ReadLine()?.Trim() ?? "sales_data.csv";
            if (!fileName.EndsWith(".csv")) fileName += ".csv";

            Console.Write("Enter your total sales goal (count): ");
            int.TryParse(Console.ReadLine(), out int targetSales);

            Console.Write("Enter total number of working/knocking days: ");
            int.TryParse(Console.ReadLine(), out int totalDays);

            // Store goal on line 1, column headers on line 2
            using (StreamWriter sw = new StreamWriter(fileName, false))
            {
                sw.WriteLine($"#GOAL,{targetSales},{totalDays}");
                sw.WriteLine("Date,HoursWorked,SalesCount");
            }

            Console.WriteLine($"\nFile '{fileName}' created successfully with a goal of {targetSales} sales over {totalDays} days.");
        }

        // Logs a daily entry for a specified save file
        static void LogDailyEntry()
        {
            Console.Write("\nEnter file name to load: ");
            string fileName = Console.ReadLine()?.Trim() ?? "";
            if (!File.Exists(fileName))
            {
                Console.WriteLine("File not found! Returning to menu.");
                return;
            }

            Console.Write("Enter hours worked today: ");
            double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double hours);

            Console.Write("Enter number of sales closed today: ");
            int.TryParse(Console.ReadLine(), out int sales);
            // Get current date for the log entry
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            // Append daily record
            using (StreamWriter sw = new StreamWriter(fileName, true))
            {
                sw.WriteLine($"{dateStr},{hours.ToString(CultureInfo.InvariantCulture)},{sales}");
            }
            // Display confirmation of the saved file
            Console.WriteLine($"Logged: {sales} sales in {hours} hrs on {dateStr}. Auto-saved to '{fileName}'.");
        }

        // Views sales statistics for a save file
        static void ViewAnalytics()
        {
            Console.Write("\nEnter file name to analyze: ");
            string fileName = Console.ReadLine()?.Trim() ?? "";
            if (!File.Exists(fileName))
            {
                Console.WriteLine("File not found! Returning to menu.");
                return;
            }

            string[] lines = File.ReadAllLines(fileName);
            if (lines.Length < 2 || !lines[0].StartsWith("#GOAL"))
            {
                Console.WriteLine("Invalid or empty save file structure.");
                return;
            }
            
            // Split header data
            string[] goalMeta = lines[0].Split(',');
            int targetSales = int.Parse(goalMeta[1]);
            int initialDays = int.Parse(goalMeta[2]);

            // Parse logged entries
            var entries = new List<(string Date, double Hours, int Sales)>();
            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] parts = lines[i].Split(',');
                if (parts.Length == 3 &&
                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double h) &&
                    int.TryParse(parts[2], out int s))
                {
                    entries.Add((parts[0], h, s));
                }
            }

            // Pacing & Days Left calculations (x - 1 per entry)
            int daysWorked = entries.Count;
            int remainingDays = Math.Max(0, initialDays - daysWorked);
            int totalSales = entries.Sum(e => e.Sales);
            double totalHours = entries.Sum(e => e.Hours);
            int remainingSales = Math.Max(0, targetSales - totalSales);

            // Sales Per Hour
            double overallSalesPerHour = totalHours > 0 ? (double)totalSales / totalHours : 0.0;

            // Rolling 7-day sales per hour
            var recentEntries = entries.TakeLast(7).ToList();
            double recentHours = recentEntries.Sum(e => e.Hours);
            int recentSales = recentEntries.Sum(e => e.Sales);
            double rollingSalesPerHour = recentHours > 0 ? (double)recentSales / recentHours : overallSalesPerHour;

            // Required sales per day calculations
            double requiredSalesPerDay = remainingDays > 0 ? (double)remainingSales / remainingDays : remainingSales;

            // Dynamic Hours Estimator
            double recommendedHoursDaily = 0.0;
            if (remainingDays > 0 && rollingSalesPerHour > 0)
            {
                recommendedHoursDaily = remainingSales / (remainingDays * rollingSalesPerHour);
            }

            // Output Dashboard
            Console.WriteLine("\n================ PROGRESS REPORT ================");
            Console.WriteLine($"Progress:                {totalSales} / {targetSales} sales ({(targetSales > 0 ? (totalSales * 100.0 / targetSales) : 0):F1}%)");
            Console.WriteLine($"Knocking Days Left:      {remainingDays} days (Worked: {daysWorked}/{initialDays})");
            Console.WriteLine($"Total Hours Knocked:     {totalHours:F1} hrs");
            Console.WriteLine($"Closing Rate (Overall):  {overallSalesPerHour:F2} sales/hr");
            Console.WriteLine($"Closing Rate (Recent):   {rollingSalesPerHour:F2} sales/hr");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine($"Pace Needed:             {requiredSalesPerDay:F2} sales/day");
            if (rollingSalesPerHour > 0)
            {
                Console.WriteLine($"Dynamic Hours Needed:    ~{recommendedHoursDaily:F1} hrs/day (based on recent pace)");
            }
            else
            {
                Console.WriteLine("Dynamic Hours Needed:    Log more sales/hours to estimate required hours.");
            }
            Console.WriteLine("=================================================");
        }
    }
}