using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminAppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // ViewModels (kept here so this file is fully self-contained)
        // =========================
        public class AdminAppointmentListItemVM
        {
            public int Id { get; set; }

            public DateTime AppointmentDateTime { get; set; }
            public int DurationMinutes { get; set; }

            public string DoctorName { get; set; } = "—";
            public string DoctorPhone { get; set; } = "—";

            public string PatientName { get; set; } = "—";
            public string PatientPhone { get; set; } = "—";

            public AppointmentStatus Status { get; set; }

            public string PaymentLabel { get; set; } = "Unpaid";
            public decimal? PaymentAmount { get; set; }
            public string Currency { get; set; } = "BDT";
        }

        public class AdminAppointmentsIndexVM
        {
            public string Filter { get; set; } = "All";
            public List<AdminAppointmentListItemVM> Appointments { get; set; } = new();
        }

        // ==========================================
        // Today's Appointments page (same Index view)
        // URL: /AdminAppointment/TodaysAppointment
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> TodaysAppointment()
        {
            ViewBag.IsTodaysPage = true;                 // special UI mode for this page
            ViewBag.HideTabs = true;                     // hide the tab section entirely
            ViewBag.SingleTab = false;                   // ✅ make sure "Today" button never appears
            ViewBag.PageTopTitle = "Sunshine Hospital";  // top title
            return await Index("Today");
        }

        // =========================
        // GET: /AdminAppointment/Index?filter=All
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index(string? filter = "All")
        {
            var normalized = (filter ?? "All").Trim().ToLowerInvariant();

            var todayStart = DateTime.Today;
            var tomorrowStart = todayStart.AddDays(1);

            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => a.IsActive)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .AsQueryable();

            switch (normalized)
            {
                case "today":
                    query = query.Where(a =>
                        a.AppointmentDateTime >= todayStart &&
                        a.AppointmentDateTime < tomorrowStart);
                    filter = "Today";
                    break;

                case "upcoming":
                    query = query.Where(a =>
                        a.AppointmentDateTime >= tomorrowStart &&
                        a.Status != AppointmentStatus.Completed &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow);
                    filter = "Upcoming";
                    break;

                case "completed":
                    query = query.Where(a => a.Status == AppointmentStatus.Completed);
                    filter = "Completed";
                    break;

                case "cancelled":
                case "canceled":
                case "no show":
                case "noshow":
                    query = query.Where(a =>
                        a.Status == AppointmentStatus.Cancelled ||
                        a.Status == AppointmentStatus.NoShow);
                    filter = "Cancelled";
                    break;

                default:
                    filter = "All";
                    break;
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            var rows = appointments.Select(a =>
            {
                var doctorName = a.Doctor?.User != null
                    ? $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}".Trim()
                    : "—";

                var doctorPhone = a.Doctor?.User?.PhoneNumber;
                if (string.IsNullOrWhiteSpace(doctorPhone)) doctorPhone = "—";

                string patientName;
                string patientPhone;

                if (a.PatientProfileId.HasValue && a.Patient?.User != null)
                {
                    patientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(patientName)) patientName = "—";

                    patientPhone = a.Patient.User.PhoneNumber;
                    if (string.IsNullOrWhiteSpace(patientPhone)) patientPhone = "—";
                }
                else
                {
                    patientName = string.IsNullOrWhiteSpace(a.UnregisteredPatientName) ? "—" : a.UnregisteredPatientName.Trim();
                    patientPhone = string.IsNullOrWhiteSpace(a.UnregisteredPatientPhone) ? "—" : a.UnregisteredPatientPhone.Trim();
                }

                return new AdminAppointmentListItemVM
                {
                    Id = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DurationMinutes = a.DurationMinutes,

                    DoctorName = doctorName,
                    DoctorPhone = doctorPhone,

                    PatientName = patientName,
                    PatientPhone = patientPhone,

                    Status = a.Status,

                    PaymentLabel = "Unpaid",
                    PaymentAmount = null,
                    Currency = "BDT"
                };
            }).ToList();

            var apptIds = rows.Select(r => r.Id).ToList();

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive && apptIds.Contains(p.AppointmentId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var latestPayByAppt = payments
                .GroupBy(p => p.AppointmentId)
                .Select(g => g.First())
                .ToDictionary(p => p.AppointmentId, p => p);

            foreach (var r in rows)
            {
                if (latestPayByAppt.TryGetValue(r.Id, out var pay))
                {
                    r.Currency = string.IsNullOrWhiteSpace(pay.Currency) ? "BDT" : pay.Currency;
                    r.PaymentAmount = pay.Amount;

                    if (pay.Status == PaymentStatus.Paid)
                        r.PaymentLabel = $"Paid ({pay.Method})";
                    else
                        r.PaymentLabel = pay.Status.ToString();
                }
            }

            var model = new AdminAppointmentsIndexVM
            {
                Filter = filter ?? "All",
                Appointments = rows
            };

            ViewBag.ActiveFilter = model.Filter;

            // ✅ CRITICAL FIX: Explicit view name so other actions don't search their own view
            return View("Index", model);
        }
    }
}
