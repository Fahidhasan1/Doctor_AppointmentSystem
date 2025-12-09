using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // /DoctorReviews
        public async Task<IActionResult> Index()
        {
            // TODO: list reviews for this doctor
            return View();
        }
    }
}
