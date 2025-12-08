using System;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReceptionistDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Receptionist Dashboard";

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var totalAppointments = await _context.Appointments
                .Where(a => a.IsActive)
                .CountAsync();

            var todaysAppointments = await _context.Appointments
                .Where(a => a.IsActive && a.AppointmentDateTime.Date == today)
                .CountAsync();

            var todaysCollections = await _context.Payments
                .Where(p =>
                    p.IsActive &&
                    p.Status == PaymentStatus.Paid &&     // ✅ here
                    p.PaidAtUtc.HasValue &&
                    p.PaidAtUtc.Value.Date == today)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var monthlyCollections = await _context.Payments
                .Where(p =>
                    p.IsActive &&
                    p.Status == PaymentStatus.Paid &&     // ✅ and here
                    p.PaidAtUtc.HasValue &&
                    p.PaidAtUtc.Value >= monthStart &&
                    p.PaidAtUtc.Value < monthEnd)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var vm = new ReceptionistDashboardViewModel
            {
                TotalAppointments = totalAppointments,
                TodaysAppointments = todaysAppointments,
                TodaysCollections = todaysCollections,
                MonthlyCollections = monthlyCollections
            };

            return View(vm);
        }
    }
}
