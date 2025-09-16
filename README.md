 AppraisalForm2025

AppraisalForm2025 is a **.NET 8 Razor Pages** application that streamlines end-to-end **employee performance appraisals**. It replaces scattered spreadsheets and email threads with a **clear, role-based workflow** and **consistent scoring**, plus **PDF outputs** for official records.

## 💡 What it solves

- **Consistency:** Everyone follows the same forms, scales, and weights (no more custom Excel formulas).
- **Clarity:** Employees, managers, HR, and the CEO each get purpose-built pages with the exact actions they need.
- **Speed:** Automated roll-ups, scoring, approvals, and one-click PDF export.
- **Auditability:** Every submitted form has an immutable PDF and a summary report.

---

## 🧭 Workflow (at a glance)

Employee (Self-Assessment)


│
▼


Reporting Manager (Review & Adjust)


│
▼


HR (All forms view + Approval gate)


(if approved)

│ 
▼



CEO (Download PDFs + Executive Summary)

markdown
Copy code



- **Employee** fills their **Self** appraisal (responsibilities, KPI items, soft skills).
- **Reporting Manager** reviews the employee’s form, can **adjust ratings** and add comments.
- **HR** has visibility across **all forms** (by department or company-wide), ensures **policy compliance**, and approves the cycle.
- **CEO** sees a consolidated **summary** and can **download PDFs** for official storage or circulation.

---

## 🧱 Key Features

- **Identity & Access**
  - ASP.NET Core Identity with **EmpCode as username**.
  - **MustChangePassword** on first login with a **ForceChange** page.
  - **Claims transformer** assigns **Manager** role automatically when a user has a defined manager scope.

- **Domain**
  - **Responsibilities** (self text inputs)
  - **KPIItems** (scored 0–100)
  - **SoftSkillRatings** (scored 1–10)
  - **ManagerScope** and **Departments** for access control

- **Scoring**
  - Final score = **KPI 70% + Soft Skills 30%** (via `ScoringService`)

- **Admin**
  - **Import** (ClosedXML) to ingest employees and scopes from Excel
  - **Cycles** to open/close appraisal rounds
  - **Scopes** to define reporting structures
  - **Passwords** to generate initial passwords by rule
  - **ManagerCheck** to validate scopes/claims alignment

- **Reports & PDFs**
  - **Form PDF** and **Summary PDF** generated with **QuestPDF**
  - Clean layout suitable for HR files and executive briefings

- **Dashboards**
  - **Manager**: Inbox & Review
  - **HR**: All/Review and **Reports → Summary**
  - **CEO**: Executive overview & **Download PDFs**

---

## 🧩 Why this workflow benefits many companies

- **SMBs to Enterprise:** Any org with a reporting structure and periodic performance reviews (quarterly, bi-annual, annual) can use it.
- **Multi-Department Fit:** Manager scopes + departments mean multiple teams can run the **same** appraisal cycle without cross-contamination.
- **Remote/Hybrid-Ready:** Role-based access and PDFs keep stakeholders aligned across geographies.
- **Policy-Aligned:** HR approval gate enforces consistent criteria before executive sign-off.
- **Auditable Results:** PDFs + clear scoring rules build trust and reduce disputes.

---

## 🧮 Scoring Model

- **KPI items:** numeric (0–100), weighted into a KPI subtotal.
- **Soft skills:** numeric (1–10), normalized to a soft-skills subtotal.
- **Final score:** `Final = KPI * 0.70 + SoftSkills * 0.30`.

You can adjust weights in `ScoringService` if your company uses a different model.

---

## 🖥️ Tech Stack

- **Backend/UI:** ASP.NET Core **.NET 8** (Razor Pages)
- **Auth:** ASP.NET Core Identity (EmpCode as username)
- **PDFs:** QuestPDF
- **Import:** ClosedXML (.xlsx)
- **UI Kit:** Bootstrap + jQuery (in `wwwroot/lib`)
- **DB:** EF Core Migrations

---

## 📁 Repository Structure

Root/
├─ Areas/Identity/... # Login/ForceChange etc.

├─ Data/ # EF DbContext, SeedData

├─ Migrations/ # EF Core migrations

├─ Models/ # AppUser, AppraisalCycle, Form, KPIItem, ...

├─ Pages/

│ ├─ Admin/ # Import, Cycles, Scopes, Passwords, ManagerCheck

│ ├─ Employee/ # Self, Appraisals

│ ├─ Manager/ # Inbox, Review

│ ├─ HR/ # All, Review

│ ├─ CEO/ # All/summary views & downloads

│ └─ Reports/ # FormPdf, Summary

├─ Security/ # ScopeRoleClaimsTransformer

├─ Services/ # ScoringService, PdfService, EmployeeImportService, PasswordRuleService

└─ wwwroot/ # Static assets (bootstrap, jquery, css, images)


---


🔐 Roles & Access
Employee: Self-appraisal, can revisit before submission (if cycle open).

Reporting Manager: Reviews assigned team forms, can adjust and comment.

HR: Full visibility, ensures policy compliance, tracks cycle progress, approves.

CEO: High-level summary & PDF download for finalized/approved forms.

Admin: Imports, cycles, scopes, password rules, manager checks.

First login requires password change (MustChangePassword) for security.

📥 Importing Employees & Scopes
Go to Admin → Import.

Upload an .xlsx file exported from your HRIS or a clean spreadsheet.

The import service (ClosedXML) maps users, departments, and manager scopes.

After import, ManagerCheck validates that scope assignments match expectations.

Tip: Keep a master HR spreadsheet template in your company drive so each cycle uses the same column names and formats.

🧾 PDF Outputs
Form PDF: The full, finalized appraisal (employee + manager review).

Summary PDF: Executive-friendly summary across departments.

Generated with QuestPDF for consistent, print-ready output.

🏢 Who can benefit?
SMEs (50–500 employees): Replace manual spreadsheets and email approvals with a structured, role-based flow.

Mid-market (500–5,000): Manager scopes + department views scale cleanly across org layers and regions.

Enterprises: Clear separation of duties (Employee → Manager → HR → CEO), consistent scoring, and PDF archives for audits.

Hybrid/Remote teams: Keep reviews standardized and transparent regardless of location.

🛠️ Configuration & Deployment
Environments: Use appsettings.{Environment}.json + environment variables. Keep real secrets in KeyVault/Secrets Manager or machine secrets.

Windows/IIS: Run behind IIS with the ASP.NET Core Module.

Linux/Nginx: Run Kestrel behind Nginx as a reverse proxy.

Containers: Add a Dockerfile if you want containerized deploys (not included by default).

🗺️ Roadmap (suggested)
Calibration view for HR across managers

Per-department weighting overrides

E-signatures on PDFs

Email/Teams notifications for stage transitions

Analytics: distribution charts, outliers, year-over-year trends



## 🚀 Getting Started (Local)

### Prerequisites
- **.NET 8 SDK**
- SQL Server (Developer/Express or a container)
- Git

### 1) Clone & restore
```powershell
git clone https://github.com/mohamed-nada77/AppraisalForm2025.git
cd AppraisalForm2025
dotnet restore
2) Configure secrets (keep real secrets out of git)
Use User Secrets for local development:

powershell
Copy code
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=AppraisalPortal;Trusted_Connection=True;TrustServerCertificate=True"
# If using certificate passwords, tokens, SMTP, etc.:
# dotnet user-secrets set "Certificates:Password" "YOUR_CERT_PASSWORD"
Create an example file for collaborators (no secrets):

json
Copy code
// appsettings.json.example
{
  "ConnectionStrings": {
    "DefaultConnection": "<set via user-secrets or environment>"
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  }
}
3) Database
powershell
Copy code
dotnet ef database update
4) Seed roles & demo users (optional)
SeedData can set up baseline roles (Admin/Manager/HR/CEO) and example scopes. Adjust SeedData as needed, then run the app so seeding executes.

5) Run
powershell
Copy code
dotnet run
Visit https://localhost:5001 (or the port shown in output).


<img width="961" height="421" alt="image" src="https://github.com/user-attachments/assets/e1857b78-2a82-46b7-b88b-c1f2cdfb0e1e" />

<img width="860" height="761" alt="image" src="https://github.com/user-attachments/assets/06ab7331-ed7a-49a2-9c5d-31ec5ea51cf3" />

<img width="1085" height="654" alt="image" src="https://github.com/user-attachments/assets/7e51cdcb-a538-4879-ace7-8f9c02f11dbd" />

<img width="1919" height="875" alt="image" src="https://github.com/user-attachments/assets/7ca81b8e-464f-452c-8fe4-9fa137878460" />



🤝 Contributing
Create a feature branch.
