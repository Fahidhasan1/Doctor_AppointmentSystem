using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminDashboardController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel();

            // ===== current admin info =====
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var fullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                vm.AdminFullName = string.IsNullOrWhiteSpace(fullName) ? currentUser.UserName ?? "Admin" : fullName;

                var initialChar = vm.AdminFullName.Trim().FirstOrDefault();
                vm.AdminInitial = (initialChar == '\0') ? "A" : initialChar.ToString().ToUpperInvariant();
            }

            // ===== counts by role (real counts from Identity) =====
            async Task<int> CountUsersInRoleAsync(string roleName)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    return 0;
                }
                var users = await _userManager.GetUsersInRoleAsync(roleName);
                return users.Count;
            }

            vm.TotalAdmins = await CountUsersInRoleAsync("Admin");
            vm.TotalDoctors = await CountUsersInRoleAsync("Doctor");
            vm.TotalReceptionists = await CountUsersInRoleAsync("Receptionist");
            vm.TotalPatients = await CountUsersInRoleAsync("Patient");

            // ===== placeholders until we add those tables =====
            vm.TotalSpecialties = 3;      // TODO: replace with real specialties count later
            vm.TotalAppointments = 0;     // TODO: real total appointments
            vm.TodaysAppointments = 0;    // TODO: today's appointments
            vm.MonthlyRevenue = 0m;       // TODO: real revenue

            // ===== chart data (same as your static demo for now) =====
            vm.MonthlyRevenueSeries = new List<decimal>
            {
                150000, 170000, 165000, 180000, 195000, 210000,
                205000, 200000, 215000, 225000, 235000, 240000
            };

            vm.MonthlyAppointmentSeries = new List<int> { 40, 65, 55, 75, 70, 95 };

            // ===== today's appointments sample rows (we'll replace with DB later) =====
            vm.TodaysAppointmentsList = new List<DashboardAppointmentRow>
            {
                new DashboardAppointmentRow
                {
                    Time = "09:30 AM",
                    PatientName = "Md. Rahim",
                    DoctorName = "Dr. Ahmed",
                    Specialty = "Cardiology",
                    StatusText = "Accepted",
                    StatusCssClass = "pill-status--accepted"
                },
                new DashboardAppointmentRow
                {
                    Time = "10:15 AM",
                    PatientName = "Sadia K.",
                    DoctorName = "Dr. Nabila",
                    Specialty = "Dermatology",
                    StatusText = "Pending",
                    StatusCssClass = "pill-status--pending"
                },
                new DashboardAppointmentRow
                {
                    Time = "11:00 AM",
                    PatientName = "Jamal U.",
                    DoctorName = "Dr. Hasan",
                    Specialty = "Orthopedics",
                    StatusText = "Accepted",
                    StatusCssClass = "pill-status--accepted"
                },
                new DashboardAppointmentRow
                {
                    Time = "12:30 PM",
                    PatientName = "Rafiq A.",
                    DoctorName = "Dr. Tania",
                    Specialty = "Pediatrics",
                    StatusText = "Cancelled",
                    StatusCssClass = "pill-status--canceled"
                },
                new DashboardAppointmentRow
                {
                    Time = "02:00 PM",
                    PatientName = "Nasrin S.",
                    DoctorName = "Dr. Imran",
                    Specialty = "Neurology",
                    StatusText = "Accepted",
                    StatusCssClass = "pill-status--accepted"
                }
            };

            // ===== recent activity items (sample) =====
            vm.RecentActivities = new List<DashboardActivityItem>
            {
                new DashboardActivityItem
                {
                    Text = "New doctor Dr. Farhana (Cardiology) registered.",
                    TimeAgo = "5 mins ago",
                    DotCssClass = ""              // default blue
                },
                new DashboardActivityItem
                {
                    Text = "12 appointments auto-accepted for tomorrow.",
                    TimeAgo = "25 mins ago",
                    DotCssClass = ""              // default blue
                },
                new DashboardActivityItem
                {
                    Text = "3 appointments cancelled by patients (email & SMS sent).",
                    TimeAgo = "1 hour ago",
                    DotCssClass = "activity-dot--red"
                },
                new DashboardActivityItem
                {
                    Text = "Bkash settlement received for yesterday's payments.",
                    TimeAgo = "2 hours ago",
                    DotCssClass = "activity-dot--green"
                }
            };

            return View(vm);
        }
    }
}
