using System.Net;
using System.Text.RegularExpressions;
using HospitalManagement.Web.Data;
using HospitalManagement.Web.Models;
using HospitalManagement.Web.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.Tests;

public class AppointmentPaginationTests(HospitalFactory factory) : IClassFixture<HospitalFactory>
{
    private static int seedDay;

    private async Task<(string Search, DateOnly Date, int DoctorId, List<Appointment> Items)> Seed(int count, bool varied = false,
        BookingStatus status = BookingStatus.Pending)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctors = await db.Doctors.OrderBy(x => x.Id).Take(2).Select(x => x.Id).ToListAsync();
        var search = "Phân trang & + " + Guid.NewGuid().ToString("N")[..8];
        var date = HospitalClock.Today.AddDays(400 + Interlocked.Add(ref seedDay, 4));
        var items = Enumerable.Range(0, count).Select(i => new Appointment
        {
            DoctorId = doctors[varied ? i % 2 : 0],
            AppointmentDate = date.AddDays(varied ? i % 3 : 0),
            AppointmentTime = new TimeOnly(8, 0).AddMinutes(varied ? i / 6 : i),
            SerialNo = i + 1, PatientName = search + $" {i:D3}",
            PatientPhone = "0901234567", PatientEmail = "pagination@example.com", Status = status, Fee = 250000
        }).ToList();
        db.Appointments.AddRange(items);
        await db.SaveChangesAsync();
        return (search, date, doctors[0], items);
    }

    private async Task<HttpClient> Login(string email = "admin@example.com", string password = "admin123")
    {
        var client = factory.Client();
        var html = await Html(client, "/Account/Login");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email, ["Password"] = password,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static async Task<string> Html(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static string ListUrl(string search, string? page = null, DateOnly? date = null, BookingStatus? status = null, int? doctorId = null) =>
        QueryHelpers.AddQueryString("/Admin/Appointment", new Dictionary<string, string?>
        {
            ["q"] = search, ["page"] = page, ["date"] = date?.ToString("yyyy-MM-dd"),
            ["status"] = status?.ToString(), ["doctorId"] = doctorId?.ToString()
        });

    private static int[] RowIds(string html)
    {
        var body = Regex.Match(html, "<tbody>(.*?)</tbody>", RegexOptions.Singleline).Groups[1].Value;
        return Regex.Matches(body, "href=\"/Appointment/Details/(\\d+)\"")
            .Select(x => int.Parse(x.Groups[1].Value)).ToArray();
    }

    private static string Href(string tag) => WebUtility.HtmlDecode(Regex.Match(tag, "href=\"([^\"]+)\"").Groups[1].Value);

    [Fact]
    public async Task Pages_have_exact_rows_stable_order_and_no_duplicates()
    {
        var data = await Seed(45, varied: true);
        using var client = await Login();
        var expected = data.Items.OrderByDescending(x => x.AppointmentDate).ThenBy(x => x.AppointmentTime).ThenBy(x => x.Id)
            .Select(x => x.Id).ToArray();
        var allRows = new List<int>();
        for (var page = 1; page <= 3; page++)
        {
            var url = ListUrl(data.Search, page.ToString());
            var html = await Html(client, url);
            var rows = RowIds(html);
            Assert.Equal(expected.Skip((page - 1) * 20).Take(20), rows);
            Assert.Equal(rows, RowIds(await Html(client, url)));
            Assert.Contains($"Hiển thị {(page - 1) * 20 + 1}–{Math.Min(page * 20, 45)} / 45 lịch hẹn", WebUtility.HtmlDecode(html));
            Assert.Contains($"aria-current=\"page\" aria-label=\"Trang {page}\"", html);
            Assert.Equal(page > 1, html.Contains("rel=\"prev\""));
            Assert.Equal(page < 3, html.Contains("rel=\"next\""));
            allRows.AddRange(rows);
        }
        Assert.Equal(expected, allRows);
        Assert.Equal(45, allRows.Distinct().Count());
    }

    [Theory]
    [InlineData("0", 1)]
    [InlineData("-4", 1)]
    [InlineData("2147483647", 3)]
    [InlineData("not-a-number", 1)]
    [InlineData("99999999999999999999", 1)]
    public async Task Invalid_or_out_of_range_page_is_safe(string requestedPage, int expectedPage)
    {
        var data = await Seed(41);
        using var client = await Login();
        var html = await Html(client, ListUrl(data.Search, requestedPage));
        Assert.Equal(data.Items.Skip((expectedPage - 1) * 20).Take(20).Select(x => x.Id), RowIds(html));
        Assert.Contains($"aria-current=\"page\" aria-label=\"Trang {expectedPage}\"", html);
    }

    [Fact]
    public async Task Every_filter_is_applied_before_counting_and_preserved_in_page_links()
    {
        var data = await Seed(25);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var otherDoctor = await db.Doctors.Where(x => x.Id != data.DoctorId).Select(x => x.Id).FirstAsync();
            db.Appointments.AddRange(Enumerable.Range(0, 4).Select(i => new Appointment
            {
                DoctorId = i == 0 ? otherDoctor : data.DoctorId,
                AppointmentDate = i == 1 ? data.Date.AddDays(1) : data.Date,
                AppointmentTime = new TimeOnly(12, i), SerialNo = 100 + i,
                PatientName = i == 3 ? "Không khớp từ khóa" : data.Search,
                PatientPhone = "0901234567", PatientEmail = "pagination@example.com",
                Status = i == 2 ? BookingStatus.Cancelled : BookingStatus.Pending
            }));
            await db.SaveChangesAsync();
        }
        using var client = await Login();
        var html = await Html(client, ListUrl(data.Search, "1", data.Date, BookingStatus.Pending, data.DoctorId));
        Assert.Equal(data.Items.Take(20).Select(x => x.Id), RowIds(html));
        Assert.Contains("Hiển thị 1–20 / 25 lịch hẹn", WebUtility.HtmlDecode(html));
        var links = Regex.Matches(html, "<a\\b[^>]*>").Select(x => x.Value).Where(x => x.Contains("class=\"page-link\""));
        foreach (var link in links)
        {
            var uri = new Uri(new Uri("http://localhost"), Href(link));
            var query = QueryHelpers.ParseQuery(uri.Query);
            Assert.Equal(data.Search, query["q"].ToString());
            Assert.Equal(data.Date.ToString("yyyy-MM-dd"), query["date"].ToString());
            Assert.Equal("Pending", query["status"].ToString());
            Assert.Equal(data.DoctorId.ToString(), query["doctorId"].ToString());
        }
        var next = Regex.Matches(html, "<a\\b[^>]*>").Select(x => x.Value).Single(x => x.Contains("rel=\"next\""));
        var secondPage = await Html(client, Href(next));
        Assert.Equal(data.Items.Skip(20).Select(x => x.Id), RowIds(secondPage));
        Assert.Contains("Hiển thị 21–25 / 25 lịch hẹn", WebUtility.HtmlDecode(secondPage));

        var filterForm = Regex.Match(secondPage, "<form\\b[^>]*class=\"filter-panel\"[^>]*>.*?</form>", RegexOptions.Singleline).Value;
        Assert.NotEmpty(filterForm);
        Assert.DoesNotContain("name=\"page\"", filterForm);
        Assert.DoesNotContain("page=", filterForm);
        var reset = Regex.Match(secondPage, "<a\\b[^>]*>Xóa lọc</a>").Value;
        Assert.NotEmpty(reset);
        Assert.DoesNotContain("?", Href(reset));
        Assert.Contains(data.Items[0].Id, RowIds(await Html(client, ListUrl(data.Search, date: data.Date, status: BookingStatus.Pending, doctorId: data.DoctorId))));
    }

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Cancelled)]
    public async Task Updating_last_row_keeps_filters_and_returns_to_existing_page(BookingStatus before, BookingStatus after)
    {
        var data = await Seed(21, status: before);
        using var client = await Login();
        var html = await Html(client, ListUrl(data.Search, "2", data.Date, before, data.DoctorId));
        var form = Regex.Matches(html, "<form\\b[^>]*>.*?</form>", RegexOptions.Singleline)
            .Select(x => x.Value).Single(x => x.Contains($"name=\"status\" type=\"hidden\" value=\"{after}\""));
        var fields = Regex.Matches(form, "name=\"([^\"]+)\"[^>]*value=\"([^\"]*)\"")
            .ToDictionary(x => x.Groups[1].Value, x => WebUtility.HtmlDecode(x.Groups[2].Value));
        Assert.Equal("2", fields["page"]);
        Assert.Equal(before.ToString(), fields["filterStatus"]);
        fields["reason"] = "Kiểm tra phân trang sau khi hủy";
        var response = await client.PostAsync("/Admin/Appointment/Status", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        var query = QueryHelpers.ParseQuery(new Uri(new Uri("http://localhost"), location).Query);
        Assert.Equal("2", query["page"].ToString());
        Assert.Equal(data.Search, query["q"].ToString());
        Assert.Equal(before.ToString(), query["status"].ToString());
        Assert.Equal(data.Date.ToString("yyyy-MM-dd"), query["date"].ToString());
        Assert.Equal(data.DoctorId.ToString(), query["doctorId"].ToString());
        var updatedPage = await Html(client, location.ToString());
        Assert.Equal(data.Items.Take(20).Select(x => x.Id), RowIds(updatedPage));
        Assert.Contains("Hiển thị 1–20 / 20 lịch hẹn", WebUtility.HtmlDecode(updatedPage));
        Assert.DoesNotContain("aria-label=\"Phân trang lịch hẹn khám\"", WebUtility.HtmlDecode(updatedPage));
    }

    [Fact]
    public async Task Empty_and_single_page_results_have_no_pager_and_staff_can_use_pagination()
    {
        var data = await Seed(3);
        using var client = await Login("staff@example.com", "staff123");
        var html = await Html(client, ListUrl(data.Search, "2147483647"));
        Assert.Equal(data.Items.Select(x => x.Id), RowIds(html));
        Assert.Contains("Hiển thị 1–3 / 3 lịch hẹn", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("class=\"pagination ", html);
        var empty = await Html(client, ListUrl(Guid.NewGuid().ToString("N"), "2147483647"));
        Assert.Empty(RowIds(empty));
        Assert.Contains("Không có lịch hẹn phù hợp.", WebUtility.HtmlDecode(empty));
        Assert.DoesNotContain("class=\"pagination ", empty);
        Assert.DoesNotContain("Hiển thị", WebUtility.HtmlDecode(empty));
    }
}
