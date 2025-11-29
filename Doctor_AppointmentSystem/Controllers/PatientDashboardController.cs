using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Patient Dashboard";
            return View();
        }
    }
}
