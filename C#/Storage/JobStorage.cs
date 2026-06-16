using AtsSystem.Models;
using System.Text.Json;

namespace AtsSystem.Storage
{
    public class JobStorage
    {
        private readonly List<JobEntry>     _jobs    = new();
        private readonly List<SummaryEntry> _summary = new();

        private const string JobsFile    = "jobs.json";
        private const string SummaryFile = "daily_summary.log";

        public JobStorage()
        {
            Load();
        }

        // ── Lookup ──────────────────────────────────────────────────────────

        public JobEntry? FindDuplicate(string company, string role)
        {
            var c = company.ToLower().Trim();
            var r = role.ToLower().Trim();
            return _jobs.FirstOrDefault(j =>
                j.Company.ToLower().Trim() == c &&
                j.Role.ToLower().Trim()    == r);
        }

        public IEnumerable<JobEntry> GetByTable(TableType table) =>
            _jobs.Where(j => j.Table == table).OrderByDescending(j => j.FitScore);

        // ── Mutations ───────────────────────────────────────────────────────

        public void AddJob(JobEntry job)
        {
            _jobs.Add(job);
            Save();
        }

        public void UpdateJob(JobEntry job)
        {
            job.UpdatedAt = DateTime.Now;
            Save();
        }

        public void MoveToMain(JobEntry job)
        {
            job.Table     = TableType.Main;
            job.UpdatedAt = DateTime.Now;
            Save();
        }

        // ── Summary log ─────────────────────────────────────────────────────

        public void LogSummary(SummaryEntry entry)
        {
            _summary.Add(entry);
            File.AppendAllText(SummaryFile,
                $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} | {entry.Action,-20} | " +
                $"{entry.Company,-25} | {entry.Role,-35} | Fit:{entry.FitScore,3}% | " +
                $"Table:{entry.Table,-7} | CV:{entry.CvChosen,-30} | {entry.Notes}\n");
        }

        // ── Display ─────────────────────────────────────────────────────────

        public void PrintTable(TableType table)
        {
            var rows = GetByTable(table).ToList();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n{'─',60}");
            Console.WriteLine($"  {table.ToString().ToUpper()} TABLE  ({rows.Count} entries)");
            Console.WriteLine($"{'─',60}");
            Console.ResetColor();

            if (!rows.Any()) { Console.WriteLine("  (empty)"); return; }

            foreach (var j in rows)
            {
                Console.ForegroundColor = j.FitScore >= 93 ? ConsoleColor.Green : ConsoleColor.White;
                Console.WriteLine($"  [{j.FitScore,3}%] {j.Company,-28} | {j.Role,-35} | {j.Status,-12} | {j.CvChosen}");
                Console.ResetColor();
                if (!string.IsNullOrEmpty(j.Notes))
                    Console.WriteLine($"         Notes: {j.Notes[..Math.Min(j.Notes.Length, 120)]}");
            }
        }

        public void PrintDailySummary()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n── DAILY SUMMARY LOG ──");
            Console.ResetColor();

            if (!File.Exists(SummaryFile))
            { Console.WriteLine("  (no log yet)"); return; }

            Console.WriteLine(File.ReadAllText(SummaryFile));
        }

        // ── Persistence ─────────────────────────────────────────────────────

        private void Save()
        {
            var json = JsonSerializer.Serialize(_jobs,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(JobsFile, json);
        }

        private void Load()
        {
            if (!File.Exists(JobsFile)) return;
            try
            {
                var json = File.ReadAllText(JobsFile);
                var loaded = JsonSerializer.Deserialize<List<JobEntry>>(json);
                if (loaded != null) _jobs.AddRange(loaded);
            }
            catch { /* first run or corrupt file */ }
        }
    }
}
