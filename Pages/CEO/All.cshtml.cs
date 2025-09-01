// /Pages/CEO/All.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.CEO
{
    [Authorize(Roles = "CEO,Admin")]
    public class AllModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public AllModel(ApplicationDbContext db) { _db = db; }

        public IList<Form> Items { get; private set; } = new List<Form>();

        public async Task OnGetAsync()
        {
            Items = await _db.Forms
                .Include(f => f.Employee)
                .Include(f => f.Cycle)
                .Where(f => f.Status == "HRReviewed" || f.Status == "Finalized")
                .OrderByDescending(f => f.Id)
                .ToListAsync();
        }
    }
}
