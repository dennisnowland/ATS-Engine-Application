namespace AtsSystem.Rules
{
    /// <summary>
    /// Selects the best CV filename based on role type keywords (Rule 1.5).
    /// </summary>
    public static class CvSelector
    {
        private static readonly (string[] Keywords, string CvFile)[] Mappings = new[]
        {
            (new[] { "php", "laravel", "wordpress", "symfony" },
             "Senior PHP Developer CV"),

            (new[] { "c#", ".net", "csharp", "asp.net", "dotnet", "vb.net" },
             "C# Developer CV"),

            (new[] { "engineering manager", "head of engineering", "vp engineering" },
             "Engineering Manager CV"),

            (new[] { "agile coach" },
             "Agile Coach CV"),

            (new[] { "scrum master" },
             "Scrum Master CV"),

            (new[] { "technical programme manager", "tpm" },
             "TPM CV"),

            (new[] { "pmo", "programme office", "project management office" },
             "PMO CV"),

            (new[] { "delivery manager", "delivery lead" },
             "Delivery CV"),

            (new[] { "senior engineer", "tech lead", "principal engineer",
                     "staff engineer", "lead developer" },
             "Senior Developer CV"),
        };

        public static (string CvFile, string Reason) Select(string roleText)
        {
            var lower = roleText.ToLower();

            foreach (var (keywords, cvFile) in Mappings)
            {
                foreach (var kw in keywords)
                {
                    if (lower.Contains(kw))
                        return (cvFile, $"Matched keyword '{kw}' → {cvFile}");
                }
            }

            // Default fallback
            return ("Senior Developer CV", "No specific keyword match — defaulting to Senior Developer CV");
        }
    }
}
