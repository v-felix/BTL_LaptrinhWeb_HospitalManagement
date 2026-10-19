namespace HospitalManagement.Web.Models.ViewModels;

public class AppointmentListVM
{
    public const int PageSize = 20;

    public IReadOnlyList<Appointment> Appointments { get; init; } = [];
    public IReadOnlyList<Doctor> Doctors { get; init; } = [];
    public DateOnly? Date { get; init; }
    public BookingStatus? Status { get; init; }
    public int? DoctorId { get; init; }
    public string? Q { get; init; }
    public int PageNumber { get; init; } = 1;
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public int FirstItem => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int LastItem => (PageNumber - 1) * PageSize + Appointments.Count;
    public int StartPage => Math.Max(1, Math.Min(PageNumber - 2, TotalPages - 4));
    public int EndPage => Math.Min(TotalPages, StartPage + 4);
}
