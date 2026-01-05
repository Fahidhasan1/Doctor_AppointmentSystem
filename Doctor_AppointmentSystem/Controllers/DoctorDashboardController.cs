using System;
using System.Collections.Generic;
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
    public class DoctorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================== DASHBOARD OVERVIEW ONLY ==================
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Doctor Dashboard";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var doctorProfile = await _context.DoctorProfiles
                .FirstOrDefaultAsync(d => d.IsActive && d.UserId == user.Id);

            if (doctorProfile == null)
                return RedirectToAction("Index", "Home");

            var doctorId = doctorProfile.Id;

            var vm = new DoctorDashboardViewModel();

            await PopulateTopSummaryAsync(vm, doctorId);
            await PopulateRevenueSectionAsync(vm, doctorId);
            await PopulateTodaySectionAsync(vm, doctorId);
            await PopulateAvailabilitySectionAsync(vm, doctorId);
            await PopulateReviewsSectionAsync(vm, doctorId);

            return View(vm);
        }

        // ---------------- TOP CARDS ----------------
        private async Task PopulateTopSummaryAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var doctorAppointments = _context.Appointments
                .Where(a => a.IsActive && a.DoctorProfileId == doctorId)
                // ✅ paid-only policy
                .Where(a => _context.Payments.Any(p =>
                    p.IsActive &&
                    p.AppointmentId == a.Id &&
                    p.Status == PaymentStatus.Paid));

            vm.TodaysAppointments = await doctorAppointments
                .CountAsync(a => a.AppointmentDateTime >= today && a.AppointmentDateTime < tomorrow);

            vm.UpcomingAppointments = await doctorAppointments
                .CountAsync(a => a.AppointmentDateTime >= tomorrow && a.Status == AppointmentStatus.Confirmed);

            // For completeness (you may still use later)
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            vm.CompletedThisMonth = await doctorAppointments
                .CountAsync(a => a.Status == AppointmentStatus.Completed &&
                                 a.AppointmentDateTime >= monthStart &&
                                 a.AppointmentDateTime < nextMonthStart);

            vm.CancelledThisMonth = await doctorAppointments
                .CountAsync(a => a.Status == AppointmentStatus.Cancelled &&
                                 a.AppointmentDateTime >= monthStart &&
                                 a.AppointmentDateTime < nextMonthStart);

            vm.NoShowThisMonth = await doctorAppointments
                .CountAsync(a => a.Status == AppointmentStatus.NoShow &&
                                 a.AppointmentDateTime >= monthStart &&
                                 a.AppointmentDateTime < nextMonthStart);

            // ✅ Unique patients treated (all time) from COMPLETED appointments
            var treatedRegisteredCount = await doctorAppointments
                .Where(a => a.Status == AppointmentStatus.Completed && a.PatientProfileId != null)
                .Select(a => a.PatientProfileId!.Value)
                .Distinct()
                .CountAsync();

            var treatedUnregisteredCount = await doctorAppointments
                .Where(a => a.Status == AppointmentStatus.Completed &&
                            a.PatientProfileId == null &&
                            !string.IsNullOrWhiteSpace(a.UnregisteredPatientPhone))
                .Select(a => a.UnregisteredPatientPhone!)
                .Distinct()
                .CountAsync();

            vm.TotalPatientsTreated = treatedRegisteredCount + treatedUnregisteredCount;
        }

        // ---------------- REVENUE CHART + MONTHLY REVENUE CARD ----------------
        private async Task PopulateRevenueSectionAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var nowUtc = DateTime.UtcNow;
            var yearStart = new DateTime(nowUtc.Year, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            var apptIdsForDoctorYear = await _context.Appointments
                .Where(a => a.IsActive &&
                            a.DoctorProfileId == doctorId &&
                            a.AppointmentDateTime >= yearStart &&
                            a.AppointmentDateTime < yearEnd)
                .Select(a => a.Id)
                .ToListAsync();

            var revenueByMonth = new decimal[12];

            if (apptIdsForDoctorYear.Count > 0)
            {
                var payments = await _context.Payments
                    .Where(p => p.IsActive
                                && p.Status == PaymentStatus.Paid
                                && p.PaidAtUtc != null
                                && apptIdsForDoctorYear.Contains(p.AppointmentId))
                    .Select(p => new { p.Amount, p.PaidAtUtc })
                    .ToListAsync();

                foreach (var p in payments)
                {
                    var paidAt = p.PaidAtUtc!.Value;
                    if (paidAt.Year == nowUtc.Year)
                    {
                        revenueByMonth[paidAt.Month - 1] += p.Amount;
                    }
                }
            }

            vm.RevenueMonthLabels = Enumerable.Range(1, 12)
                .Select(m => new DateTime(nowUtc.Year, m, 1).ToString("MMM"))
                .ToList();

            vm.RevenueMonthValues = revenueByMonth.ToList();
            vm.MonthlyRevenue = revenueByMonth[nowUtc.Month - 1];
        }

        // ---------------- TODAY'S APPOINTMENTS + STATUS DONUT (UPDATED) ----------------
        private async Task PopulateTodaySectionAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var nowLocal = DateTime.Now;

            // Paid-only today appointments
            var todaysAppointmentsQuery = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.IsActive &&
                            a.DoctorProfileId == doctorId &&
                            a.AppointmentDateTime >= today &&
                            a.AppointmentDateTime < tomorrow)
                .Where(a => _context.Payments.Any(p =>
                    p.IsActive &&
                    p.AppointmentId == a.Id &&
                    p.Status == PaymentStatus.Paid));

            // ✅ Donut values required:
            vm.TodayCompletedCount = await todaysAppointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.Completed);
            vm.TodayNoShowCount = await todaysAppointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.NoShow);

            // Remaining Today = confirmed and time is still in the future
            vm.TodayRemainingCount = await todaysAppointmentsQuery.CountAsync(a =>
                a.Status == AppointmentStatus.Confirmed &&
                a.AppointmentDateTime > nowLocal);

            // Keep legacy values if anything still references them
            vm.TodayAcceptedCount = await todaysAppointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.Confirmed);
            vm.TodayCancelledCount = await todaysAppointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.Cancelled);

            // Build row list with Payment info
            var appts = await todaysAppointmentsQuery
                .OrderBy(a => a.AppointmentDateTime)
                .Take(6)
                .Select(a => new
                {
                    a.Id,
                    a.AppointmentDateTime,
                    a.Status,
                    a.VisitType,

                    PatientName =
                        a.PatientProfileId != null
                            ? ((a.Patient != null && a.Patient.User != null)
                                ? ((a.Patient.User.FirstName + " " + a.Patient.User.LastName).Trim())
                                : "—")
                            : (!string.IsNullOrWhiteSpace(a.UnregisteredPatientName) ? a.UnregisteredPatientName! : "—"),

                    PatientPhone =
                        a.PatientProfileId != null
                            ? (!string.IsNullOrWhiteSpace(a.Patient!.User!.PhoneNumber) ? a.Patient.User.PhoneNumber! : "—")
                            : (!string.IsNullOrWhiteSpace(a.UnregisteredPatientPhone) ? a.UnregisteredPatientPhone! : "—")
                })
                .ToListAsync();

            var ids = appts.Select(x => x.Id).ToList();

            var latestPaidPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive &&
                            p.Status == PaymentStatus.Paid &&
                            ids.Contains(p.AppointmentId))
                .GroupBy(p => p.AppointmentId)
                .Select(g => g.OrderByDescending(p => p.PaidAtUtc ?? p.CreatedAt).FirstOrDefault())
                .ToListAsync();

            var payMap = latestPaidPayments
                .Where(p => p != null)
                .ToDictionary(p => p!.AppointmentId, p => p!);

            vm.TodaysAppointmentsList = appts.Select(a =>
            {
                payMap.TryGetValue(a.Id, out var pay);

                return new DoctorDashboardAppointmentRow
                {
                    AppointmentId = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    VisitType = a.VisitType,
                    PatientName = a.PatientName,
                    PatientPhone = a.PatientPhone,

                    PaymentDisplay = pay != null ? $"Paid ({pay.Method})" : "Paid",
                    PaidAmount = pay?.Amount,
                    Currency = pay?.Currency ?? "BDT"
                };
            }).ToList();
        }

        // ---------------- AVAILABILITY (MORE ACCURATE) + UPCOMING OFF DAYS ----------------
        private async Task PopulateAvailabilitySectionAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var dow = today.DayOfWeek;

            var schedules = await _context.DoctorSchedules
                .AsNoTracking()
                .Where(s => s.IsActive && s.DoctorProfileId == doctorId && s.DayOfWeek == dow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // Paid-only booked appointments for today (exclude cancelled + no show for slot taken)
            var bookedTodayQuery = _context.Appointments
                .AsNoTracking()
                .Where(a => a.IsActive &&
                            a.DoctorProfileId == doctorId &&
                            a.AppointmentDateTime >= today &&
                            a.AppointmentDateTime < tomorrow)
                .Where(a => _context.Payments.Any(p =>
                    p.IsActive &&
                    p.AppointmentId == a.Id &&
                    p.Status == PaymentStatus.Paid))
                .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow);

            var slotSummaries = new List<DoctorDashboardSlotSummary>();

            foreach (var s in schedules)
            {
                var totalMinutes = (int)(s.EndTime - s.StartTime).TotalMinutes;
                int totalSlots = (s.SlotDurationMinutes > 0) ? (totalMinutes / s.SlotDurationMinutes) : 0;

                var bookedInBlock = await bookedTodayQuery.CountAsync(a =>
                    a.AppointmentDateTime.TimeOfDay >= s.StartTime &&
                    a.AppointmentDateTime.TimeOfDay < s.EndTime);

                string partOfDay =
                    s.StartTime < TimeSpan.FromHours(12) ? "Morning" :
                    s.StartTime < TimeSpan.FromHours(17) ? "Afternoon" : "Evening";

                slotSummaries.Add(new DoctorDashboardSlotSummary
                {
                    Label = $"{partOfDay} {s.StartTime:hh\\:mm} – {s.EndTime:hh\\:mm}",
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TotalSlots = totalSlots,
                    SlotsBooked = bookedInBlock,
                    SlotsRemaining = Math.Max(totalSlots - bookedInBlock, 0)
                });
            }

            vm.TodaySlots = slotSummaries;

            vm.UpcomingOffDays = await _context.DoctorUnavailabilities
                .AsNoTracking()
                .Where(u => u.IsActive && u.DoctorProfileId == doctorId && u.EndDateTime >= today)
                .OrderBy(u => u.StartDateTime)
                .Take(5)
                .Select(u => new DoctorDashboardOffDaySummary
                {
                    Date = u.StartDateTime.Date,
                    Reason = u.Reason
                })
                .ToListAsync();
        }

        // ---------------- REVIEWS SUMMARY ----------------
        private async Task PopulateReviewsSectionAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var reviewsQuery = _context.DoctorReviews
                .AsNoTracking()
                .Where(r => r.IsActive && r.IsVisible && r.DoctorProfileId == doctorId);

            vm.TotalReviews = await reviewsQuery.CountAsync();

            if (vm.TotalReviews > 0)
            {
                vm.AverageRating = await reviewsQuery.AverageAsync(r => (double)r.Rating);
            }
        }
    }
}
