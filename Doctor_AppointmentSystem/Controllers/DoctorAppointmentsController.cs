using System;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorAppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ Single view (Index.cshtml) for ALL filters
        // /DoctorAppointments?filter=All|Today|Upcoming|Completed|Cancelled|ThisMonthPaid
        [HttpGet]
        public async Task<IActionResult> Index(string? filter = "All", bool fromCard = false)
        {
            var doctorId = await GetDoctorIdAsync();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            filter = (filter ?? "All").Trim();
            var normalized = filter.Trim().ToLowerInvariant();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Month range (local date basis)
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            // Base query: active appointments for this doctor
            var baseQuery = _context.Appointments
                .AsNoTracking()
                .Where(a => a.IsActive && a.DoctorProfileId == doctorId.Value)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .OrderBy(a => a.AppointmentDateTime)
                .AsQueryable();

            // ✅ PAID-ONLY policy (hide unpaid everywhere)
            baseQuery = baseQuery.Where(a =>
                _context.Payments.Any(p =>
                    p.IsActive &&
                    p.AppointmentId == a.Id &&
                    p.Status == PaymentStatus.Paid
                )
            );

            // ✅ Special filter: ThisMonthPaid (for Monthly Revenue card click)
            // We show appointments that have a PAID payment created/paid in this month.
            if (normalized == "thismonthpaid")
            {
                var monthPaidApptIds = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.IsActive &&
                                p.Status == PaymentStatus.Paid &&
                                (p.PaidAtUtc ?? p.CreatedAt) >= monthStart &&
                                (p.PaidAtUtc ?? p.CreatedAt) < nextMonthStart)
                    .Select(p => p.AppointmentId)
                    .Distinct()
                    .ToListAsync();

                baseQuery = baseQuery.Where(a => monthPaidApptIds.Contains(a.Id));
            }
            else
            {
                // Apply normal tab filters
                switch (normalized)
                {
                    case "today":
                        baseQuery = baseQuery.Where(a =>
                            a.AppointmentDateTime >= today &&
                            a.AppointmentDateTime < tomorrow);
                        filter = "Today";
                        break;

                    case "upcoming":
                        baseQuery = baseQuery.Where(a =>
                            a.AppointmentDateTime >= tomorrow &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.NoShow);
                        filter = "Upcoming";
                        break;

                    case "completed":
                        baseQuery = baseQuery.Where(a => a.Status == AppointmentStatus.Completed);
                        filter = "Completed";
                        break;

                    case "cancelled":
                    case "canceled":
                        baseQuery = baseQuery.Where(a =>
                            a.Status == AppointmentStatus.Cancelled ||
                            a.Status == AppointmentStatus.NoShow);
                        filter = "Cancelled";
                        break;

                    case "all":
                    default:
                        filter = "All";
                        break;
                }
            }

            // ✅ IMPORTANT FIX:
            // Don’t build complex strings inside EF projection (causes EmptyProjectionMember).
            // Pull raw fields first, then build final display strings in memory.
            var rawRows = await baseQuery
                .Select(a => new
                {
                    a.Id,
                    a.AppointmentDateTime,
                    a.DurationMinutes,
                    a.Status,

                    a.PatientProfileId,

                    RegFirst = a.Patient != null && a.Patient.User != null ? a.Patient.User.FirstName : null,
                    RegLast = a.Patient != null && a.Patient.User != null ? a.Patient.User.LastName : null,
                    RegPhone = a.Patient != null && a.Patient.User != null ? a.Patient.User.PhoneNumber : null,

                    a.UnregisteredPatientName,
                    a.UnregisteredPatientPhone
                })
                .ToListAsync();

            var rows = rawRows.Select(a =>
            {
                string patientName;
                string patientPhone;

                if (a.PatientProfileId != null)
                {
                    var fn = (a.RegFirst ?? "").Trim();
                    var ln = (a.RegLast ?? "").Trim();
                    var full = (fn + " " + ln).Trim();
                    patientName = string.IsNullOrWhiteSpace(full) ? "—" : full;

                    patientPhone = string.IsNullOrWhiteSpace(a.RegPhone) ? "—" : a.RegPhone!;
                }
                else
                {
                    patientName = string.IsNullOrWhiteSpace(a.UnregisteredPatientName) ? "—" : a.UnregisteredPatientName!.Trim();
                    patientPhone = string.IsNullOrWhiteSpace(a.UnregisteredPatientPhone) ? "—" : a.UnregisteredPatientPhone!.Trim();
                }

                return new DoctorAppointmentListItemViewModel
                {
                    Id = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    PatientName = patientName,
                    PatientPhone = patientPhone,

                    // defaults; filled from latest payment map below
                    PaymentDisplay = "Paid",
                    PaidAmount = null,
                    Currency = "BDT"
                };
            }).ToList();

            // ✅ Fill payment display + amount (latest payment per appointment)
            var ids = rows.Select(x => x.Id).ToList();

            var latestPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive && ids.Contains(p.AppointmentId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Build "latest payment per appointment" in memory
            var payMap = latestPayments
                .GroupBy(p => p.AppointmentId)
                .Select(g => g.First())
                .ToDictionary(p => p.AppointmentId, p => p);

            foreach (var r in rows)
            {
                if (payMap.TryGetValue(r.Id, out var pay))
                {
                    r.Currency = string.IsNullOrWhiteSpace(pay.Currency) ? "BDT" : pay.Currency;
                    r.PaidAmount = pay.Amount;

                    if (pay.Status == PaymentStatus.Paid)
                        r.PaymentDisplay = $"Paid ({pay.Method})";
                    else
                        r.PaymentDisplay = pay.Status.ToString();
                }
                else
                {
                    // Should not happen due to paid-only policy, but keep safe
                    r.PaymentDisplay = "Paid";
                    r.PaidAmount = null;
                    r.Currency = "BDT";
                }
            }

            // ✅ Page title for card navigation (when fromCard = true)
            var pageTitle = filter switch
            {
                "Today" => "Today's Appointments",
                "Upcoming" => "Upcoming Appointments",
                "Completed" => "Completed Appointments",
                "Cancelled" => "Cancelled / No Show Appointments",
                _ => "Appointments"
            };

            if (normalized == "thismonthpaid")
            {
                pageTitle = "This Month Paid Appointments";
                filter = "ThisMonthPaid";
            }

            var vm = new DoctorAppointmentsIndexViewModel
            {
                Filter = filter,
                PageTitle = pageTitle,
                Appointments = rows
            };

            ViewBag.FromCard = fromCard;
            ViewBag.ActiveFilter = filter;
            ViewBag.PageTitle = pageTitle;

            // ✅ Always render Index.cshtml
            return View("Index", vm);
        }

        // ✅ These routes exist only so Dashboard cards / old links work
        // They MUST render Index.cshtml, not Today.cshtml / Upcoming.cshtml
        [HttpGet]
        public Task<IActionResult> Today(bool fromCard = false) => Index("Today", fromCard);

        [HttpGet]
        public Task<IActionResult> Upcoming(bool fromCard = false) => Index("Upcoming", fromCard);

        // ✅ Optional: monthly revenue card click endpoint
        [HttpGet]
        public Task<IActionResult> ThisMonthPaid(bool fromCard = false) => Index("ThisMonthPaid", fromCard);

        // ✅ ACTIONS (toast via TempData + redirect back to same filter)
        // Only allow actions for TODAY confirmed appointments (matches your UI rule).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int id, string filter = "All")
        {
            var doctorId = await GetDoctorIdAsync();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            var appt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.DoctorProfileId == doctorId.Value);

            if (appt == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            if (appt.AppointmentDateTime.Date != DateTime.Today || appt.Status != AppointmentStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "Action is only allowed for today's confirmed appointments.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            appt.Status = AppointmentStatus.Completed;
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment marked as completed.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNoShow(int id, string filter = "All")
        {
            var doctorId = await GetDoctorIdAsync();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            var appt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.DoctorProfileId == doctorId.Value);

            if (appt == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            if (appt.AppointmentDateTime.Date != DateTime.Today || appt.Status != AppointmentStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "Action is only allowed for today's confirmed appointments.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            appt.Status = AppointmentStatus.NoShow;
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment marked as No Show.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        private async Task<int?> GetDoctorIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var profile = await _context.DoctorProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

            return profile?.Id;
        }
    }
}
