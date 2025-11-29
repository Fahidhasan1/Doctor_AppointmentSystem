using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistDashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Receptionist Dashboard";
            return View();
        }
    }
}
