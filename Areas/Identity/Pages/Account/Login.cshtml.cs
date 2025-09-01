using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppraisalPortal.Models;

namespace AppraisalPortal.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        public LoginModel(SignInManager<AppUser> signInManager) { _signInManager = signInManager; }

        [BindProperty] public InputModel Input { get; set; } = new();
        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required] public string EmpCode { get; set; } = string.Empty;
            [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
            [Display(Name = "Remember me?")] public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid) return Page();

            var result = await _signInManager.PasswordSignInAsync(Input.EmpCode, Input.Password, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded) return LocalRedirect(ReturnUrl);
            if (result.RequiresTwoFactor) return RedirectToPage("./LoginWith2fa", new { ReturnUrl, Input.RememberMe });
            if (result.IsLockedOut) ModelState.AddModelError(string.Empty, "User account locked out.");
            else ModelState.AddModelError(string.Empty, "Invalid Emp Code or password.");
            return Page();
        }
    }
}
