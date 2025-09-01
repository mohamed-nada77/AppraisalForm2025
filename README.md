---8<--- COPY FROM HERE ---8<---

AppraisalForm2025

AppraisalForm2025 is a .NET 8 Razor Pages application that streamlines end-to-end employee performance appraisals. It replaces scattered spreadsheets and email threads with a clear, role-based workflow, consistent scoring, and PDF outputs for official records.

Support (first-time login & general help):
If you face any issue or need assistance, please contact: mohamed@alpagoproperties.com
 · +971 52 885 8560

🎯 What it solves

Consistency: Everyone follows the same forms, scales, and weights (no more one-off Excel formulas).

Clarity: Employees, Managers, HR, and the CEO each get purpose-built pages with only the actions they need.

Speed: Automated roll-ups, scoring, approvals, and one-click PDF export.

Auditability: Every submitted form has a permanent PDF and a cycle summary.

🧭 Workflow (at a glance)
Employee (Self-Assessment)
     │
     ▼
Reporting Manager (Review & Adjust)
     │
     ▼
HR (All forms view + Approval gate)
     │  (if approved)
     ▼
CEO (Download PDFs + Executive Summary)


Employee completes their Self appraisal (Responsibilities, KPI items, Soft skills).

Reporting Manager reviews and can adjust ratings / add comments.

HR has full visibility across all forms, ensures policy compliance, and approves the cycle.

CEO sees summary insights and downloads PDFs for official records.

🧱 Key Features

Identity & Access

ASP.NET Core Identity with EmpCode as username.

MustChangePassword on first login with a dedicated Force-Change page.

Claims transformer assigns Manager role automatically when a user has a defined ManagerScope (departments/teams).

Domain Model

Responsibilities (self text inputs)

KPIItems (0–100)

SoftSkillRatings (1–10)

ManagerScope (+ departments) for access control

Scoring

Final score = KPI 70% + Soft Skills 30% (via ScoringService).

Swap weights easily to fit different company policies.

Administration

Import (ClosedXML) to ingest employees/scopes from Excel.

Cycles to open/close appraisal rounds.

Scopes to define reporting structures.

Passwords to generate initial passwords by rule.

ManagerCheck to validate scope & claims alignment.

Reports & PDFs

Form PDF (final appraisal) and Summary PDF (company/department overview) via QuestPDF.

Clean, print-ready layout.

Role Dashboards

Manager: Inbox & Review

HR: All/Review and Reports → Summary

CEO: Executive overview & Download PDFs

🖥️ Tech Stack

Framework: ASP.NET Core .NET 8 (Razor Pages)

Auth: ASP.NET Core Identity (EmpCode username)

PDF: QuestPDF

Excel Import: ClosedXML (.xlsx)

UI: Bootstrap + jQuery (in wwwroot/lib)

Data: EF Core + Migrations

📁 Project Structure (high level)
Root/
├─ Areas/Identity/...      # Login, ForceChange
├─ Data/                   # EF DbContext, SeedData
├─ Migrations/             # EF Core migrations
├─ Models/                 # AppUser, AppraisalCycle, Form, KPIItem, Responsibility, SoftSkillRating, ManagerScope...
├─ Pages/
│  ├─ Admin/               # Import, Cycles, Scopes, Passwords, ManagerCheck
│  ├─ Employee/            # Self, Appraisals
│  ├─ Manager/             # Inbox, Review
│  ├─ HR/                  # All, Review
│  ├─ CEO/                 # Executive views & downloads
│  └─ Reports/             # FormPdf, Summary
├─ Security/               # ScopeRoleClaimsTransformer
├─ Services/               # ScoringService, PdfService, EmployeeImportService, PasswordRuleService
└─ wwwroot/                # Static assets (bootstrap, jquery, css, images)

🚀 Getting Started (Local)
Prerequisites

.NET 8 SDK

SQL Server (Developer/Express or container)

Git

1) Restore dependencies
dotnet restore

2) Configure secrets (keep real secrets out of Git)

Use User Secrets for development:

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=AppraisalPortal;Trusted_Connection=True;TrustServerCertificate=True"
# Optional examples:
# dotnet user-secrets set "Certificates:Password" "YOUR_CERT_PASSWORD"
# dotnet user-secrets set "Smtp:Host" "smtp.example.com"


Include a non-secret example for collaborators (commit this):

// appsettings.json.example
{
  "ConnectionStrings": { "DefaultConnection": "<set via user-secrets or environment>" },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } }
}


Tip: Add real appsettings.json and appsettings.*.local.json to .gitignore. Use environment variables or secrets in production.

3) Database migration
dotnet ef database update

4) Seeding (optional)

SeedData can create baseline roles (Admin/Manager/HR/CEO) and demo users/scopes. Adjust if needed, then run the app to execute seeding.

5) Run
dotnet run


Open the URL shown in the console (e.g., https://localhost:5001).

🔐 Roles & Permissions

Employee: Self-appraisal (can revise while cycle open).

Reporting Manager: Reviews assigned team forms, adjusts ratings, adds comments.

HR: Full visibility, policy gatekeeper, cycle approval.

CEO: High-level summaries and PDF downloads.

Admin: Imports, cycles, scopes, password rules, scope validation.

First-time login experience: Users are greeted, asked to change their password (MustChangePassword), and shown a professional welcome note plus support contact for any issues.

📥 Importing Employees & Scopes (Excel)

Go to Admin → Import.

Upload a clean .xlsx file (export from HRIS or a curated template).

The EmployeeImportService maps users, departments, and ManagerScope.

Use ManagerCheck to verify scopes & access are correct.

Keep a master HR template in your drive so every cycle uses consistent columns.

🧾 PDF Outputs

Form PDF: Final, signed-off appraisal (employee + manager sections).

Summary PDF: A department/company overview for HR/CEO.

Built with QuestPDF for consistent, print-ready documents.

🧮 Scoring Model (default)

KPI items: numeric 0–100 → KPI subtotal.

Soft skills: numeric 1–10 → normalized soft-skills subtotal.

Final score:

Final = KPI * 0.70 + SoftSkills * 0.30


Weights are centralized in ScoringService and easy to change.

🏢 Who Benefits & Why

SMEs (50–500 employees): Replace manual spreadsheets and email approvals with a single source of truth.

Mid-market (500–5,000): Manager scopes + department views scale across org layers and regions.

Enterprises: Clear separation of duties (Employee → Manager → HR → CEO), consistent scoring, and auditable PDF records.

Hybrid/Remote: Role-based access and PDF outputs keep evaluations standardized across locations/time zones.

Benefits across the board:

Faster cycles and less admin work

Fairer, consistent evaluations

Stronger compliance and audit trail

Clear executive visibility
