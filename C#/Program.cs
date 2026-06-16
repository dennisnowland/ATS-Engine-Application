using AtsSystem.Engine;
using AtsSystem.Models;
using AtsSystem.Rules;
using AtsSystem.Storage;

namespace AtsSystem
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║         ATS MASTER RULE ENGINE  v1.0                ║");
            Console.WriteLine("║         Version: 16 June 2026                       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();

            var storage = new JobStorage();
            var engine  = new AtsEngine(storage);

            bool running = true;
            while (running)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("MENU");
                Console.ResetColor();
                Console.WriteLine("  1. Paste Job Description  (Compare Rule)");
                Console.WriteLine("  2. Record Submission       (Submission Rule)");
                Console.WriteLine("  3. Record Rejection        (Failure Rule)");
                Console.WriteLine("  4. Record Interview        (Interview Rule)");
                Console.WriteLine("  5. Import Failures CSV");
                Console.WriteLine("  6. Import Interviews CSV");
                Console.WriteLine("  7. View Main Table");
                Console.WriteLine("  8. View Under-93 Table");
                Console.WriteLine("  9. View Daily Summary Log");
                Console.WriteLine("  0. Exit");
                Console.Write("> ");

                var choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("Paste job description (end with a line containing only END):");
                        var jd = ReadMultiline();
                        await engine.RunCompareRule(jd);
                        break;

                    case "2":
                        Console.WriteLine("Paste submission confirmation email (end with END):");
                        var sub = ReadMultiline();
                        engine.RunSubmissionRule(sub);
                        break;

                    case "3":
                        Console.WriteLine("Paste rejection email (end with END):");
                        var rej = ReadMultiline();
                        engine.RunFailureRule(rej);
                        break;

                    case "4":
                        Console.WriteLine("Paste interview invite (end with END):");
                        var inv = ReadMultiline();
                        engine.RunInterviewRule(inv);
                        break;

                    case "5":
                        Console.Write("Path to failures.csv: ");
                        var fp = Console.ReadLine()?.Trim() ?? "";
                        engine.ImportFailuresCsv(fp);
                        break;

                    case "6":
                        Console.Write("Path to interviews.csv: ");
                        var ip = Console.ReadLine()?.Trim() ?? "";
                        engine.ImportInterviewsCsv(ip);
                        break;

                    case "7":
                        storage.PrintTable(TableType.Main);
                        break;

                    case "8":
                        storage.PrintTable(TableType.Under93);
                        break;

                    case "9":
                        storage.PrintDailySummary();
                        break;

                    case "0":
                        running = false;
                        break;

                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }

            Console.WriteLine("Goodbye!");
        }

        static string ReadMultiline()
        {
            var lines = new List<string>();
            string? line;
            while ((line = Console.ReadLine()) != null && line.Trim() != "END")
                lines.Add(line);
            return string.Join("\n", lines);
        }
    }
}
