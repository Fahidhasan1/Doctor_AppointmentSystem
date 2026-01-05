using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /AdminDashboard
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // For sidebar header
            var name = (currentUser?.FirstName + " " + currentUser?.LastName)?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = User.Identity?.Name ?? "Admin";

            ViewBag.CurrentUserName = name;
            ViewBag.ProfileImagePath = currentUser?.ProfileImagePath;

            // Counts by role
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            var receptionists = await _userManager.GetUsersInRoleAsync("Receptionist");
            var patients = await _userManager.GetUsersInRoleAsync("Patient");

            var totalSpecialties = await _context.Specialties.CountAsync();
            var totalAppointments = await _context.Appointments.CountAsync(a => a.IsActive);

            // Today's appointments (server local date)
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todaysAppointments = await _context.Appointments
                .CountAsync(a => a.IsActive
                                 && a.AppointmentDateTime >= today
                                 && a.AppointmentDateTime < tomorrow);

            // Monthly revenue (Paid payments only) - current month (UTC)
            var nowUtc = DateTime.UtcNow;
            var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1);
            var nextMonthStartUtc = monthStartUtc.AddMonths(1);

            var monthlyRevenue = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive
                            && p.Status == PaymentStatus.Paid
                            && p.PaidAtUtc.HasValue
                            && p.PaidAtUtc.Value >= monthStartUtc
                            && p.PaidAtUtc.Value < nextMonthStartUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // =========================================================
            // ✅ Revenue Graph Data (CURRENT YEAR: Jan - Dec)
            // =========================================================
            var year = nowUtc.Year;

            var revenueRaw = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive
                            && p.Status == PaymentStatus.Paid
                            && p.PaidAtUtc.HasValue
                            && p.PaidAtUtc.Value.Year == year)
                .GroupBy(p => p.PaidAtUtc.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            var revenueByMonthArr = new decimal[12];
            foreach (var r in revenueRaw)
            {
                if (r.Month >= 1 && r.Month <= 12)
                    revenueByMonthArr[r.Month - 1] = r.Total;
            }

            // =========================================================
            // ✅ Appointment Graph Data (ROLLING LAST 6 MONTHS)
            // =========================================================
            var localNow = DateTime.Now; // appointments are usually local
            var startMonth = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(-5);
            var endMonth = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(1);

            var apptMonthLabels = Enumerable.Range(0, 6)
                .Select(i => startMonth.AddMonths(i).ToString("MMM"))
                .ToList();

            var monthKeys = Enumerable.Range(0, 6)
                .Select(i => startMonth.AddMonths(i))
                .Select(d => new { d.Year, d.Month })
                .ToList();

            var apptRaw = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.IsActive
                            && a.AppointmentDateTime >= startMonth
                            && a.AppointmentDateTime < endMonth)
                .GroupBy(a => new { a.AppointmentDateTime.Year, a.AppointmentDateTime.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            var apptsByMonth = monthKeys.Select(k =>
            {
                var match = apptRaw.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month);
                return match?.Count ?? 0;
            }).ToList();

            var vm = new AdminDashboardViewModel
            {
                TotalAdmins = admins.Count,
                TotalDoctors = doctors.Count,
                TotalReceptionists = receptionists.Count,
                TotalPatients = patients.Count,
                TotalSpecialties = totalSpecialties,
                TotalAppointments = totalAppointments,
                TodaysAppointments = todaysAppointments,
                MonthlyRevenue = monthlyRevenue,

                MonthLabels = apptMonthLabels,
                AppointmentsByMonth = apptsByMonth,

                RevenueByMonth = revenueByMonthArr.ToList()
            };

            return View(vm);
        }

        // =========================================================
        // ✅ Revenue Report Page
        // Modes: month | last7 | last15 | custom
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> RevenueReport(
      string mode = "month",
      int? year = null,
      int? month = null,
      string? start = null,
      string? end = null)
        {
            // ✅ Sidebar/header data (same as Index)
            var currentUser = await _userManager.GetUserAsync(User);

            var name = (currentUser?.FirstName + " " + currentUser?.LastName)?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = User.Identity?.Name ?? "Admin";

            ViewBag.CurrentUserName = name;
            ViewBag.ProfileImagePath = currentUser?.ProfileImagePath;

            ViewBag.PageTopTitle = "Sunshine Hospital";

            // ======================
            // Date range
            // ======================
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
                    fromUtc = new DateTime(y, m, 1);
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
                fromUtc = new DateTime(y, m, 1);
                toUtcExclusive = fromUtc.AddMonths(1);
                mode = "month";
            }

            // ======================
            // Query (Include needed navigation)
            // ======================
            var query = _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive
                            && p.Status == PaymentStatus.Paid
                            && p.PaidAtUtc.HasValue
                            && p.PaidAtUtc.Value >= fromUtc
                            && p.PaidAtUtc.Value < toUtcExclusive)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(pt => pt.User);

            // ✅ Pull into memory first (so we can safely use ?. without EF errors)
            var payments = await query
                .OrderByDescending(p => p.PaidAtUtc)
                .ToListAsync();

            var total = payments.Sum(p => p.Amount);

            // ✅ Now build rows in memory (no EF translation)
            var rows = payments.Select(p =>
            {
                var doctorFirst = p.Appointment?.Doctor?.User?.FirstName ?? "";
                var doctorLast = p.Appointment?.Doctor?.User?.LastName ?? "";
                var doctorName = (doctorFirst + " " + doctorLast).Trim();
                if (string.IsNullOrWhiteSpace(doctorName)) doctorName = "—";

                string patientName;
                var patientFirst = p.Appointment?.Patient?.User?.FirstName ?? "";
                var patientLast = p.Appointment?.Patient?.User?.LastName ?? "";
                var regPatient = (patientFirst + " " + patientLast).Trim();

                if (!string.IsNullOrWhiteSpace(regPatient))
                    patientName = regPatient;
                else if (!string.IsNullOrWhiteSpace(p.Appointment?.UnregisteredPatientName))
                    patientName = p.Appointment!.UnregisteredPatientName!;
                else
                    patientName = "—";

                return new RevenueReportRowViewModel
                {
                    PaidAtUtc = p.PaidAtUtc!.Value,
                    AppointmentId = p.AppointmentId,
                    Method = p.Method.ToString(),
                    Currency = string.IsNullOrWhiteSpace(p.Currency) ? "BDT" : p.Currency,
                    Amount = p.Amount,
                    DoctorName = doctorName,
                    PatientName = patientName
                };
            }).ToList();

            var vm = new RevenueReportViewModel
            {
                Mode = mode,
                Year = y,
                Month = m,
                Start = start,
                End = end,
                FromUtc = fromUtc,
                ToUtc = toUtcExclusive.AddSeconds(-1),
                Rows = rows,
                Total = total
            };

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
            // QuestPDF license (Community)
            QuestPDF.Settings.License = LicenseType.Community;

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

            // Same query as RevenueReport (with includes for names)
            var query = _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive
                            && p.Status == PaymentStatus.Paid
                            && p.PaidAtUtc.HasValue
                            && p.PaidAtUtc.Value >= fromUtc
                            && p.PaidAtUtc.Value < toUtcExclusive)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(pt => pt.User);

            var total = await query.SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var rows = await query
                .OrderByDescending(p => p.PaidAtUtc)
                .Select(p => new RevenueReportRowViewModel
                {
                    PaidAtUtc = p.PaidAtUtc!.Value,
                    AppointmentId = p.AppointmentId,
                    Method = p.Method.ToString(),
                    Currency = string.IsNullOrWhiteSpace(p.Currency) ? "BDT" : p.Currency,
                    Amount = p.Amount,

                    DoctorName =
                        p.Appointment != null && p.Appointment.Doctor != null && p.Appointment.Doctor.User != null
                            ? (p.Appointment.Doctor.User.FirstName + " " + p.Appointment.Doctor.User.LastName).Trim()
                            : "—",

                    PatientName =
                        p.Appointment != null && p.Appointment.Patient != null && p.Appointment.Patient.User != null
                            ? (p.Appointment.Patient.User.FirstName + " " + p.Appointment.Patient.User.LastName).Trim()
                            : (p.Appointment != null && !string.IsNullOrWhiteSpace(p.Appointment.UnregisteredPatientName)
                                ? p.Appointment.UnregisteredPatientName
                                : "—")
                })
                .ToListAsync();

            var vm = new RevenueReportViewModel
            {
                Mode = mode,
                Year = y,
                Month = m,
                Start = start,
                End = end,
                FromUtc = fromUtc,
                ToUtc = toUtcExclusive.AddSeconds(-1),
                Rows = rows,
                Total = total
            };

            // Build PDF bytes
            var pdfBytes = new RevenueReportPdfDocument(vm).GeneratePdf();

            var fileName = $"RevenueReport_{vm.FromUtc:yyyyMMdd}_{vm.ToUtc:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public class RevenueReportPdfDocument : IDocument
        {
            private readonly RevenueReportViewModel _model;

            public RevenueReportPdfDocument(RevenueReportViewModel model)
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

                        col.Item().Text($"Range: {_model.FromUtc.ToLocalTime():dd MMM yyyy} - {_model.ToUtc.ToLocalTime():dd MMM yyyy}")
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
                                columns.RelativeColumn(2);  // Date
                                columns.RelativeColumn(3);  // Doctor
                                columns.RelativeColumn(3);  // Patient
                                columns.RelativeColumn(2);  // Method
                                columns.RelativeColumn(2);  // Amount
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeader).Text("Date");
                                header.Cell().Element(CellHeader).Text("Doctor");
                                header.Cell().Element(CellHeader).Text("Patient");
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
                                    table.Cell().Element(CellBody).Text(r.PaidAtUtc.ToLocalTime().ToString("dd MMM yyyy"));
                                    table.Cell().Element(CellBody).Text(r.DoctorName);
                                    table.Cell().Element(CellBody).Text(r.PatientName);
                                    table.Cell().Element(CellBody).Text(r.Method);
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
                            text.Span($"৳{_model.Total:0.00}").SemiBold();
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


        // =========================================================
        // Existing navigation actions
        // =========================================================
        public IActionResult Admins() => View();
        public IActionResult Doctors() => View();
        public IActionResult Receptionists() => View();
        public IActionResult Patients() => View();
        public IActionResult Specialties() => View();

        public IActionResult Appointments()
        {
            return RedirectToAction("Index", "AdminAppointment", new { filter = "All" });
        }

        public IActionResult TodayAppointments()
        {
            return RedirectToAction("TodaysAppointment", "AdminAppointment");
        }

        public IActionResult Payments() => View();
    }
}
