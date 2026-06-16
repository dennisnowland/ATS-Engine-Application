using AtsSystem.Models;
using System.Text.RegularExpressions;

namespace AtsSystem.Rules
{
    /// <summary>
    /// Extracts job metadata from raw pasted text (Rule 1.1).
    /// Uses heuristics and regex patterns to find common fields.
    /// </summary>
    public static class MetadataExtractor
    {
        public static JobEntry Extract(string text)
        {
            var job = new JobEntry();

            // Salary: £ or $ followed by digits
            var salaryMatch = Regex.Match(text, @"[£$€]\s?[\d,]+(?:k|K)?(?:\s*[-–]\s*[£$€]?\s*[\d,]+(?:k|K)?)?(?:\s*(?:per annum|pa|p\.a\.|per year|/yr))?", RegexOptions.IgnoreCase);
            if (salaryMatch.Success)
                job.Salary = salaryMatch.Value.Trim();

            // Job type
            if (Regex.IsMatch(text, @"\bpermanent\b", RegexOptions.IgnoreCase))
                job.JobType = "Permanent";
            else if (Regex.IsMatch(text, @"\bcontract\b", RegexOptions.IgnoreCase))
                job.JobType = "Contract";
            else if (Regex.IsMatch(text, @"\bfixed.?term\b", RegexOptions.IgnoreCase))
                job.JobType = "Fixed Term";

            // Work mode
            if (Regex.IsMatch(text, @"\bfully\s+remote\b", RegexOptions.IgnoreCase))
                job.Mode = "Remote";
            else if (Regex.IsMatch(text, @"\bhybrid\b", RegexOptions.IgnoreCase))
                job.Mode = "Hybrid";
            else if (Regex.IsMatch(text, @"\bon.?site\b|\bin.?office\b", RegexOptions.IgnoreCase))
                job.Mode = "On-site";

            // Source detection
            var knownSources = new[] { "LinkedIn", "Indeed", "Totaljobs", "Reed", "CV-Library",
                                        "Monster", "Glassdoor", "Jobsite", "Guardian Jobs" };
            foreach (var src in knownSources)
            {
                if (text.Contains(src, StringComparison.OrdinalIgnoreCase))
                { job.Source = src; break; }
            }

            // Agency — look for "via", "through", "Recruitment" near company names
            var agencyMatch = Regex.Match(text, @"(?:via|through|agency:?)\s+([A-Z][A-Za-z\s&]+(?:Recruitment|Staffing|Consulting|Solutions|Ltd|Limited)?)", RegexOptions.IgnoreCase);
            if (agencyMatch.Success)
                job.Agency = agencyMatch.Groups[1].Value.Trim();

            // SC Clearance
            if (Regex.IsMatch(text, @"\bsc\s+clear(ed|ance)\b|\bsecurity\s+clear(ed|ance)\b", RegexOptions.IgnoreCase))
                job.Notes += " [SC Clearance Required]";

            // Location extraction — look for UK city names
            var cities = new[] { "London", "Manchester", "Birmingham", "Leeds", "Edinburgh",
                                  "Glasgow", "Bristol", "Cardiff", "Nottingham", "Liverpool",
                                  "Sheffield", "Leicester", "Southampton", "Cambridge", "Oxford" };
            var foundCities = cities.Where(c => text.Contains(c, StringComparison.OrdinalIgnoreCase)).ToList();
            if (foundCities.Any())
                job.Location = string.Join(", ", foundCities.Take(3));
            else if (Regex.IsMatch(text, @"\bUK\b"))
                job.Location = "UK";

            // Attempt to extract company name from common patterns
            var companyPatterns = new[]
            {
                @"(?:company|employer|hiring company|organisation):\s*(.+)",
                @"^(.+?)\s+(?:is hiring|are hiring|is looking)",
                @"About\s+(.{3,40}?)[\n\r]",
            };
            foreach (var pattern in companyPatterns)
            {
                var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (m.Success && string.IsNullOrEmpty(job.Company))
                {
                    job.Company = m.Groups[1].Value.Trim().Split('\n')[0].Trim();
                    break;
                }
            }

            // Attempt role extraction
            var rolePatterns = new[]
            {
                @"(?:job title|role|position):\s*(.+)",
                @"(?:hiring|seeking|looking for)\s+(?:a|an)?\s*(.+?)(?:\s+to|\s+who|\s+with|\.|\n)",
                @"^((?:Senior|Lead|Principal|Staff|Junior|Mid)?\s*(?:Software|Full[- ]?Stack|Backend|Frontend|PHP|C#|\.NET|Python|DevOps|Cloud|Platform|Data|Mobile|QA|Test)[\w\s]+(?:Developer|Engineer|Architect|Manager|Lead|Specialist))",
            };
            foreach (var pattern in rolePatterns)
            {
                var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (m.Success && string.IsNullOrEmpty(job.Role))
                {
                    job.Role = m.Groups[1].Value.Trim().Split('\n')[0].Trim();
                    break;
                }
            }

            return job;
        }

        /// <summary>
        /// Prompts the user to fill in any empty mandatory fields interactively.
        /// </summary>
        public static void PromptMissingFields(JobEntry job)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n── Metadata Extracted ──");
            Console.ResetColor();

            job.Role    = PromptIfEmpty("Role",    job.Role);
            job.Company = PromptIfEmpty("Company", job.Company);
            if (string.IsNullOrEmpty(job.Location))
                job.Location = PromptIfEmpty("Location", job.Location);
        }

        private static string PromptIfEmpty(string field, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine($"  {field}: {value}");
                return value;
            }
            Console.Write($"  {field} not detected — enter manually: ");
            return Console.ReadLine()?.Trim() ?? "";
        }
    }
}
