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
            if (user == null)
            {
                return Challenge();
            }

            var doctorProfile = await _context.DoctorProfiles
                .FirstOrDefaultAsync(d => d.IsActive && d.UserId == user.Id);

            if (doctorProfile == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var doctorId = doctorProfile.Id;

            var vm = new DoctorDashboardViewModel();

            await PopulateTopSummaryAsync(vm, doctorId);
            await PopulateRevenueSectionAsync(vm, doctorId); // ✅ updated: PaidAtUtc-only + count
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
                // ✅ Paid-only policy (same as Doctor Appointments page)
                .Where(a => _context.Payments.Any(p => p.IsActive &&
                                                     p.AppointmentId == a.Id &&
                                                     p.Status == PaymentStatus.Paid));

            vm.TodaysAppointments = await doctorAppointments
                .CountAsync(a => a.AppointmentDateTime >= today &&
                                 a.AppointmentDateTime < tomorrow);

            vm.UpcomingAppointments = await doctorAppointments
                .CountAsync(a => a.AppointmentDateTime >= tomorrow &&
                                 a.Status == AppointmentStatus.Confirmed);

            // (we still compute these but don't show card now)
            var now = DateTime.Today;
            var monthStart = new DateTime(now.Year, now.Month, 1);
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
            // Registered patients: distinct PatientProfileId
            var treatedRegisteredCount = await doctorAppointments
                .Where(a => a.Status == AppointmentStatus.Completed && a.PatientProfileId != null)
                .Select(a => a.PatientProfileId!.Value)
                .Distinct()
                .CountAsync();

            // Unregistered patients: distinct phone (fallback unique key)
            var treatedUnregisteredCount = await doctorAppointments
                .Where(a => a.Status == AppointmentStatus.Completed && a.PatientProfileId == null &&
                            !string.IsNullOrWhiteSpace(a.UnregisteredPatientPhone))
                .Select(a => a.UnregisteredPatientPhone!)
                .Distinct()
                .CountAsync();

            vm.TotalPatientsTreated = treatedRegisteredCount + treatedUnregisteredCount;
        }

        // ---------------- REVENUE CHART + MONTHLY REVENUE CARD ----------------
        // ✅ UPDATED:
        // - Use PaidAtUtc ONLY (no fallback to CreatedAt)
        // - Optionally sets vm.PaidAppointmentsThisMonth if that property exists in the ViewModel
        private async Task PopulateRevenueSectionAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var nowUtc = DateTime.UtcNow;
            var yearStart = new DateTime(nowUtc.Year, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            // Get doctor appointment ids for the year (to scope payments to this doctor)
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
                    .Select(p => new { p.Amount, p.PaidAtUtc, p.AppointmentId })
                    .ToListAsync();

                foreach (var p in payments)
                {
                    var paidAt = p.PaidAtUtc!.Value;
                    if (paidAt.Year == nowUtc.Year)
                    {
                        int monthIndex = paidAt.Month - 1;
                        revenueByMonth[monthIndex] += p.Amount;
                    }
                }

                // If your DoctorDashboardViewModel has this property, set it:
                // public int PaidAppointmentsThisMonth { get; set; }
                var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1);
                var nextMonthStart = monthStart.AddMonths(1);

                // Use reflection-safe check so controller compiles even if you haven't added the property yet
                var prop = vm.GetType().GetProperty("PaidAppointmentsThisMonth");
                if (prop != null && prop.PropertyType == typeof(int) && prop.CanWrite)
                {
                    var countDistinct = payments
                        .Where(x => x.PaidAtUtc!.Value >= monthStart && x.PaidAtUtc!.Value < nextMonthStart)
                        .Select(x => x.AppointmentId)
                        .Distinct()
                        .Count();

                    prop.SetValue(vm, countDistinct);
                }
            }

            vm.RevenueMonthLabels = Enumerable.Range(1, 12)
                .Select(m => new DateTime(nowUtc.Year, m, 1).ToString("MMM"))
                .ToList();

            vm.RevenueMonthValues = revenueByMonth.ToList();
            vm.MonthlyRevenue = revenueByMonth[nowUtc.Month - 1];
        }

        // ---------------- TODAY'S APPOINTMENTS + STATUS DONUT ----------------
        private async Task PopulateTodaySectionAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var nowLocal = DateTime.Now;

            var todaysAppointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.IsActive &&
                            a.DoctorProfileId == doctorId &&
                            a.AppointmentDateTime >= today &&
                            a.AppointmentDateTime < tomorrow)
                // ✅ Paid-only policy
                .Where(a => _context.Payments.Any(p => p.IsActive &&
                                                     p.AppointmentId == a.Id &&
                                                     p.Status == PaymentStatus.Paid));

            vm.TodayAcceptedCount = await todaysAppointmentsQuery
                .CountAsync(a => a.Status == AppointmentStatus.Confirmed);

            vm.TodayCompletedCount = await todaysAppointmentsQuery
                .CountAsync(a => a.Status == AppointmentStatus.Completed);

            // Treat NoShow as Cancelled bucket for the donut (matches "Cancelled" label)
            vm.TodayCancelledCount = await todaysAppointmentsQuery
                .CountAsync(a => a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow);

            vm.TodayRemainingCount = await todaysAppointmentsQuery
                .CountAsync(a => a.Status == AppointmentStatus.Confirmed &&
                                 a.AppointmentDateTime > nowLocal);

            vm.TodaysAppointmentsList = await todaysAppointmentsQuery
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new DoctorDashboardAppointmentRow
                {
                    AppointmentId = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    PatientName = a.PatientProfileId != null
                               ? ((a.Patient != null && a.Patient.User != null)
                                   ? ((a.Patient.User.FirstName + " " + a.Patient.User.LastName).Trim())
                                   : "-")
                               : (!string.IsNullOrWhiteSpace(a.UnregisteredPatientName) ? a.UnregisteredPatientName! : "-"),
                    VisitType = a.VisitType,
                    Status = a.Status
                })
                .Take(6)
                .ToListAsync();
        }

        // ---------------- TODAY'S SLOTS + UPCOMING OFF DAYS ----------------
        private async Task PopulateAvailabilitySectionAsync(DoctorDashboardViewModel vm, int doctorId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var todayDayOfWeek = today.DayOfWeek;

            var todaysSchedules = await _context.DoctorSchedules
                .Where(s => s.IsActive &&
                            s.DoctorProfileId == doctorId &&
                            s.DayOfWeek == todayDayOfWeek)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var todaysAppointmentsQuery = _context.Appointments
                .Where(a => a.IsActive &&
                            a.DoctorProfileId == doctorId &&
                            a.AppointmentDateTime >= today &&
                            a.AppointmentDateTime < tomorrow)
                // ✅ Paid-only policy
                .Where(a => _context.Payments.Any(p => p.IsActive &&
                                                     p.AppointmentId == a.Id &&
                                                     p.Status == PaymentStatus.Paid));

            var todaySlotSummaries = new List<DoctorDashboardSlotSummary>();

            foreach (var s in todaysSchedules)
            {
                var totalMinutes = (int)(s.EndTime - s.StartTime).TotalMinutes;
                int totalSlots = s.SlotDurationMinutes > 0
                    ? totalMinutes / s.SlotDurationMinutes
                    : 0;

                var bookedInSlot = await todaysAppointmentsQuery
                    .Where(a =>
                        a.AppointmentDateTime.TimeOfDay >= s.StartTime &&
                        a.AppointmentDateTime.TimeOfDay < s.EndTime &&
                        a.Status != AppointmentStatus.Cancelled)
                    .CountAsync();

                string partOfDay;
                if (s.StartTime < TimeSpan.FromHours(12))
                    partOfDay = "Morning";
                else if (s.StartTime < TimeSpan.FromHours(17))
                    partOfDay = "Afternoon";
                else
                    partOfDay = "Evening";

                string label = $"{partOfDay} {s.StartTime:hh\\:mm} – {s.EndTime:hh\\:mm}";

                todaySlotSummaries.Add(new DoctorDashboardSlotSummary
                {
                    Label = label,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TotalSlots = totalSlots,
                    SlotsBooked = bookedInSlot
                });
            }

            vm.TodaySlots = todaySlotSummaries;

            vm.UpcomingOffDays = await _context.DoctorUnavailabilities
                .Where(u => u.IsActive &&
                            u.DoctorProfileId == doctorId &&
                            u.EndDateTime >= today)
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
                .Where(r => r.IsActive &&
                            r.IsVisible &&
                            r.DoctorProfileId == doctorId);

            vm.TotalReviews = await reviewsQuery.CountAsync();
            if (vm.TotalReviews > 0)
            {
                vm.AverageRating = await reviewsQuery.AverageAsync(r => (double)r.Rating);
            }
        }
    }
}
