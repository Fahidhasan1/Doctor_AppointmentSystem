using System;
using System.Linq;
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
    public class DoctorAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorAppointmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> GetDoctorIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var profile = await _context.DoctorProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.IsActive);

            return profile?.Id;
        }

        // /DoctorAppointments/Today
        public async Task<IActionResult> Today()
        {
            var doctorId = await GetDoctorIdAsync();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            // TODO: build a proper view model and view
            // For now just return an empty page
            return View();
        }

        // /DoctorAppointments/Upcoming
        public async Task<IActionResult> Upcoming()
        {
            var doctorId = await GetDoctorIdAsync();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            return View();
        }

        // /DoctorAppointments/History
        public async Task<IActionResult> History()
        {
            var doctorId = await GetDoctorIdAsync();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            return View();
        }

        // Later: actions to update appointment status (Complete / Cancel / NoShow)
    }
}
