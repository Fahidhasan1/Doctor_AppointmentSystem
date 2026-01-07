using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorRevenueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorRevenueController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /DoctorRevenue/RevenueReport
        [HttpGet]
        public async Task<IActionResult> RevenueReport(
            string mode = "month",
            int? year = null,
            int? month = null,
            string? start = null,
            string? end = null)
        {
            // ✅ Sidebar info (same pattern as admin)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var name = (currentUser.FirstName + " " + currentUser.LastName)?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = User.Identity?.Name ?? "Doctor";

            ViewBag.CurrentUserName = name;
            ViewBag.ProfileImagePath = currentUser.ProfileImagePath;

            // Top title like other pages
            ViewBag.PageTopTitle = "Sunshine Hospital";

            // ✅ Get active doctor profile id for this logged-in doctor
            var doctorProfileId = await _context.DoctorProfiles
                .AsNoTracking()
                .Where(d => d.UserId == currentUser.Id && d.IsActive)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            if (!doctorProfileId.HasValue)
            {
                TempData["ErrorMessage"] = "Doctor profile not found or inactive.";
                return RedirectToAction("Index", "DoctorDashboard");
            }

            var nowUtc = DateTime.UtcNow;
            mode = (mode ?? "month").Trim().ToLowerInvariant();

            DateTime fromUtc;
            DateTime toUtcExclusive;

            var y = year ?? nowUtc.Year;
            var m = month ?? nowUtc.Month;

            if (mode == "month")
            {
                fromUtc = new DateTime(y, m, 1);
                toUtcExclusive = fromUtc.AddMonths(1);
            }
            else if (mode == "last7")
            {
                toUtcExclusive = nowUtc.Date.AddDays(1);
                fromUtc = toUtcExclusive.AddDays(-7);
            }
            else if (mode == "last15")
            {
                toUtcExclusive = nowUtc.Date.AddDays(1);
                fromUtc = toUtcExclusive.AddDays(-15);
            }
            else if (mode == "custom")
            {
                if (!DateTime.TryParse(start, out var s) || !DateTime.TryParse(end, out var e))
                {
                    fromUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1);
                    toUtcExclusive = fromUtc.AddMonths(1);
                    mode = "month";
                }
                else
                {
                    fromUtc = s.Date;
                    toUtcExclusive = e.Date.AddDays(1);
                }
            }
            else
            {
                fromUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1);
                toUtcExclusive = fromUtc.AddMonths(1);
                mode = "month";
            }

            // ✅ Query: only paid payments for THIS doctor
            int did = doctorProfileId.Value;

            IQueryable<Payment> query = _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive
                            && p.Status == PaymentStatus.Paid
                            && p.PaidAtUtc.HasValue
                            && p.PaidAtUtc.Value >= fromUtc
                            && p.PaidAtUtc.Value < toUtcExclusive
                            && p.Appointment != null
                            && p.Appointment.DoctorProfileId == did);

            query = query
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(pt => pt.User);

            var total = await query.SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var rows = await query
                .OrderByDescending(p => p.PaidAtUtc)
                .Select(p => new DoctorRevenueRowViewModel
                {
                    Date = p.PaidAtUtc!.Value,
                    PaymentMethod = p.Method.ToString(),
                    Amount = p.Amount,

                    PatientName =
                        p.Appointment != null &&
                        p.Appointment.Patient != null &&
                        p.Appointment.Patient.User != null
                            ? (p.Appointment.Patient.User.FirstName + " " + p.Appointment.Patient.User.LastName).Trim()
                            : (p.Appointment != null && !string.IsNullOrWhiteSpace(p.Appointment.UnregisteredPatientName)
                                ? p.Appointment.UnregisteredPatientName
                                : "—"),

                    PatientPhone =
                        p.Appointment != null &&
                        p.Appointment.Patient != null &&
                        p.Appointment.Patient.User != null &&
                        !string.IsNullOrWhiteSpace(p.Appointment.Patient.User.PhoneNumber)
                            ? p.Appointment.Patient.User.PhoneNumber
                            : (p.Appointment != null && !string.IsNullOrWhiteSpace(p.Appointment.UnregisteredPatientPhone)
                                ? p.Appointment.UnregisteredPatientPhone
                                : "—")
                })
                .ToListAsync();

            var vm = new DoctorRevenueReportViewModel
            {
                RangeType = ModeToRangeType(mode),
                Year = y,
                Month = m,
                StartDate = (mode == "custom" && DateTime.TryParse(start, out var s2)) ? s2.Date : null,
                EndDate = (mode == "custom" && DateTime.TryParse(end, out var e2)) ? e2.Date : null,
                RangeLabel = $"Range: {fromUtc:dd MMM yyyy} \u2013 {toUtcExclusive.AddDays(-1):dd MMM yyyy}",
                Rows = rows,
                TotalRevenue = total
            };

            ViewData["Title"] = "Revenue Report";
            return View(vm);
        }


[HttpGet]
    public async Task<IActionResult> RevenueReportPdf(
    string mode = "month",
    int? year = null,
    int? month = null,
    string? start = null,
    string? end = null)
    {
        // ✅ identify current doctor
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var doctorProfileId = await _context.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.UserId == currentUser.Id && d.IsActive)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync();

        if (!doctorProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "Doctor profile not found or inactive.";
            return RedirectToAction("Index", "DoctorDashboard");
        }

        var nowUtc = DateTime.UtcNow;
        mode = (mode ?? "month").Trim().ToLowerInvariant();

        DateTime fromUtc;
        DateTime toUtcExclusive;

        var y = year ?? nowUtc.Year;
        var m = month ?? nowUtc.Month;

        if (mode == "month")
        {
            fromUtc = new DateTime(y, m, 1);
            toUtcExclusive = fromUtc.AddMonths(1);
        }
        else if (mode == "last7")
        {
            toUtcExclusive = nowUtc.Date.AddDays(1);
            fromUtc = toUtcExclusive.AddDays(-7);
        }
        else if (mode == "last15")
        {
            toUtcExclusive = nowUtc.Date.AddDays(1);
            fromUtc = toUtcExclusive.AddDays(-15);
        }
        else if (mode == "custom")
        {
            if (!DateTime.TryParse(start, out var s) || !DateTime.TryParse(end, out var e))
            {
                mode = "month";
                fromUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1);
                toUtcExclusive = fromUtc.AddMonths(1);
                y = nowUtc.Year;
                m = nowUtc.Month;
            }
            else
            {
                fromUtc = s.Date;
                toUtcExclusive = e.Date.AddDays(1);
            }
        }
        else
        {
            mode = "month";
            fromUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1);
            toUtcExclusive = fromUtc.AddMonths(1);
            y = nowUtc.Year;
            m = nowUtc.Month;
        }

        int did = doctorProfileId.Value;

        IQueryable<Payment> query = _context.Payments
            .AsNoTracking()
            .Where(p => p.IsActive
                        && p.Status == PaymentStatus.Paid
                        && p.PaidAtUtc.HasValue
                        && p.PaidAtUtc.Value >= fromUtc
                        && p.PaidAtUtc.Value < toUtcExclusive
                        && p.Appointment != null
                        && p.Appointment.DoctorProfileId == did);

        query = query
            .Include(p => p.Appointment)
                .ThenInclude(a => a.Patient)
                    .ThenInclude(pt => pt.User);

        var rows = await query
            .OrderByDescending(p => p.PaidAtUtc)
            .Select(p => new DoctorRevenueRowViewModel
            {
                Date = p.PaidAtUtc!.Value,
                PatientName =
                    p.Appointment != null &&
                    p.Appointment.Patient != null &&
                    p.Appointment.Patient.User != null
                        ? (p.Appointment.Patient.User.FirstName + " " + p.Appointment.Patient.User.LastName).Trim()
                        : (p.Appointment != null && !string.IsNullOrWhiteSpace(p.Appointment.UnregisteredPatientName)
                            ? p.Appointment.UnregisteredPatientName
                            : "—"),

                PatientPhone =
                    p.Appointment != null &&
                    p.Appointment.Patient != null &&
                    p.Appointment.Patient.User != null &&
                    !string.IsNullOrWhiteSpace(p.Appointment.Patient.User.PhoneNumber)
                        ? p.Appointment.Patient.User.PhoneNumber
                        : (p.Appointment != null && !string.IsNullOrWhiteSpace(p.Appointment.UnregisteredPatientPhone)
                            ? p.Appointment.UnregisteredPatientPhone
                            : "—"),

                PaymentMethod = p.Method.ToString(),
                Amount = p.Amount
            })
            .ToListAsync();

        var total = await query.SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var vm = new DoctorRevenueReportViewModel
        {
            RangeType = mode switch
            {
                "last7" => "Last7",
                "last15" => "Last15",
                "custom" => "Custom",
                _ => "Month"
            },
            Year = y,
            Month = m,
            StartDate = (mode == "custom" && DateTime.TryParse(start, out var s2)) ? s2.Date : null,
            EndDate = (mode == "custom" && DateTime.TryParse(end, out var e2)) ? e2.Date : null,
            RangeLabel = $"Range: {fromUtc:dd MMM yyyy} – {toUtcExclusive.AddDays(-1):dd MMM yyyy}",
            Rows = rows,
            TotalRevenue = total
        };

        // ✅ Generate PDF
        QuestPDF.Settings.License = LicenseType.Community;
        var pdfBytes = new DoctorRevenueReportPdfDocument(vm).GeneratePdf();

        var fileName = $"DoctorRevenueReport_{mode}_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

        public class DoctorRevenueReportPdfDocument : IDocument
        {
            private readonly DoctorRevenueReportViewModel _model;

            public DoctorRevenueReportPdfDocument(DoctorRevenueReportViewModel model)
            {
                _model = model;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Sunshine Hospital").FontSize(18).SemiBold();
                        col.Item().Text("Revenue Report").FontSize(14).SemiBold();

                        col.Item().Text(_model.RangeLabel ?? "")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken2);

                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Date
                                columns.RelativeColumn(4); // Patient
                                columns.RelativeColumn(3); // Patient Number
                                columns.RelativeColumn(2); // Method
                                columns.RelativeColumn(2); // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeader).Text("Date");
                                header.Cell().Element(CellHeader).Text("Patient");
                                header.Cell().Element(CellHeader).Text("Patient Number");
                                header.Cell().Element(CellHeader).Text("Method");
                                header.Cell().Element(CellHeader).AlignRight().Text("Amount");

                                static IContainer CellHeader(IContainer c) =>
                                    c.PaddingVertical(6).PaddingHorizontal(4)
                                     .Background(Colors.Grey.Lighten3)
                                     .DefaultTextStyle(x => x.SemiBold());
                            });

                            if (_model.Rows == null || _model.Rows.Count == 0)
                            {
                                table.Cell().ColumnSpan(5)
                                    .Padding(10)
                                    .Text("No payments found in this range.")
                                    .FontColor(Colors.Grey.Darken1);
                            }
                            else
                            {
                                foreach (var r in _model.Rows)
                                {
                                    table.Cell().Element(CellBody).Text(r.Date.ToLocalTime().ToString("dd MMM yyyy"));
                                    table.Cell().Element(CellBody).Text(r.PatientName ?? "—");
                                    table.Cell().Element(CellBody).Text(r.PatientPhone ?? "—");
                                    table.Cell().Element(CellBody).Text(r.PaymentMethod ?? "—");
                                    table.Cell().Element(CellBody).AlignRight().Text($"৳{r.Amount:0.00}");
                                }
                            }

                            static IContainer CellBody(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                 .PaddingVertical(6).PaddingHorizontal(4);
                        });

                        col.Item().PaddingTop(12).AlignRight().Text(text =>
                        {
                            text.Span("Total Revenue: ").SemiBold();
                            text.Span($"৳{_model.TotalRevenue:0.00}").SemiBold();
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on ").FontColor(Colors.Grey.Darken1);
                        text.Span(DateTime.Now.ToString("dd MMM yyyy hh:mm tt")).FontColor(Colors.Grey.Darken1);
                    });
                });
            }
        }


        private static string ModeToRangeType(string mode)
        {
            return mode switch
            {
                "last7" => "Last7",
                "last15" => "Last15",
                "custom" => "Custom",
                _ => "Month"
            };
        }
    }
}
