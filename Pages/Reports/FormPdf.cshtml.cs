using AppraisalPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppraisalPortal.Pages.Reports
{
    [Authorize(Roles = "HR,Admin,CEO,Manager")]
    public class FormPdfModel : PageModel
    {
        private readonly PdfService _pdf;

        public FormPdfModel(PdfService pdf)
        {
            _pdf = pdf;
        }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
        {
            var bytes = await _pdf.SummaryPdfAsync(id, ct);
            return File(bytes, "application/pdf", $"Appraisal_{id}.pdf");
        }
    }
}
