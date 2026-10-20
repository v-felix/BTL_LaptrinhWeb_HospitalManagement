using System.Globalization;
using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("vi-VN");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, LegacyPasswordHasher>();
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "HospitalManagement.Auth";
    options.Cookie.HttpOnly = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnValidatePrincipal = async context =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);
        if (context.Principal?.Identity?.IsAuthenticated == true)
        {
            var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await manager.GetUserAsync(context.Principal);
            if (user?.IsActive != true) { context.RejectPrincipal(); await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme); }
        }
    };
});
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<AccessService>();

var app = builder.Build();
if (app.Configuration.GetValue<bool>("Database:Initialize") || args.Contains("--initialize-only"))
{
    using var scope = app.Services.CreateScope();
    await DbInitializer.InitializeAsync(scope.ServiceProvider, app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:SeedDemoData"));
}
if (args.Contains("--initialize-only")) return;
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program { }
