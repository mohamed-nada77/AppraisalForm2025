// =====================  /Pages/Account/Login.cshtml.cs  =====================
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppraisalPortal.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty] public InputModel Input { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            public string Login { get; set; } = string.Empty; // username or email

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ReturnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            // Find by username first; if not found try email
            AppUser? user = await _userManager.FindByNameAsync(Input.Login);
            if (user == null && Input.Login.Contains('@'))
                user = await _userManager.FindByEmailAsync(Input.Login);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return Page(); // <-- stay on login page
            }

            // Do not redirect on failure: show error and return Page()
            var result = await _signInManager.PasswordSignInAsync(
                user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");

                // If your app enforces an initial password change:
                if (user.MustChangePassword)
                {
                    // Make sure user is signed in; then redirect to change password
                    return RedirectToPage("/Account/ChangePassword", new { first = true, returnUrl = ReturnUrl });
                }

                return LocalRedirect(IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/"));
            }

            if (result.RequiresTwoFactor)
            {
                // If you don't use 2FA, treat as error:
                ModelState.AddModelError(string.Empty, "Two-factor authentication is required.");
                return Page();
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked due to too many attempts. Please try again later.");
                _logger.LogWarning("User account locked out.");
                return Page();
            }

            // Wrong password or other failure
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        private static bool IsLocalUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.StartsWith("/") && !url.StartsWith("//") && !url.StartsWith("/\\");
        }
    }
}
