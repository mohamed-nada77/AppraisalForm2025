// /Pages/HR/Review.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.HR
{
    [Authorize(Roles = "HR,Admin")]
    public class ReviewModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _um;
        public ReviewModel(ApplicationDbContext db, UserManager<AppUser> um) { _db = db; _um = um; }

        public int FormId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string CycleName { get; set; } = "";
        [BindProperty] public string? HRComments { get; set; }
        public string? Error { get; set; }

        public int KpiPercent { get; set; }
        public int SoftPercent { get; set; }
        public decimal KpiWeighted { get; set; }
        public decimal SoftWeighted { get; set; }
        public decimal FinalScore { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var f = await _db.Forms
                .Include(x => x.Employee)
                .Include(x => x.Cycle)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return NotFound();

            FormId = id;
            EmployeeName = f.Employee.Name;
            CycleName = f.Cycle.Name;
            HRComments = f.HRComments;

            // Recompute (trusted)
            var kpis = await _db.KPIItems.Where(k => k.FormId == id).Select(k => Math.Clamp(k.Score, 0, 100)).ToListAsync();
            KpiPercent = kpis.Count > 0 ? (int)Math.Round(kpis.Average(), MidpointRounding.AwayFromZero) : 0;

            var soft = await _db.SoftSkillRatings.Where(s => s.FormId == id).Select(s => Math.Clamp(s.Score, 1, 10)).ToListAsync();
            SoftPercent = soft.Count > 0 ? (int)Math.Round(((decimal)soft.Sum() / (soft.Count * 10m)) * 100m, MidpointRounding.AwayFromZero) : 0;

            KpiWeighted = Math.Round(KpiPercent * 0.70m, 2);
            SoftWeighted = Math.Round(SoftPercent * 0.30m, 2);
            FinalScore = Math.Round(KpiWeighted + SoftWeighted, 2);

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var f = await _db.Forms.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return NotFound();

            // Only HR manager (EmpCode 88) OR Admin may approve.
            var user = await _um.GetUserAsync(User);
            var me = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == user!.Id);
            var isHrManager88 = me?.EmpCode?.Trim() == "88";
            var isAdmin = User.IsInRole("Admin");
            if (!isHrManager88 && !isAdmin)
            {
                Error = "Only HR Manager (EmpCode 88) or Admin can approve.";
                await OnGetAsync(id);
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(HRComments))
                f.HRComments = HRComments.Trim();

            f.Status = "HRReviewed";
            f.FinalScore ??= f.ManagerScore; // keep if already set; else accept manager score
            await _db.SaveChangesAsync();
            return RedirectToPage("/HR/All");
        }
    }
}
