using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorPatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorPatientsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // /DoctorPatients
        public async Task<IActionResult> Index()
        {
            // TODO: list distinct patients treated by this doctor
            return View();
        }
    }
}
