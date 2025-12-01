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
            {
                name = User.Identity?.Name ?? "Admin";
            }

            ViewBag.CurrentUserName = name;
            ViewBag.ProfileImagePath = currentUser?.ProfileImagePath;

            // Counts by role
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            var receptionists = await _userManager.GetUsersInRoleAsync("Receptionist");
            var patients = await _userManager.GetUsersInRoleAsync("Patient");

            var totalSpecialties = await _context.Specialties.CountAsync();
            var totalAppointments = await _context.Appointments.CountAsync(a => a.IsActive);

            // Today's appointments
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var todaysAppointments = await _context.Appointments
                .CountAsync(a =>
                    a.IsActive &&
                    a.AppointmentDateTime >= today &&
                    a.AppointmentDateTime < tomorrow);

            // Monthly revenue (Paid payments only)
            decimal monthlyRevenue = 0;
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            monthlyRevenue = await _context.Payments
                .Where(p => p.IsActive
                            && p.Status == PaymentStatus.Paid
                            && p.PaidAtUtc >= monthStart
                            && p.PaidAtUtc < nextMonthStart)
                .SumAsync(p => p.Amount);

            var vm = new AdminDashboardViewModel
            {
                TotalAdmins = admins.Count,
                TotalDoctors = doctors.Count,
                TotalReceptionists = receptionists.Count,
                TotalPatients = patients.Count,
                TotalSpecialties = totalSpecialties,
                TotalAppointments = totalAppointments,
                TodaysAppointments = todaysAppointments,
                MonthlyRevenue = monthlyRevenue
            };

            return View(vm);
        }


        // These are placeholders for when you click the cards.
        // Later we’ll replace them with actual list/manage pages.
        public IActionResult Admins() => View();
        public IActionResult Doctors() => View();
        public IActionResult Receptionists() => View();
        public IActionResult Patients() => View();
        public IActionResult Specialties() => View();
        public IActionResult Appointments() => View();
        public IActionResult TodayAppointments() => View();
        public IActionResult Payments() => View();
    }
}
