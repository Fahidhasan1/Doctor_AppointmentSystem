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
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReceptionistDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Receptionist Dashboard";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var receptionistProfile = await _context.ReceptionistProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (receptionistProfile == null || !receptionistProfile.IsActive)
            {
                TempData["LoginError"] =
                    "Your receptionist account is currently inactive. Please contact the administrator.";

                return RedirectToAction("Index", "Home");
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todaysAppointmentsQuery = _context.Appointments
                .Where(a =>
                    a.IsActive &&
                    a.AppointmentDateTime >= today &&
                    a.AppointmentDateTime < tomorrow);

            var todaysAppointments = await todaysAppointmentsQuery.CountAsync();

            var completedToday = await todaysAppointmentsQuery
                .CountAsync(a => a.Status == AppointmentStatus.Completed);

            var cancelledToday = await todaysAppointmentsQuery
                .CountAsync(a => a.Status == AppointmentStatus.Cancelled);

            var pendingCheckins = await todaysAppointmentsQuery
                .CountAsync(a =>
                    a.Status == AppointmentStatus.Confirmed);

            // Payments today (cash)
            var cashCollectedToday = await _context.Payments
                .Where(p =>
                    p.IsActive &&
                    p.Status == PaymentStatus.Paid &&
                    p.Method == PaymentMethod.Cash &&
                    p.PaidAtUtc >= today &&
                    p.PaidAtUtc < tomorrow)
                .SumAsync(p => p.Amount);

            var todaysApptIds = await todaysAppointmentsQuery
                .Select(a => a.Id)
                .ToListAsync();

            int pendingPaymentsToday = 0;
            if (todaysApptIds.Count > 0)
            {
                pendingPaymentsToday = await _context.Payments
                    .CountAsync(p =>
                        p.IsActive &&
                        p.Status == PaymentStatus.Pending &&
                        todaysApptIds.Contains(p.AppointmentId));
            }

            var totalPatients = await _context.PatientProfiles
                .CountAsync(p => p.IsActive);

            var totalDoctors = await _context.DoctorProfiles
                .CountAsync(d => d.IsActive);

            var vm = new ReceptionistDashboardViewModel
            {
                TodaysAppointments = todaysAppointments,
                PendingCheckins = pendingCheckins,
                CompletedToday = completedToday,
                CancelledToday = cancelledToday,
                PendingPaymentsToday = pendingPaymentsToday,
                CashCollectedToday = cashCollectedToday,
                TotalPatients = totalPatients,
                TotalDoctors = totalDoctors
            };

            return View(vm);
        }
    }
}
