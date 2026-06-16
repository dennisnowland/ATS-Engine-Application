# ATS Master Rule Engine — C# Application
**Version:** 1.0.0 — 16 June 2026

A full C# console application implementing all 12 rules from the ATS Master Rule Backup.

---

## Rules Implemented

| # | Rule | Class |
|---|------|-------|
| 1 | Compare Rule (score, route, CV select, notes) | `AtsEngine.RunCompareRule` |
| 2 | Submission Rule | `AtsEngine.RunSubmissionRule` |
| 3 | Failure Rule | `AtsEngine.RunFailureRule` |
| 4 | Interview Rule | `AtsEngine.RunInterviewRule` |
| 5 | Failures.csv & Interviews.csv Matching | `AtsEngine.ImportFailuresCsv / ImportInterviewsCsv` |
| 6 | Under-93 Rule | Enforced in `RunCompareRule` — all <93% go to Under93 table |
| 7 | Auto-Generate Cover Letter (≥93%) | `CoverLetterGenerator` |
| 8 | Daily Summary Log | `JobStorage.LogSummary` → `daily_summary.log` |
| 9 | Best CV Output Rule | `CvSelector` + output in `RunCompareRule` |
| 10 | Auto-Add Missing Interview Entries | `AtsEngine.RunInterviewRule` (auto-create branch) |
| 11 | Auto-Detect Job URL | `AtsEngine.RunCompareRule` (URL fetch branch) |
| 12 | Always Run Compare on Every Job URL | Persistent logic in `RunCompareRule` |

---

## Project Structure

```
AtsSystem/
├── AtsSystem.csproj
├── Program.cs                  ← Entry point & menu
├── README.md
├── Engine/
│   └── AtsEngine.cs            ← Orchestrates all rules
├── Models/
│   └── JobEntry.cs             ← Data models (JobEntry, SummaryEntry, enums)
├── Rules/
│   ├── FitScoreCalculator.cs   ← Weighted scoring (Rule 1.2)
│   ├── CvSelector.cs           ← CV selection logic (Rule 1.5)
│   ├── MetadataExtractor.cs    ← Job metadata extraction (Rule 1.1)
│   └── CoverLetterGenerator.cs ← Cover letter generation (Rule 7)
└── Storage/
    └── JobStorage.cs           ← JSON persistence + display
```

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## How to Run

```bash
cd AtsSystem
dotnet run
```

---

## Menu Options

| Option | Rule Triggered |
|--------|---------------|
| 1 | Paste job description → Compare Rule |
| 2 | Paste submission email → Submission Rule |
| 3 | Paste rejection email → Failure Rule |
| 4 | Paste interview invite → Interview Rule |
| 5 | Import failures.csv |
| 6 | Import interviews.csv |
| 7 | View Main Table (≥93%) |
| 8 | View Under-93 Table |
| 9 | View Daily Summary Log |
| 0 | Exit |

---

## Fit Score Weights

| Category | Weight |
|----------|--------|
| PHP / Laravel / C# / .NET | +20 |
| Python / AI / LLM tools | +10 |
| AWS / Azure / Cloud | +10 |
| CI/CD / DevOps | +10 |
| SaaS / Product-led | +10 |
| High-traffic systems | +10 |
| Agile / Delivery | +5 |
| Remote / Hybrid | +3 |
| SC Clearance | +2 |
| AI-first / AI tooling | +5 |

Total possible: 100%

---

## CSV Format

Both `failures.csv` and `interviews.csv` should have this format:

```csv
Company,Role
Barchester Healthcare,Senior Software Developer
Acme Corp,PHP Developer
```

---

## Output Files

| File | Description |
|------|-------------|
| `jobs.json` | All job entries (persisted between runs) |
| `daily_summary.log` | Append-only log of all actions |
| `CoverLetter_*.txt` | Auto-generated cover letters (≥93% only) |
