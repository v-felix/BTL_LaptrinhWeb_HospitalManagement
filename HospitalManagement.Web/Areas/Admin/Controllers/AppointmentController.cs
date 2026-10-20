using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Models.ViewModels;
using HospitalManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin,Staff")]
public class AppointmentController(ApplicationDbContext db, NotificationService mail) : Controller
{
    public async Task<IActionResult> Index(DateOnly? date, BookingStatus? status, int? doctorId, string? q, int page = 1)
    {
        var query = db.Appointments.AsNoTracking().Include(x => x.Doctor.User).AsQueryable();
        if (date != null) query = query.Where(x => x.AppointmentDate == date);
        if (status != null) query = query.Where(x => x.Status == status);
        if (doctorId != null) query = query.Where(x => x.DoctorId == doctorId);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.PatientName.Contains(q) || x.PatientPhone.Contains(q) || x.Reference.Contains(q));
        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)AppointmentListVM.PageSize));
        page = Math.Clamp(page, 1, totalPages);
        return View(new AppointmentListVM
        {
            Appointments = await query.OrderByDescending(x => x.AppointmentDate).ThenBy(x => x.AppointmentTime).ThenBy(x => x.Id)
                .Skip((page - 1) * AppointmentListVM.PageSize).Take(AppointmentListVM.PageSize).ToListAsync(),
            Doctors = await db.Doctors.AsNoTracking().Include(x => x.User).ToListAsync(),
            Date = date, Status = status, DoctorId = doctorId, Q = q,
            PageNumber = page, TotalCount = totalCount
        });
    }
    [HttpPost] public async Task<IActionResult> Status(int id, BookingStatus status, string? reason, string rowVersion,
        DateOnly? date, BookingStatus? filterStatus, int? doctorId, string? q, int page = 1)
    {
        var appointment = await db.Appointments.FindAsync(id); if (appointment == null) return NotFound();
        if (status == BookingStatus.Completed || !AppointmentService.CanTransition(appointment.Status, status)) return BadRequest("Chuyển trạng thái không hợp lệ.");
        if (status == BookingStatus.Cancelled && (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)) return BadRequest("Nhập lý do hủy.");
        if (!AccessService.VersionMatches(appointment.RowVersion, rowVersion)) return Conflict("Lịch khám đã thay đổi. Vui lòng tải lại trang.");
        appointment.Status = status; appointment.CancellationReason = status == BookingStatus.Cancelled ? reason : null;
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict("Lịch khám đã thay đổi."); }
        await mail.SendAsync(appointment.PatientEmail, "Cập nhật lịch " + appointment.Reference, Display.Status(status) + (reason == null ? "" : ": " + reason));
        TempData["Success"] = "Đã cập nhật lịch khám.";
        return RedirectToAction(nameof(Index), new { date = date?.ToString("yyyy-MM-dd"), status = filterStatus, doctorId, q, page });
    }
}
