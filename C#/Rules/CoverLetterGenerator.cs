using AtsSystem.Models;

namespace AtsSystem.Rules
{
    /// <summary>
    /// Rule 7: Auto-generate a tailored cover letter when Fit Score >= 93%.
    /// </summary>
    public static class CoverLetterGenerator
    {
        public static string Generate(JobEntry job, string jobDescription)
        {
            var date     = DateTime.Now.ToString("d MMMM yyyy");
            var role     = job.Role;
            var company  = job.Company;
            var location = string.IsNullOrEmpty(job.Location) ? "UK" : job.Location;

            // Extract key skills from job description for body personalisation
            var skills = ExtractKeySkills(jobDescription);

            return $@"
{date}

Hiring Manager
{company}
{location}

Dear Hiring Manager,

Re: {role} — {company}

I am writing to express my strong interest in the {role} position at {company}. Having reviewed the job specification in detail, I am confident that my experience and skills align closely with your requirements, and I am excited by the opportunity to contribute to your team.

{BuildStrengthsParagraph(skills, role)}

{BuildExperienceParagraph(job, skills)}

I am particularly drawn to {company} because of the opportunity to {BuildCompanyValue(job, jobDescription)}. I thrive in environments that value collaboration, technical excellence, and continuous improvement, all of which appear central to your culture.

I would welcome the opportunity to discuss how my background fits your needs in more detail. I am available for interview at your convenience and can provide references on request.

Thank you for taking the time to consider my application.

Yours sincerely,

[Your Full Name]
[Email Address]
[Phone Number]
[LinkedIn Profile]

— CV Submitted: {job.CvChosen}
— Fit Score:    {job.FitScore}%
— Generated:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";
        }

        private static List<string> ExtractKeySkills(string text)
        {
            var lower = text.ToLower();
            var found = new List<string>();

            var candidates = new[]
            {
                ("c#", "C#"), (".net", ".NET"), ("asp.net", "ASP.NET"), ("php", "PHP"),
                ("laravel", "Laravel"), ("python", "Python"), ("azure", "Azure"),
                ("aws", "AWS"), ("sql server", "SQL Server"), ("postgresql", "PostgreSQL"),
                ("docker", "Docker"), ("kubernetes", "Kubernetes"), ("ci/cd", "CI/CD"),
                ("react", "React"), ("angular", "Angular"), ("typescript", "TypeScript"),
                ("microservices", "Microservices"), ("agile", "Agile"), ("scrum", "Scrum"),
                ("devops", "DevOps"), ("rest api", "REST APIs"), ("graphql", "GraphQL"),
                ("redis", "Redis"), ("elasticsearch", "Elasticsearch"),
                ("terraform", "Terraform"), ("github actions", "GitHub Actions"),
            };

            foreach (var (kw, label) in candidates)
                if (lower.Contains(kw)) found.Add(label);

            return found.Take(6).ToList();
        }

        private static string BuildStrengthsParagraph(List<string> skills, string role)
        {
            if (!skills.Any())
                return $"Throughout my career I have built deep expertise in full-stack software development, delivering robust, maintainable solutions across a range of sectors.";

            return $"Throughout my career I have built deep expertise in {string.Join(", ", skills.Take(4))}, " +
                   $"skills which are directly relevant to this {role} position. I have consistently delivered " +
                   $"high-quality, maintainable solutions and I enjoy mentoring colleagues to raise the bar across the team.";
        }

        private static string BuildExperienceParagraph(JobEntry job, List<string> skills)
        {
            var extra = skills.Count > 4 ? $", along with {string.Join(", ", skills.Skip(4))}" : "";
            return $"My background includes hands-on work with {string.Join(", ", skills.Take(4))}{extra}. " +
                   $"I have a proven record of translating complex requirements into pragmatic, scalable solutions " +
                   $"and of working closely with business analysts and stakeholders to ensure delivery on time and within scope.";
        }

        private static string BuildCompanyValue(JobEntry job, string jd)
        {
            if (jd.ToLower().Contains("healthcare") || jd.ToLower().Contains("health"))
                return "make a meaningful impact in the healthcare sector through technology";
            if (jd.ToLower().Contains("fintech") || jd.ToLower().Contains("finance"))
                return "work on challenging, high-stakes financial technology problems";
            if (jd.ToLower().Contains("saas"))
                return "build and scale modern SaaS products that reach a broad user base";
            return "solve meaningful technical challenges and deliver real value to your customers";
        }

        public static void SaveToFile(JobEntry job, string content)
        {
            var filename = $"CoverLetter_{Sanitise(job.Company)}_{Sanitise(job.Role)}_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            File.WriteAllText(filename, content);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✅ Cover letter saved → {filename}");
            Console.ResetColor();
        }

        private static string Sanitise(string s) =>
            string.Concat(s.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
    }
}
