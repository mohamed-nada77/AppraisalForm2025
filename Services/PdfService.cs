// /Services/PdfService.cs
using AppraisalPortal.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AppraisalPortal.Services
{
    public class PdfService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public PdfService(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> SummaryPdfAsync(int formId, CancellationToken ct = default)
        {
            var form = await _db.Forms
                .Include(f => f.Employee).ThenInclude(e => e.Manager)
                .Include(f => f.Cycle)
                .FirstOrDefaultAsync(f => f.Id == formId, ct)
                ?? throw new InvalidOperationException("Form not found.");

            var self = await _db.Responsibilities
                .AsNoTracking()
                .Where(r => r.FormId == form.Id)
                .OrderBy(r => r.Id)
                .ToListAsync(ct);

            var kpis = await _db.KPIItems
                .AsNoTracking()
                .Where(k => k.FormId == form.Id)
                .OrderBy(k => k.Id)
                .ToListAsync(ct);

            var softs = await _db.SoftSkillRatings
                .AsNoTracking()
                .Where(s => s.FormId == form.Id)
                .OrderBy(s => s.Id)
                .ToListAsync(ct);

            // ---- Calculations
            int kpiTotal = kpis.Count > 0 ? (int)Math.Round(kpis.Average(x => (double)x.Score)) : 0;
            decimal kpiWeighted = Math.Round((decimal)kpiTotal * 0.70m, 2);

            int softPct = 0;
            if (softs.Count > 0)
            {
                var sum = softs.Sum(x => x.Score);
                softPct = (int)Math.Round((double)sum / (softs.Count * 10) * 100);
            }
            decimal softWeighted = Math.Round((decimal)softPct * 0.30m, 2);
            decimal finalScore = Math.Round(kpiWeighted + softWeighted, 2);

            // ---- Logo path (optional)
            string? logoPath = new[]
            {
                Path.Combine(_env.WebRootPath, "img", "logo.png"),
                Path.Combine(_env.WebRootPath, "img", "alpago-logo.png"),
                Path.Combine(_env.WebRootPath, "logo.png")
            }.FirstOrDefault(File.Exists);

            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(s => s.FontSize(10));

                    page.Content().Column(col =>
                    {
                        // Comfortable spacing BETWEEN sections (medium)
                        col.Spacing(14);

                        // Header (kept tight)
                        col.Item().Element(e => BuildHeader(e, logoPath));

                        // SECTION 1: Employee Info
                        col.Item().Element(e =>
                        {
                            e.Column(sec =>
                            {
                                // Tight gap under section header
                                sec.Spacing(4);

                                sec.Item().Element(x => SectionHeader(x, "Section 1: Employee Information"));

                                // Minimal top padding so header-to-body gap stays small
                                sec.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Column(left =>
                                    {
                                        LabeledText(left, "Employee Name:", form.Employee.Name);
                                        LabeledText(left, "Employee ID:", form.Employee.EmpCode);
                                        LabeledText(left, "Designation:", form.Employee.Designation ?? "—");
                                        LabeledText(left, "Department:", form.Employee.Department ?? "—");
                                    });

                                    row.RelativeItem().Column(right =>
                                    {
                                        var mgr = form.Employee.Manager?.Name
                                                  ?? form.Employee.ManagerNameCached
                                                  ?? "—";
                                        LabeledText(right, "Reporting Manager:", mgr);
                                        LabeledText(right, "Date of Joining:", form.Employee.DateOfJoining?.ToString("dd/MM/yyyy") ?? "—");
                                        LabeledText(right, "Review Period:", $"{form.Cycle.Name} ({form.Cycle.Start:dd/MM/yyyy} – {form.Cycle.End:dd/MM/yyyy})");
                                    });
                                });
                            });
                        });

                        // SECTION 2: Self (Not used in scoring)
                        col.Item().Element(e =>
                        {
                            e.Column(sec =>
                            {
                                sec.Spacing(4);
                                sec.Item().Element(x => SectionHeader(x, "Section 2: Employee Self-Evaluation (Not used in scoring)"));
                                // Keep header-to-table tight
                                sec.Item().Element(x => BuildSelfTable(x, self, form.SelfComments));
                            });
                        });

                        // SECTION 3: KPI
                        col.Item().Element(e =>
                        {
                            e.Column(sec =>
                            {
                                sec.Spacing(4);
                                sec.Item().Element(x => SectionHeader(x, "Section 3: Manager KPI Evaluation (70%)"));
                                sec.Item().Element(x => BuildKpiTable(x, kpis));

                                // Small breathing room before the summary line only
                                sec.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Total KPI (average): ").SemiBold();
                                    t.Span($"{kpiTotal} / 100  ");
                                    t.Span("Weighted (70%): ").SemiBold();
                                    t.Span($"{kpiWeighted:0.##}");
                                });
                            });
                        });

                        // SECTION 4: Soft Skills
                        col.Item().Element(e =>
                        {
                            e.Column(sec =>
                            {
                                sec.Spacing(4);
                                sec.Item().Element(x => SectionHeader(x, "Section 4: Soft Skills Evaluation (30%)"));
                                sec.Item().Element(x => BuildSoftTable(x, softs));

                                // Match KPI summary spacing
                                sec.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Total Soft Skills: ").SemiBold();
                                    t.Span($"{softPct} / 100  ");
                                    t.Span("Weighted (30%): ").SemiBold();
                                    t.Span($"{softWeighted:0.##}");
                                });
                            });
                        });

                        // SECTION 5: Final Summary
                        col.Item().Element(e =>
                        {
                            e.Column(sec =>
                            {
                                sec.Spacing(4);
                                sec.Item().Element(x => SectionHeader(x, "Section 5: Final Summary & Comments"));

                                // Slight top padding for neat separation from header band
                                sec.Item().PaddingTop(4).Column(c =>
                                {
                                    c.Item().Text(t =>
                                    {
                                        t.Span("Final Score: ").SemiBold();
                                        t.Span($"{finalScore:0.##} / 100  ");
                                        t.Span("(computed: KPI ").Light();
                                        t.Span($"{kpiWeighted:0.##}").SemiBold();
                                        t.Span(" + Soft ").Light();
                                        t.Span($"{softWeighted:0.##}").SemiBold();
                                        t.Span(")").Light();
                                    });

                                    c.Item().PaddingTop(6).Row(r =>
                                    {
                                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(6)
                                            .Column(cc =>
                                            {
                                                cc.Item().Text("Manager Comments").SemiBold();
                                                cc.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(form.ManagerComments) ? "—" : form.ManagerComments);
                                            });

                                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(6)
                                            .Column(cc =>
                                            {
                                                cc.Item().Text("HR Comments").SemiBold();
                                                cc.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(form.HRComments) ? "—" : form.HRComments);
                                            });

                                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(6)
                                            .Column(cc =>
                                            {
                                                cc.Item().Text("CEO Comments").SemiBold();
                                                cc.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(form.CEOComments) ? "—" : form.CEOComments);
                                            });
                                    });
                                });
                            });
                        });
                    });

                    page.Footer().AlignRight().Text($"Generated {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
                });
            }).GeneratePdf();
        }

        // ===== helpers =====

        private static void BuildHeader(IContainer c, string? logoPath)
        {
            c.PaddingBottom(10).Row(r =>
            {
                r.RelativeItem(2).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                    {
                        col.Item().Height(24).Image(logoPath);
                    }
                    else
                    {
                        col.Item().Text("Alpago Properties").SemiBold().FontSize(14);
                    }

                    col.Item().Text("Employee Performance Evaluation Summary").FontSize(12);
                });

                r.RelativeItem().AlignRight().Text($"Ref: {Guid.NewGuid().ToString()[..8].ToUpper()}").FontSize(9).Light();
            });
        }

        private static void SectionHeader(IContainer c, string title)
        {
            c.PaddingVertical(8)
             .Background(Colors.Grey.Lighten4)
             .Border(1).BorderColor(Colors.Grey.Lighten3)
             .PaddingHorizontal(10)
             .Text(title).SemiBold();
        }

        private static void LabeledText(ColumnDescriptor column, string label, string value)
        {
            column.Item().PaddingVertical(2).Text(t =>
            {
                t.Span(label).SemiBold();
                t.Span(" ");
                t.Span(value);
            });
        }

        private static void BuildSelfTable(IContainer c, List<Responsibility> rows, string? selfComments)
        {
            if (rows.Count == 0)
            {
                c.PaddingVertical(6).Text("No self-evaluation submitted.").Light();
                return;
            }

            c.Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(3);
                    cd.RelativeColumn(5);
                    cd.RelativeColumn(2);
                });

                t.Header(h =>
                {
                    h.Cell().Element(x => HeaderCell(x, "Key Responsibility"));
                    h.Cell().Element(x => HeaderCell(x, "Description"));
                    h.Cell().Element(x => HeaderCell(x, "Achievement (%)", center: true));
                });

                bool alt = false;
                foreach (var r in rows)
                {
                    BodyCell(t, r.Title ?? "", alt);
                    BodyCell(t, string.IsNullOrWhiteSpace(r.Description) ? "—" : r.Description, alt);
                    BodyCell(t, r.AchievementPercent.ToString(), alt, center: true);
                    alt = !alt;
                }
            });

            if (!string.IsNullOrWhiteSpace(selfComments))
                c.PaddingTop(6).Text(t =>
                {
                    t.Span("Employee Comments: ").SemiBold();
                    t.Span(selfComments);
                });
        }

        private static void BuildKpiTable(IContainer c, List<KPIItem> rows)
        {
            if (rows.Count == 0)
            {
                c.PaddingVertical(6).Text("No KPI items.").Light();
                return;
            }

            c.Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(5);
                    cd.RelativeColumn(4);
                    cd.RelativeColumn(2);
                });

                t.Header(h =>
                {
                    h.Cell().Element(x => HeaderCell(x, "KPI"));
                    h.Cell().Element(x => HeaderCell(x, "Actual Performance"));
                    h.Cell().Element(x => HeaderCell(x, "Score (0–100)", center: true));
                });

                bool alt = false;
                foreach (var k in rows)
                {
                    BodyCell(t, k.Description ?? "", alt);
                    BodyCell(t, string.IsNullOrWhiteSpace(k.ActualPerformance) ? "—" : k.ActualPerformance, alt);
                    BodyCell(t, k.Score.ToString(), alt, center: true);
                    alt = !alt;
                }
            });
        }

        private static void BuildSoftTable(IContainer c, List<SoftSkillRating> rows)
        {
            if (rows.Count == 0)
            {
                c.PaddingVertical(6).Text("No soft-skill rows.").Light();
                return;
            }

            c.Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(8);
                    cd.RelativeColumn(2);
                });

                t.Header(h =>
                {
                    h.Cell().Element(x => HeaderCell(x, "Attribute"));
                    h.Cell().Element(x => HeaderCell(x, "Score (1–10)", center: true));
                });

                bool alt = false;
                foreach (var s in rows)
                {
                    BodyCell(t, s.Attribute, alt);
                    BodyCell(t, s.Score.ToString(), alt, center: true);
                    alt = !alt;
                }
            });
        }

        // ---- single-chain safe cells (fixes DocumentComposeException)
        private static void HeaderCell(IContainer container, string text, bool center = false)
        {
            var box = container
                .Background(Colors.Grey.Lighten4)
                .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5).PaddingHorizontal(6);

            if (center)
                box = box.AlignCenter();

            box.Text(text).SemiBold();
        }

        private static void BodyCell(TableDescriptor table, string text, bool alt, bool center = false)
        {
            var bg = alt ? Colors.Grey.Lighten5 : Colors.White;

            var cell = table.Cell()
                .Background(bg)
                .BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                .PaddingVertical(5).PaddingHorizontal(6);

            if (center)
                cell = cell.AlignCenter();

            cell.Text(text);
        }
    }
}
