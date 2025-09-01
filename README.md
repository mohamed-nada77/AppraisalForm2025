AppraisalPortal
AppraisalPortal is a .NET 8 Razor Pages application that streamlines end-to-end employee performance appraisals. It replaces scattered spreadsheets and email threads with a clear, role-based workflow and consistent scoring, plus PDF outputs for official records.
💡 What it solves
•	Consistency: Everyone follows the same forms, scales, and weights (no more custom Excel formulas).
•	Clarity: Employees, managers, HR, and the CEO each get purpose-built pages with the exact actions they need.
•	Speed: Automated roll-ups, scoring, approvals, and one-click PDF export.
•	Auditability: Every submitted form has an immutable PDF and a summary report.
________________________________________
🧭 Workflow (at a glance)
Employee (Self-Assessment) │ ▼ Reporting Manager (Review & Adjust) │ ▼ HR (All forms view + Approval gate) │ (if approved) ▼ CEO (Download PDFs + Executive Summary)
markdown Copy code
•	Employee fills their Self appraisal (responsibilities, KPI items, soft skills).
•	Reporting Manager reviews the employee’s form, can adjust ratings and add comments.
•	HR has visibility across all forms (by department or company-wide), ensures policy compliance, and approves the cycle.
•	CEO sees a consolidated summary and can download PDFs for official storage or circulation.
________________________________________
🧱 Key Features
•	Identity & Access
o	ASP.NET Core Identity with EmpCode as username.
o	MustChangePassword on first login with a ForceChange page.
o	Claims transformer assigns Manager role automatically when a user has a defined manager scope.
•	Domain
o	Responsibilities (self text inputs)
o	KPIItems (scored 0–100)
o	SoftSkillRatings (scored 1–10)
o	ManagerScope and Departments for access control
•	Scoring
o	Final score = KPI 70% + Soft Skills 30% (via ScoringService)
•	Admin
o	Import (ClosedXML) to ingest employees and scopes from Excel
o	Cycles to open/close appraisal rounds
o	Scopes to define reporting structures
o	Passwords to generate initial passwords by rule
o	ManagerCheck to validate scopes/claims alignment
•	Reports & PDFs
o	Form PDF and Summary PDF generated with QuestPDF
o	Clean layout suitable for HR files and executive briefings
•	Dashboards
o	Manager: Inbox & Review
o	HR: All/Review and Reports → Summary
o	CEO: Executive overview & Download PDFs
________________________________________
🧩 Why this workflow benefits many companies
•	SMBs to Enterprise: Any org with a reporting structure and periodic performance reviews (quarterly, bi-annual, annual) can use it.
•	Multi-Department Fit: Manager scopes + departments mean multiple teams can run the same appraisal cycle without cross-contamination.
•	Remote/Hybrid-Ready: Role-based access and PDFs keep stakeholders aligned across geographies.
•	Policy-Aligned: HR approval gate enforces consistent criteria before executive sign-off.
•	Auditable Results: PDFs + clear scoring rules build trust and reduce disputes.
________________________________________
🧮 Scoring Model
•	KPI items: numeric (0–100), weighted into a KPI subtotal.
•	Soft skills: numeric (1–10), normalized to a soft-skills subtotal.
•	Final score: Final = KPI * 0.70 + SoftSkills * 0.30.
You can adjust weights in ScoringService if your company uses a different model.
________________________________________
🖥️ Tech Stack
•	Backend/UI: ASP.NET Core .NET 8 (Razor Pages)
•	Auth: ASP.NET Core Identity (EmpCode as username)
•	PDFs: QuestPDF
•	Import: ClosedXML (.xlsx)
•	UI Kit: Bootstrap + jQuery (in wwwroot/lib)
•	DB: EF Core Migrations
