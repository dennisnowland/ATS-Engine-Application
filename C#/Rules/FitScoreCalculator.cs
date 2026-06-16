namespace AtsSystem.Rules
{
    /// <summary>
    /// Calculates a 0-100 fit score based on the weighted category table
    /// defined in the ATS Master Rule (Rule 1.2).
    /// </summary>
    public static class FitScoreCalculator
    {
        private static readonly (string[] Keywords, int Weight)[] Categories = new[]
        {
            // PHP / Laravel / C# / .NET  (+20)
            (new[] { "php", "laravel", "c#", ".net", "csharp", "asp.net", "dotnet", "vb.net" }, 20),

            // Python / AI / LLM tools  (+10)
            (new[] { "python", "ai", "llm", "machine learning", "ml", "openai", "langchain", "gpt" }, 10),

            // AWS / Azure / Cloud  (+10)
            (new[] { "aws", "azure", "cloud", "gcp", "google cloud", "ec2", "s3", "lambda" }, 10),

            // CI/CD / DevOps  (+10)
            (new[] { "ci/cd", "cicd", "devops", "azure devops", "github actions", "jenkins",
                     "docker", "kubernetes", "terraform", "pipeline" }, 10),

            // SaaS / Product-led  (+10)
            (new[] { "saas", "product-led", "product led", "b2b saas", "b2c saas",
                     "subscription", "multi-tenant" }, 10),

            // High-traffic systems  (+10)
            (new[] { "high traffic", "high-traffic", "scalable", "distributed",
                     "microservices", "event-driven", "millions of users" }, 10),

            // Agile / Delivery  (+5)
            (new[] { "agile", "scrum", "kanban", "sprint", "delivery", "jira" }, 5),

            // Remote / Hybrid  (+3)
            (new[] { "remote", "hybrid", "flexible working", "work from home" }, 3),

            // SC Clearance  (+2)
            (new[] { "sc clearance", "security clearance", "dv clearance", "sc cleared" }, 2),

            // AI-first / AI tooling  (+5)
            (new[] { "ai-first", "ai first", "copilot", "ai tooling", "llm tools",
                     "generative ai", "gen ai", "chatgpt", "claude" }, 5),
        };

        public static int Calculate(string text)
        {
            var lower  = text.ToLower();
            int total  = 0;

            foreach (var (keywords, weight) in Categories)
            {
                if (keywords.Any(k => lower.Contains(k)))
                    total += weight;
            }

            return Math.Min(total, 100);
        }

        /// <summary>Returns a breakdown string for the notes field.</summary>
        public static string Breakdown(string text)
        {
            var lower  = text.ToLower();
            var hits   = new List<string>();

            string[] labels =
            {
                "PHP/Laravel/C#/.NET (+20)",
                "Python/AI/LLM (+10)",
                "AWS/Azure/Cloud (+10)",
                "CI/CD/DevOps (+10)",
                "SaaS/Product-led (+10)",
                "High-traffic systems (+10)",
                "Agile/Delivery (+5)",
                "Remote/Hybrid (+3)",
                "SC Clearance (+2)",
                "AI-first tooling (+5)"
            };

            for (int i = 0; i < Categories.Length; i++)
            {
                var (keywords, _) = Categories[i];
                if (keywords.Any(k => lower.Contains(k)))
                    hits.Add("✅ " + labels[i]);
                else
                    hits.Add("❌ " + labels[i]);
            }

            return string.Join(" | ", hits);
        }
    }
}
