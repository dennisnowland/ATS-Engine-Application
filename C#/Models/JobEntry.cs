namespace AtsSystem.Models
{
    public enum AppStatus
    {
        ToReview,
        HighPriority,
        LowPriority,
        Submitted,
        Interviewing,
        Rejected,
        Refused
    }

    public enum TableType
    {
        Main,
        Under93
    }

    public class JobEntry
    {
        public Guid   Id           { get; set; } = Guid.NewGuid();
        public string Role         { get; set; } = "";
        public string Company      { get; set; } = "";
        public string Location     { get; set; } = "";
        public string Salary       { get; set; } = "";
        public string Mode         { get; set; } = "";       // Remote / Hybrid / On-site
        public string JobType      { get; set; } = "";       // Permanent / Contract
        public string Agency       { get; set; } = "";
        public string Source       { get; set; } = "";
        public int    FitScore     { get; set; }
        public AppStatus Status    { get; set; } = AppStatus.ToReview;
        public TableType Table     { get; set; } = TableType.Under93;
        public string CvChosen     { get; set; } = "";
        public string Notes        { get; set; } = "";
        public int    Duplicates   { get; set; } = 0;
        public DateTime CreatedAt  { get; set; } = DateTime.Now;
        public DateTime UpdatedAt  { get; set; } = DateTime.Now;
    }

    public class SummaryEntry
    {
        public DateTime Timestamp  { get; set; } = DateTime.Now;
        public string   Action     { get; set; } = "";
        public string   Company    { get; set; } = "";
        public string   Role       { get; set; } = "";
        public int      FitScore   { get; set; }
        public TableType Table     { get; set; }
        public string   CvChosen   { get; set; } = "";
        public string   Notes      { get; set; } = "";
    }
}
