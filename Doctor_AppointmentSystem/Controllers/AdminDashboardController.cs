//using Doctor_AppointmentSystem.Data;
//using Doctor_AppointmentSystem.Enums;
//using Doctor_AppointmentSystem.Models;
//using Doctor_AppointmentSystem.ViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace Doctor_AppointmentSystem.Controllers
//{
//    [Authorize(Roles = "Admin")]
//    public class AdminDashboardController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;

//        public AdminDashboardController(
//            ApplicationDbContext context,
//            UserManager<ApplicationUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // GET: /AdminDashboard
//        public async Task<IActionResult> Index()
//        {
//            var currentUser = await _userManager.GetUserAsync(User);

//            // For sidebar header
//            var name = (currentUser?.FirstName + " " + currentUser?.LastName)?.Trim();
//            if (string.IsNullOrWhiteSpace(name))
//            {
//                name = User.Identity?.Name ?? "Admin";
//            }

//            ViewBag.CurrentUserName = name;
//            ViewBag.ProfileImagePath = currentUser?.ProfileImagePath;

//            // Counts by role
//            var admins = await _userManager.GetUsersInRoleAsync("Admin");
//            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
//            var receptionists = await _userManager.GetUsersInRoleAsync("Receptionist");
//            var patients = await _userManager.GetUsersInRoleAsync("Patient");

//            var totalSpecialties = await _context.Specialties.CountAsync();
//            var totalAppointments = await _context.Appointments.CountAsync(a => a.IsActive);

//            // Today's appointments
//            var today = DateTime.Today;
//            var tomorrow = today.AddDays(1);
//            var todaysAppointments = await _context.Appointments
//                .CountAsync(a =>
//                    a.IsActive &&
//                    a.AppointmentDateTime >= today &&
//                    a.AppointmentDateTime < tomorrow);

//            // Monthly revenue (Paid payments only)
//            decimal monthlyRevenue = 0;
//            var now = DateTime.UtcNow;
//            var monthStart = new DateTime(now.Year, now.Month, 1);
//            var nextMonthStart = monthStart.AddMonths(1);

//            monthlyRevenue = await _context.Payments
//                .Where(p => p.IsActive
//                            && p.Status == PaymentStatus.Paid
//                            && p.PaidAtUtc >= monthStart
//                            && p.PaidAtUtc < nextMonthStart)
//                .SumAsync(p => p.Amount);

//            var vm = new AdminDashboardViewModel
//            {
//                TotalAdmins = admins.Count,
//                TotalDoctors = doctors.Count,
//                TotalReceptionists = receptionists.Count,
//                TotalPatients = patients.Count,
//                TotalSpecialties = totalSpecialties,
//                TotalAppointments = totalAppointments,
//                TodaysAppointments = todaysAppointments,
//                MonthlyRevenue = monthlyRevenue
//            };

//            return View(vm);
//        }




//        // These are placeholders for when you click the cards.
//        // Later we’ll replace them with actual list/manage pages.
//        public IActionResult Admins() => View();
//        public IActionResult Doctors() => View();
//        public IActionResult Receptionists() => View();
//        public IActionResult Patients() => View();
//        public IActionResult Specialties() => View();
//        //public IActionResult Appointments() => View();

//        public IActionResult Appointments()
//        {
//            return RedirectToAction("Index", "AdminAppointment", new { filter = "All" });
//        }

//        public IActionResult TodayAppointments() => View();
//        public IActionResult Payments() => View();
//    }
//}


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

            // Today's appointments (server local date)
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todaysAppointments = await _context.Appointments
                .CountAsync(a =>
                    a.IsActive &&
                    a.AppointmentDateTime >= today &&
                    a.AppointmentDateTime < tomorrow);

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

            var revenueMonthLabels = new List<string>
            {
                "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"
            };

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
            // This fixes "December missing" when current month is January
            // =========================================================
            var localNow = DateTime.Now; // AppointmentDateTime is typically local
            var startMonth = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(-5);
            var endMonth = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(1);

            var apptMonthLabels = Enumerable.Range(0, 6)
                .Select(i => startMonth.AddMonths(i))
                .Select(d => d.ToString("MMM"))
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

            // =========================================================
            // ✅ ViewModel
            // Note: We keep MonthLabels used by JS.
            // If you want BOTH charts use different labels, we can add extra props.
            // For now:
            // - MonthLabels will be appointment labels (last 6 months)
            // - RevenueByMonth is still 12 months
            // =========================================================

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

                // ✅ chart data
                // Appointment chart (6 months)
                MonthLabels = apptMonthLabels,
                AppointmentsByMonth = apptsByMonth,

                // Revenue chart (12 months) - still Jan..Dec
                RevenueByMonth = revenueByMonthArr.ToList()
            };

            return View(vm);
        }

        // These are placeholders for when you click the cards.
        public IActionResult Admins() => View();
        public IActionResult Doctors() => View();
        public IActionResult Receptionists() => View();
        public IActionResult Patients() => View();
        public IActionResult Specialties() => View();

        public IActionResult Appointments()
        {
            return RedirectToAction("Index", "AdminAppointment", new { filter = "All" });
        }

        // ✅ Fix today's appointment card click
        public IActionResult TodayAppointments()
        {
            return RedirectToAction("TodaysAppointment", "AdminAppointment");
        }

        public IActionResult Payments() => View();
    }
}
