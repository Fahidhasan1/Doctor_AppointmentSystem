using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Change these namespaces if your project uses different ones
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.ViewModels;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientAppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /PatientAppointments?filter=upcoming|completed|cancelled|payments
        public async Task<IActionResult> Index(string? filter, bool fromCard = false)

        {
            // 1) Identify logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 2) Get patient profile
            var patientProfile = await _context.PatientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);

            if (patientProfile == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
                return RedirectToAction("Index", "PatientDashboard");
            }

            // 3) Normalize filter
            var normalized = (filter ?? "").Trim().ToLowerInvariant();
            var now = DateTime.Now;

            // 4) Base query (only this patient's active appointments)
            IQueryable<Appointment> baseQuery = _context.Appointments
                .AsNoTracking()
                .Where(a => a.PatientProfileId == patientProfile.Id && a.IsActive);

            // 5) Apply filter so dashboard cards show ONLY that category
            switch (normalized)
            {
              
                   case "upcoming":
                    baseQuery = baseQuery.Where(a =>
                        a.AppointmentDateTime >= now &&
                        (a.Status == AppointmentStatus.Confirmed ||
                         a.Status == AppointmentStatus.Rescheduled));
                    break;


                case "completed":
                    baseQuery = baseQuery.Where(a => a.Status == AppointmentStatus.Completed);
                    break;

                case "cancelled":
                    baseQuery = baseQuery.Where(a =>
                        a.Status == AppointmentStatus.Cancelled ||
                        a.Status == AppointmentStatus.NoShow);
                    break;

                case "payments":
                    // Paid appointments only
                    baseQuery = baseQuery.Where(a =>
                        _context.Payments.Any(p =>
                            p.AppointmentId == a.Id &&
                            p.IsActive &&
                            p.Status == PaymentStatus.Paid));
                    break;

                default:
                    // All (sidebar)
                    normalized = "";
                    break;
            }

            // 6) Project to ViewModel
            var list = await baseQuery
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new PatientAppointmentListItemViewModel
                {
                    Id = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DurationMinutes = a.DurationMinutes,
                    VisitType = a.VisitType,
                    Status = a.Status,

                    // Doctor name (from DoctorProfiles -> Users)
                    DoctorName = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => ((d.User.FirstName ?? "") + " " + (d.User.LastName ?? "")).Trim())
                        .FirstOrDefault() ?? "",

                    // Specialty (first specialty if multiple)
                    SpecialtyName = _context.DoctorSpecialties
                        .Where(ds => ds.DoctorProfileId == a.DoctorProfileId)
                        .Select(ds => ds.Specialty.Name)
                        .FirstOrDefault(),

                    // Payment info
                    IsPaid = _context.Payments.Any(p =>
                        p.AppointmentId == a.Id &&
                        p.IsActive &&
                        p.Status == PaymentStatus.Paid),

                    AmountPaid = _context.Payments
                        .Where(p => p.AppointmentId == a.Id && p.IsActive && p.Status == PaymentStatus.Paid)
                        .OrderByDescending(p => p.PaidAtUtc)
                        .Select(p => (decimal?)p.Amount)
                        .FirstOrDefault(),

                    PaymentMethod = _context.Payments
                        .Where(p => p.AppointmentId == a.Id && p.IsActive && p.Status == PaymentStatus.Paid)
                        .OrderByDescending(p => p.PaidAtUtc)
                        .Select(p => p.Method.ToString())
                        .FirstOrDefault()
                })
                .ToListAsync();

            // 7) Send to View
            var vm = new PatientAppointmentsIndexViewModel
            {
                Filter = normalized,
                Appointments = list,
                FromCard = fromCard
            };


            return View(vm);
        }
    }
}
