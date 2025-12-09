using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorScheduleController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // /DoctorSchedule/ManageSlots
        public async Task<IActionResult> ManageSlots()
        {
            // TODO: load schedules for this doctor
            return View();
        }

        // /DoctorSchedule/UnavailableDates
        public async Task<IActionResult> UnavailableDates()
        {
            // TODO: load unavailability list
            return View();
        }

        // /DoctorSchedule/MaxAppointmentsPerDay
        public IActionResult MaxAppointmentsPerDay()
        {
            // TODO: configure daily limits
            return View();
        }
    }
}
