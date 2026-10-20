using System.Diagnostics;
using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Models.ViewModels;
using HospitalManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(new HomeVM
    {
        Doctors = await db.Doctors.AsNoTracking().Include(x => x.User).Where(x => x.IsActive && x.User.IsActive).OrderBy(x => x.Id).Take(4).ToListAsync(),
        Packages = await db.HealthChecks.AsNoTracking().Where(x => x.IsActive).ToListAsync(),
        News = await db.News.AsNoTracking().Where(x => x.IsPublished && x.PublishedDate <= HospitalClock.Today).OrderByDescending(x => x.PublishedDate).Take(3).ToListAsync(),
        DoctorCount = await db.Doctors.CountAsync(x => x.IsActive && x.User.IsActive), ServiceCount = await db.DiagnosticServices.CountAsync(x => x.IsActive)
    });
    public IActionResult About() => View();
    public async Task<IActionResult> Doctors(string? q, string? speciality)
    {
        var query = db.Doctors.AsNoTracking().Include(x => x.User).Where(x => x.IsActive && x.User.IsActive);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.User.FullName.Contains(q) || x.Speciality.Contains(q));
        if (!string.IsNullOrWhiteSpace(speciality)) query = query.Where(x => x.Speciality == speciality);
        ViewBag.Specialities = await db.Doctors.Where(x => x.IsActive && x.User.IsActive).Select(x => x.Speciality).Distinct().OrderBy(x => x).ToListAsync();
        ViewBag.Q = q; ViewBag.Speciality = speciality;
        return View(await query.OrderBy(x => x.User.FullName).ToListAsync());
    }
    public async Task<IActionResult> Doctor(int id)
    {
        var doctor = await db.Doctors.AsNoTracking().Include(x => x.User).Include(x => x.Schedules).FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.User.IsActive);
        return doctor == null ? NotFound() : View(doctor);
    }
    public async Task<IActionResult> News(string? q)
    {
        var query = db.News.AsNoTracking().Where(x => x.IsPublished && x.PublishedDate <= HospitalClock.Today);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Title.Contains(q) || x.Category.Contains(q));
        ViewBag.Q = q; return View(await query.OrderByDescending(x => x.PublishedDate).ToListAsync());
    }
    public async Task<IActionResult> Article(int id)
    {
        var article = await db.News.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsPublished && x.PublishedDate <= HospitalClock.Today);
        return article == null ? NotFound() : View(article);
    }
    [Route("Home/HttpError/{statusCode:int:range(400,599)}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpError(int statusCode)
    {
        Response.StatusCode = statusCode;
        return statusCode == 404 ? View("NotFound") : StatusCode(statusCode);
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
