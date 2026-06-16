using AtsSystem.Models;
using AtsSystem.Rules;
using AtsSystem.Storage;
using System.Text.RegularExpressions;

namespace AtsSystem.Engine
{
    /// <summary>
    /// Orchestrates all 12 ATS Master Rules.
    /// </summary>
    public class AtsEngine
    {
        private readonly JobStorage _storage;

        public AtsEngine(JobStorage storage) => _storage = storage;

        // ─────────────────────────────────────────────────────────────────
        // RULE 1 — COMPARE RULE
        // ─────────────────────────────────────────────────────────────────
        public async Task RunCompareRule(string text)
        {
            // Rule 11 & 12: if text looks like a URL, fetch it first
            if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                Console.WriteLine($"  🌐 URL detected — fetching: {uri}");
                text = await FetchUrl(uri.ToString());
                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("  ❌ Could not fetch URL content. Please paste the job description directly.");
                    return;
                }
            }

            Print("🧠 COMPARE RULE", ConsoleColor.Cyan);

            // 1.1 Extract metadata
            var job = MetadataExtractor.Extract(text);
            MetadataExtractor.PromptMissingFields(job);

            // 1.2 Fit score
            job.FitScore = FitScoreCalculator.Calculate(text);
            var breakdown = FitScoreCalculator.Breakdown(text);

            // 1.5 CV selection
            var (cv, cvReason) = CvSelector.Select($"{job.Role} {text}");
            job.CvChosen = cv;

            // 1.4 Duplicate detection
            var existing = _storage.FindDuplicate(job.Company, job.Role);
            if (existing != null)
            {
                existing.Duplicates++;
                _storage.UpdateJob(existing);
                Print($"  ⚠️  Duplicate detected — {job.Company} / {job.Role}. Duplicate count: {existing.Duplicates}", ConsoleColor.Yellow);
                return;
            }

            // 1.3 Table routing
            job.Table  = job.FitScore >= 93 ? TableType.Main : TableType.Under93;
            job.Status = job.FitScore >= 93 ? AppStatus.HighPriority : AppStatus.LowPriority;

            // 1.6 Auto-generated notes
            job.Notes = BuildCompareNotes(job, breakdown, cvReason, text);

            // Under93 rule label
            if (job.Table == TableType.Under93)
                job.Notes += " | Under 93 Rule Applied";

            // 1.7/1.8 Add to storage
            _storage.AddJob(job);

            // Output results
            PrintJobResult(job);
            Print($"\n  📊 Score Breakdown:\n  {breakdown.Replace("|", "\n  ")}", ConsoleColor.White);

            // 9 Best CV output
            Print($"\n  📄 Best CV to use: {cv}", ConsoleColor.Green);
            Print($"     Reason: {cvReason}", ConsoleColor.White);

            // Rule 7 — Cover letter
            if (job.FitScore >= 93)
            {
                Print("\n  ✉️  Fit Score ≥ 93% — Generating cover letter…", ConsoleColor.Green);
                var letter = CoverLetterGenerator.Generate(job, text);
                CoverLetterGenerator.SaveToFile(job, letter);
                job.Notes += " | Cover letter generated automatically";
                _storage.UpdateJob(job);
                Console.WriteLine(letter);
            }

            // Rule 8 — Daily summary
            _storage.LogSummary(new SummaryEntry
            {
                Action   = "Job Added",
                Company  = job.Company,
                Role     = job.Role,
                FitScore = job.FitScore,
                Table    = job.Table,
                CvChosen = job.CvChosen,
                Notes    = $"Score:{job.FitScore}% Table:{job.Table}"
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // RULE 2 — SUBMISSION RULE
        // ─────────────────────────────────────────────────────────────────
        public void RunSubmissionRule(string emailText)
        {
            Print("🧠 SUBMISSION RULE", ConsoleColor.Cyan);

            var (company, role) = ExtractCompanyRole(emailText);
            var source          = DetectSource(emailText);

            var existing = _storage.FindDuplicate(company, role);
            if (existing != null)
            {
                existing.Status      = AppStatus.Submitted;
                existing.Duplicates += existing.Status == AppStatus.Submitted ? 1 : 0;
                existing.Notes      += $" | Submission recorded {DateTime.Now:yyyy-MM-dd HH:mm} via {source}";
                _storage.UpdateJob(existing);
                Print($"  ✅ Updated existing entry: {company} / {role} → Submitted", ConsoleColor.Green);
            }
            else
            {
                var job = new JobEntry
                {
                    Company  = company,
                    Role     = role,
                    Source   = source,
                    Status   = AppStatus.Submitted,
                    Table    = TableType.Under93,
                    Notes    = $"Submission recorded {DateTime.Now:yyyy-MM-dd HH:mm} via {source}"
                };
                _storage.AddJob(job);
                Print($"  ✅ New entry created in Under93: {company} / {role} → Submitted", ConsoleColor.Green);
            }

            _storage.LogSummary(new SummaryEntry
            {
                Action  = "Submission",
                Company = company,
                Role    = role,
                Notes   = $"via {source}"
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // RULE 3 — FAILURE RULE
        // ─────────────────────────────────────────────────────────────────
        public void RunFailureRule(string emailText)
        {
            Print("🧠 FAILURE RULE", ConsoleColor.Cyan);

            var (company, role) = ExtractCompanyRole(emailText);
            var source          = DetectSource(emailText);

            var existing = _storage.FindDuplicate(company, role);
            if (existing != null)
            {
                existing.Status = existing.Table == TableType.Main
                    ? AppStatus.Refused
                    : AppStatus.Rejected;
                existing.Notes += $" | Rejection recorded {DateTime.Now:yyyy-MM-dd HH:mm} via {source}";
                _storage.UpdateJob(existing);
                Print($"  ❌ Updated: {company} / {role} → {existing.Status}", ConsoleColor.Red);
            }
            else
            {
                var job = new JobEntry
                {
                    Company = company,
                    Role    = role,
                    Source  = source,
                    Status  = AppStatus.Rejected,
                    Table   = TableType.Under93,
                    Notes   = $"Rejection recorded {DateTime.Now:yyyy-MM-dd HH:mm} via {source}"
                };
                _storage.AddJob(job);
                Print($"  ❌ New entry in Under93: {company} / {role} → Rejected", ConsoleColor.Red);
            }

            _storage.LogSummary(new SummaryEntry
            {
                Action  = "Rejection",
                Company = company,
                Role    = role,
                Notes   = $"via {source}"
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // RULE 4 — INTERVIEW RULE
        // ─────────────────────────────────────────────────────────────────
        public void RunInterviewRule(string emailText)
        {
            Print("🧠 INTERVIEW RULE", ConsoleColor.Cyan);

            var (company, role) = ExtractCompanyRole(emailText);
            var interviewDate   = ExtractDate(emailText);

            var existing = _storage.FindDuplicate(company, role);
            if (existing != null)
            {
                // Rule 4.4: promote from Under93 if needed
                if (existing.Table == TableType.Under93)
                {
                    existing.FitScore = Math.Max(existing.FitScore, 93);
                    _storage.MoveToMain(existing);
                    existing.Notes += " | Promoted from Under93 via Interview";
                }

                existing.Status = AppStatus.Interviewing;
                existing.Notes += $" | Started Interview Stage {DateTime.Now:yyyy-MM-dd HH:mm}" +
                                  (interviewDate != null ? $" — Interview: {interviewDate}" : "");
                _storage.UpdateJob(existing);
                Print($"  🎯 Updated: {company} / {role} → Interviewing", ConsoleColor.Green);
            }
            else
            {
                // Rule 10: auto-create if no entry exists
                var job = new JobEntry
                {
                    Company  = company,
                    Role     = role,
                    FitScore = 93,
                    Status   = AppStatus.Interviewing,
                    Table    = TableType.Main,
                    Notes    = $"Interview detected before job entry — auto-created. " +
                               $"Date: {interviewDate ?? "not detected"}"
                };
                _storage.AddJob(job);
                Print($"  🎯 Auto-created Main entry: {company} / {role} → Interviewing", ConsoleColor.Green);
            }

            _storage.LogSummary(new SummaryEntry
            {
                Action  = "Interview",
                Company = company,
                Role    = role,
                Table   = TableType.Main,
                Notes   = $"Interview date: {interviewDate ?? "unknown"}"
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // RULE 5 — CSV MATCHING
        // ─────────────────────────────────────────────────────────────────
        public void ImportFailuresCsv(string path)
        {
            Print("🧠 IMPORT FAILURES CSV", ConsoleColor.Cyan);
            ImportCsv(path, (company, role) =>
            {
                var job = _storage.FindDuplicate(company, role);
                if (job != null)
                {
                    job.Status = AppStatus.Refused;
                    job.Notes += " | Matched via Failures.csv";
                    _storage.UpdateJob(job);
                    Print($"  ❌ Matched & marked Refused: {company} / {role}", ConsoleColor.Red);
                }
                else
                {
                    Print($"  ⚠️  No match found for: {company} / {role}", ConsoleColor.Yellow);
                }
            });
        }

        public void ImportInterviewsCsv(string path)
        {
            Print("🧠 IMPORT INTERVIEWS CSV", ConsoleColor.Cyan);
            ImportCsv(path, (company, role) =>
            {
                var job = _storage.FindDuplicate(company, role);
                if (job != null)
                {
                    job.Status = AppStatus.Interviewing;
                    job.Notes += " | Matched via Interviews.csv";
                    _storage.UpdateJob(job);
                    Print($"  🎯 Matched & marked Interviewing: {company} / {role}", ConsoleColor.Green);
                }
                else
                {
                    Print($"  ⚠️  No match found for: {company} / {role}", ConsoleColor.Yellow);
                }
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────

        private string BuildCompareNotes(JobEntry job, string breakdown, string cvReason, string text)
        {
            var lower   = text.ToLower();
            var gaps    = new List<string>();
            var strengths = new List<string>();

            if (lower.Contains("c#") || lower.Contains(".net")) strengths.Add(".NET/C#");
            if (lower.Contains("php") || lower.Contains("laravel")) strengths.Add("PHP/Laravel");
            if (lower.Contains("python")) strengths.Add("Python");
            if (lower.Contains("azure") || lower.Contains("aws")) strengths.Add("Cloud");
            if (lower.Contains("ci/cd") || lower.Contains("devops")) strengths.Add("DevOps/CI-CD");

            if (!lower.Contains("python")) gaps.Add("Python");
            if (!lower.Contains("aws") && !lower.Contains("azure")) gaps.Add("Cloud");
            if (!lower.Contains("agile") && !lower.Contains("scrum")) gaps.Add("Agile");

            return $"Fit:{job.FitScore}% | " +
                   $"Strengths: {(strengths.Any() ? string.Join(", ", strengths) : "General")} | " +
                   $"Gaps: {(gaps.Any() ? string.Join(", ", gaps) : "None identified")} | " +
                   $"CV: {job.CvChosen} ({cvReason})";
        }

        private static void PrintJobResult(JobEntry job)
        {
            var colour = job.FitScore >= 93 ? ConsoleColor.Green :
                         job.FitScore >= 70 ? ConsoleColor.Yellow : ConsoleColor.Red;

            Print($"\n  Role:     {job.Role}", ConsoleColor.White);
            Print($"  Company:  {job.Company}", ConsoleColor.White);
            Print($"  Location: {job.Location}", ConsoleColor.White);
            Print($"  Salary:   {job.Salary}", ConsoleColor.White);
            Print($"  Type:     {job.JobType}  Mode: {job.Mode}", ConsoleColor.White);
            Print($"  Source:   {job.Source}", ConsoleColor.White);
            Print($"\n  Fit Score: {job.FitScore}%", colour);
            Print($"  Table:    {job.Table}", colour);
            Print($"  Status:   {job.Status}", colour);
        }

        private (string company, string role) ExtractCompanyRole(string text)
        {
            // Try obvious label patterns first
            var companyMatch = Regex.Match(text,
                @"(?:company|employer|from|at)\s*:?\s*([A-Z][^\n,]{2,50})",
                RegexOptions.IgnoreCase);

            var roleMatch = Regex.Match(text,
                @"(?:role|position|job title|applying for|application for)\s*:?\s*([A-Z][^\n,]{2,60})",
                RegexOptions.IgnoreCase);

            var company = companyMatch.Success
                ? companyMatch.Groups[1].Value.Trim()
                : PromptField("Company name not detected — enter manually");

            var role = roleMatch.Success
                ? roleMatch.Groups[1].Value.Trim()
                : PromptField("Role not detected — enter manually");

            return (company, role);
        }

        private static string DetectSource(string text)
        {
            var sources = new[] { "LinkedIn", "Indeed", "Totaljobs", "Reed", "CV-Library",
                                   "Monster", "Glassdoor", "Jobsite" };
            foreach (var s in sources)
                if (text.Contains(s, StringComparison.OrdinalIgnoreCase)) return s;
            return "Unknown";
        }

        private static string? ExtractDate(string text)
        {
            var match = Regex.Match(text,
                @"\b(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4}|\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\w*\s+\d{4})\b",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Value : null;
        }

        private static void ImportCsv(string path, Action<string, string> handler)
        {
            if (!File.Exists(path))
            { Print($"  ❌ File not found: {path}", ConsoleColor.Red); return; }

            int count = 0;
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var cols = line.Split(',');
                if (cols.Length < 2) continue;
                var company = cols[0].Trim().Trim('"');
                var role    = cols[1].Trim().Trim('"');
                handler(company, role);
                count++;
            }
            Print($"  ✅ Processed {count} CSV rows.", ConsoleColor.Green);
        }

        private static async Task<string> FetchUrl(string url)
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (compatible; ATS-Bot/1.0)");
                http.Timeout = TimeSpan.FromSeconds(15);
                var html = await http.GetStringAsync(url);
                // Strip HTML tags for plain text
                return Regex.Replace(html, "<[^>]+>", " ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠️  Fetch error: {ex.Message}");
                return "";
            }
        }

        private static string PromptField(string prompt)
        {
            Console.Write($"  {prompt}: ");
            return Console.ReadLine()?.Trim() ?? "";
        }

        private static void Print(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
    }
}
