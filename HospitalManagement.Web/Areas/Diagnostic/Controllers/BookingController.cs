using System.Security.Claims;
using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Models.ViewModels;
using HospitalManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Areas.Diagnostic.Controllers;

[Area("Diagnostic"), Authorize(Roles = "Diagnostic,Admin")]
public class BookingController(ApplicationDbContext db, IWebHostEnvironment env, NotificationService mail) : Controller
{
    public async Task<IActionResult> Index(DateOnly? date, BookingStatus? status)
    {
        var query = db.DiagnosticBookings.AsNoTracking().Include(x => x.DiagnosticService).AsQueryable();
        if (date != null) query = query.Where(x => x.BookingDate == date);
        if (status != null) query = query.Where(x => x.Status == status);
        ViewBag.Date = date; ViewBag.Status = status;
        return View(await query.OrderByDescending(x => x.BookingDate).ThenBy(x => x.BookingTime).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Status(int id, BookingStatus status, PaymentStatus paymentStatus, string rowVersion)
    {
        var booking = await db.DiagnosticBookings.FindAsync(id); if (booking == null) return NotFound();
        if (!Enum.IsDefined(paymentStatus) || !Enum.IsDefined(status) || status == BookingStatus.Completed ||
            booking.Status is BookingStatus.Completed or BookingStatus.Cancelled || (status != booking.Status && !AppointmentService.CanTransition(booking.Status, status))) return BadRequest("Trạng thái không hợp lệ.");
        if (!AccessService.VersionMatches(booking.RowVersion, rowVersion)) return Conflict("Phiếu đã thay đổi.");
        booking.Status = status; booking.PaymentStatus = paymentStatus;
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict("Phiếu đã thay đổi."); }
        TempData["Success"] = "Đã cập nhật tiếp nhận xét nghiệm.";
        return RedirectToAction(nameof(Index));
    }
    [HttpGet] public async Task<IActionResult> Result(int id)
    {
        var booking = await db.DiagnosticBookings.Include(x => x.DiagnosticService).FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null) return NotFound();
        if (booking.Status != BookingStatus.Confirmed) return BadRequest("Cần xác nhận phiếu trước khi trả kết quả.");
        ViewBag.Booking = booking; return View(new ResultVM { BookingId = id, RowVersion = Convert.ToBase64String(booking.RowVersion) });
    }
    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)] public async Task<IActionResult> Result(ResultVM model)
    {
        var booking = await db.DiagnosticBookings.Include(x => x.DiagnosticService).Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == model.BookingId);
        if (booking == null) return NotFound();
        if (booking.Status != BookingStatus.Confirmed) return BadRequest("Phiếu không ở trạng thái đã xác nhận.");
        if (!AccessService.VersionMatches(booking.RowVersion, model.RowVersion)) return Conflict("Phiếu đã thay đổi.");
        if (model.File != null)
        {
            if (model.File.Length is < 5 or > 5242880 || !Path.GetExtension(model.File.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.File), "Chỉ nhận PDF, tối đa 5 MB.");
            else
            {
                using var stream = model.File.OpenReadStream(); var signature = new byte[5];
                await stream.ReadExactlyAsync(signature);
                if (!signature.SequenceEqual("%PDF-"u8.ToArray())) ModelState.AddModelError(nameof(model.File), "Tệp không phải PDF hợp lệ.");
            }
        }
        if (!ModelState.IsValid) { ViewBag.Booking = booking; return View(model); }
        string? fileName = null, filePath = null;
        if (model.File != null)
        {
            var directory = Path.Combine(env.ContentRootPath, "App_Data", "Results"); Directory.CreateDirectory(directory);
            fileName = Guid.NewGuid().ToString("N") + ".pdf"; filePath = Path.Combine(directory, fileName);
            await using var stream = System.IO.File.Create(filePath); await model.File.CopyToAsync(stream);
        }
        db.TestResults.Add(new TestResult { BookingDiagnosticId = booking.Id, Content = model.Content, Conclusion = model.Conclusion,
            FileName = fileName, CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)! });
        booking.Status = BookingStatus.Completed;
        try { await db.SaveChangesAsync(); }
        catch
        {
            if (filePath != null) System.IO.File.Delete(filePath);
            throw;
        }
        await mail.SendAsync(booking.Patient.Email!, "Kết quả xét nghiệm " + booking.Reference,
            mail.AbsoluteUrl(Url.Action("Details", "Diagnostic", new { area = "", id = booking.Id })!));
        TempData["Success"] = "Đã trả kết quả cho bệnh nhân.";
        return RedirectToAction("Details", "Diagnostic", new { area = "", id = booking.Id });
    }
}
