using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppraisalPortal.Pages.Account
{
    [Authorize]
    public class ForceChangeModel : PageModel
    {
        private readonly UserManager<AppUser> _um;
        private readonly SignInManager<AppUser> _sm;

        public ForceChangeModel(UserManager<AppUser> um, SignInManager<AppUser> sm)
        { _um = um; _sm = sm; }

        [BindProperty] public Vm Input { get; set; } = new();

        public class Vm
        {
            [Required] public string Current { get; set; } = "";
            [Required] public string New { get; set; } = "";
            [Compare(nameof(New), ErrorMessage = "Passwords do not match.")]
            public string Confirm { get; set; } = "";
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _um.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var res = await _um.ChangePasswordAsync(user, Input.Current, Input.New);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            user.MustChangePassword = false;
            await _um.UpdateAsync(user);
            await _sm.RefreshSignInAsync(user);

            TempData["Msg"] = "Password updated.";
            return RedirectToPage("/Index");
        }
    }
}
