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

        public DoctorDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Doctor Dashboard";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Force login again
            }

            // Get doctor profile WITHOUT filtering IsActive
            var doctorProfile = await _context.DoctorProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            // 🚨 If doctor profile missing or inactive → redirect & show message
            if (doctorProfile == null || !doctorProfile.IsActive)
            {
                // Use existing toast system in _Layout.cshtml
                TempData["LoginError"] =
                    "Your doctor account is currently inactive. Please contact the administrator.";

                return RedirectToAction("Index", "Home");
            }



            // From here on, doctor profile is guaranteed to exist & active
            var doctorId = doctorProfile.Id;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var nowUtc = DateTime.UtcNow;
            var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var doctorAppointments = _context.Appointments
                .Where(a => a.IsActive && a.DoctorProfileId == doctorId);

            // === TOP STATS ===
            var todaysAppointments = await doctorAppointments
                .CountAsync(a =>
                    a.AppointmentDateTime >= today &&
                    a.AppointmentDateTime < tomorrow);

            var upcomingAppointments = await doctorAppointments
                .CountAsync(a =>
                    a.AppointmentDateTime >= tomorrow &&
                    a.Status == AppointmentStatus.Confirmed);

            var completedThisMonth = await doctorAppointments
                .CountAsync(a =>
                    a.Status == AppointmentStatus.Completed &&
                    a.AppointmentDateTime >= monthStart &&
                    a.AppointmentDateTime < nextMonthStart);

            var cancelledThisMonth = await doctorAppointments
                .CountAsync(a =>
                    a.Status == AppointmentStatus.Cancelled &&
                    a.AppointmentDateTime >= monthStart &&
                    a.AppointmentDateTime < nextMonthStart);

            var noShowThisMonth = await doctorAppointments
                .CountAsync(a =>
                    a.Status == AppointmentStatus.NoShow &&
                    a.AppointmentDateTime >= monthStart &&
                    a.AppointmentDateTime < nextMonthStart);

            var totalPatientsTreated = await doctorAppointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .Select(a => a.PatientProfileId)
                .Distinct()
                .CountAsync();

            decimal monthlyRevenue = 0;
            var doctorApptIdsThisMonth = await doctorAppointments
                .Where(a => a.AppointmentDateTime >= monthStart &&
                            a.AppointmentDateTime < nextMonthStart)
                .Select(a => a.Id)
                .ToListAsync();

            if (doctorApptIdsThisMonth.Count > 0)
            {
                monthlyRevenue = await _context.Payments
                    .Where(p => p.IsActive &&
                                p.Status == PaymentStatus.Paid &&
                                doctorApptIdsThisMonth.Contains(p.AppointmentId))
                    .SumAsync(p => p.Amount);
            }

            var reviewsQuery = _context.DoctorReviews
                .Where(r => r.IsActive &&
                            r.IsVisible &&
                            r.DoctorProfileId == doctorId);

            var totalReviews = await reviewsQuery.CountAsync();
            double averageRating = totalReviews > 0
                ? await reviewsQuery.AverageAsync(r => (double)r.Rating)
                : 0;

            var vm = new DoctorDashboardViewModel
            {
                TodaysAppointments = todaysAppointments,
                UpcomingAppointments = upcomingAppointments,
                CompletedThisMonth = completedThisMonth,
                CancelledThisMonth = cancelledThisMonth,
                NoShowThisMonth = noShowThisMonth,
                TotalPatientsTreated = totalPatientsTreated,
                MonthlyRevenue = monthlyRevenue,
                AverageRating = averageRating,
                TotalReviews = totalReviews
            };

            return View(vm);
        }

    }
}

