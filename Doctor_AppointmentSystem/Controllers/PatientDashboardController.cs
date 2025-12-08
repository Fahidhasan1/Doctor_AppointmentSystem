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
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Patient Dashboard";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var patientProfile = await _context.PatientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patientProfile == null || !patientProfile.IsActive)
            {
                TempData["LoginError"] =
                    "Your patient account is currently inactive. Please contact the administrator.";

                return RedirectToAction("Index", "Home");
            }

            var patientId = patientProfile.Id;

            var today = DateTime.Today;
            var nowUtc = DateTime.UtcNow;
            var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var patientAppointments = _context.Appointments
                .Where(a => a.IsActive && a.PatientProfileId == patientId);

            var totalAppointments = await patientAppointments.CountAsync();

            var upcomingAppointments = await patientAppointments
                .CountAsync(a =>
                    a.AppointmentDateTime >= today &&
                    a.Status == AppointmentStatus.Confirmed);

            var completedAppointments = await patientAppointments
                .CountAsync(a => a.Status == AppointmentStatus.Completed);

            var cancelledAppointments = await patientAppointments
                .CountAsync(a => a.Status == AppointmentStatus.Cancelled);

            var nextAppt = await patientAppointments
                .Where(a =>
                    a.AppointmentDateTime >= today &&
                    a.Status == AppointmentStatus.Confirmed)
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => (DateTime?)a.AppointmentDateTime)
                .FirstOrDefaultAsync();

            var lastAppt = await patientAppointments
                .Where(a =>
                    a.AppointmentDateTime < today &&
                    a.Status == AppointmentStatus.Completed)
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => (DateTime?)a.AppointmentDateTime)
                .FirstOrDefaultAsync();

            var patientApptIds = await patientAppointments
                .Select(a => a.Id)
                .ToListAsync();

            decimal totalPaid = 0;
            int pendingPayments = 0;

            if (patientApptIds.Count > 0)
            {
                var paymentsForPatient = _context.Payments
                    .Where(p => p.IsActive &&
                                patientApptIds.Contains(p.AppointmentId));

                totalPaid = await paymentsForPatient
                    .Where(p => p.Status == PaymentStatus.Paid)
                    .SumAsync(p => p.Amount);

                pendingPayments = await paymentsForPatient
                    .CountAsync(p => p.Status == PaymentStatus.Pending);
            }

            var notificationCount = await _context.Notifications
                .CountAsync(n => n.IsActive && n.UserId == user.Id);

            var vm = new PatientDashboardViewModel
            {
                UpcomingAppointments = upcomingAppointments,
                CompletedAppointments = completedAppointments,
                CancelledAppointments = cancelledAppointments,
                TotalAppointments = totalAppointments,
                PendingPayments = pendingPayments,
                TotalPaid = totalPaid,
                NextAppointmentDate = nextAppt,
                LastAppointmentDate = lastAppt,
                NotificationCount = notificationCount
            };

            return View(vm);
        }
    }
}
