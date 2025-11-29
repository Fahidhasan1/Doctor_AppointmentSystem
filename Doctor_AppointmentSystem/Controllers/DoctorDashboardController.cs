using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Doctor Dashboard";
            return View();
        }
    }
}
