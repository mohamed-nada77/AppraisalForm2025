// /Program.cs
using AppraisalPortal;
using AppraisalPortal.Models;
using AppraisalPortal.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.IO.Compression;   // <-- add
using AppraisalPortal.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<PdfService>();

// QuestPDF license (REQUIRED)
QuestPDF.Settings.License = LicenseType.Community;

// Data Protection keys
var keysPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "AppraisalPortal", "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("AppraisalPortal");

// Services & DB
builder.Services.AddScoped<PasswordRuleService>();
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddDefaultIdentity<AppUser>(o =>
{
    o.SignIn.RequireConfirmedAccount = false;
    o.User.RequireUniqueEmail = false;
    o.Password.RequiredLength = 8;
    o.Password.RequireDigit = true;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireUppercase = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI();

builder.Services.AddScoped<IClaimsTransformation, ScopeRoleClaimsTransformer>();

/*
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = ".AppraisalPortal.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.SlidingExpiration = true;
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.LoginPath = "/";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
});*/

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.AddRazorPages();
builder.Services.AddScoped<EmployeeImportService>();
builder.Services.AddScoped<ScoringService>();



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();

// global force-password-change gate
app.Use(async (ctx, next) =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? "";
        var exempt = path.StartsWith("/account/forcechange")
                  || path.StartsWith("/identity")
                  || path.StartsWith("/css")
                  || path.StartsWith("/js")
                  || path.StartsWith("/img");
        if (!exempt)
        {
            var um = ctx.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var user = await um.GetUserAsync(ctx.User);
            if (user?.MustChangePassword == true)
            {
                ctx.Response.Redirect("/Account/ForceChange");
                return;
            }
        }
    }
    await next();
});

app.UseAuthorization();

app.MapRazorPages();

// single-PDF
app.MapGet("/reports/{id:int}/pdf",
    [Authorize(Roles = "HR,CEO,Admin")] async (int id, PdfService pdf) =>
    {
        var bytes = await pdf.SummaryPdfAsync(id);
        return Results.File(bytes, "application/pdf", $"Appraisal_{id}.pdf");
    });

// bulk ZIP by cycle
app.MapGet("/reports/cycle/{cycleId:int}/pdf.zip",
    [Authorize(Roles = "HR,CEO,Admin")] async (int cycleId, ApplicationDbContext db, PdfService pdf) =>
    {
        var formIds = await db.Forms
            .Where(f => f.CycleId == cycleId && (f.Status == "HRReviewed" || f.Status == "Finalized"))
            .Select(f => f.Id)
            .ToListAsync();

        if (formIds.Count == 0)
            return Results.NotFound($"No approved forms found for cycle #{cycleId}.");

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var id in formIds)
            {
                var pdfBytes = await pdf.SummaryPdfAsync(id);
                var entry = zip.CreateEntry($"Appraisal_{id}.pdf", CompressionLevel.Fastest);
                await using var es = entry.Open();
                await es.WriteAsync(pdfBytes);
            }
        }
        ms.Position = 0;
        return Results.File(ms.ToArray(), "application/zip", $"Appraisals_Cycle_{cycleId}.zip");
    });

// seed
using (var scope = app.Services.CreateScope())
{
    await SeedData.RunAsync(scope.ServiceProvider);
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    if (config.GetValue<bool>("ResetAdmin"))
    {
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var admin = await userMgr.FindByNameAsync("ADMIN");
        if (admin != null)
        {
            var token = await userMgr.GeneratePasswordResetTokenAsync(admin);
            await userMgr.ResetPasswordAsync(admin, token, "Admin#12345");
        }
    }
}

app.Run();
