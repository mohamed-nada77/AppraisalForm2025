public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }

    // NEW: force reset on first login
    public bool MustChangePassword { get; set; } = true;
}
