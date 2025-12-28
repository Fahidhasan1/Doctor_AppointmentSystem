using System;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorPatientsTreatedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorPatientsTreatedController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ Unique patients treated (COMPLETED + PAID)
        // GET: /DoctorPatientsTreated?fromCard=true
        [HttpGet]
        public async Task<IActionResult> Index(bool fromCard = false)
        {
            var doctorId = await GetDoctorIdAsync();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            // Base: doctor's completed appointments
            // + paid-only policy
            var completedPaidQuery = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.IsActive &&
                            a.DoctorProfileId == doctorId.Value &&
                            a.Status == AppointmentStatus.Completed)
                .Where(a => _context.Payments.Any(p =>
                    p.IsActive &&
                    p.AppointmentId == a.Id &&
                    p.Status == PaymentStatus.Paid));

            // ---- Registered: unique by PatientProfileId ----
            var registered = await completedPaidQuery
                .Where(a => a.PatientProfileId != null)
                .GroupBy(a => a.PatientProfileId!.Value)
                .Select(g => new
                {
                    PatientProfileId = g.Key,
                    Visits = g.Count(),
                    LastVisit = g.Max(x => x.AppointmentDateTime),

                    // pull raw fields only (safe for EF)
                    FirstName = g.Select(x => x.Patient!.User!.FirstName).FirstOrDefault(),
                    LastName = g.Select(x => x.Patient!.User!.LastName).FirstOrDefault(),
                    Phone = g.Select(x => x.Patient!.User!.PhoneNumber).FirstOrDefault()
                })
                .ToListAsync();

            // ---- Unregistered: unique by phone (fallback key) ----
            var unregistered = await completedPaidQuery
                .Where(a => a.PatientProfileId == null &&
                            !string.IsNullOrWhiteSpace(a.UnregisteredPatientPhone))
                .GroupBy(a => a.UnregisteredPatientPhone!)
                .Select(g => new
                {
                    Phone = g.Key,
                    Visits = g.Count(),
                    LastVisit = g.Max(x => x.AppointmentDateTime),
                    Name = g.Select(x => x.UnregisteredPatientName).FirstOrDefault()
                })
                .ToListAsync();

            // Build final VM in memory (safe formatting)
            var vm = new DoctorPatientsTreatedIndexViewModel
            {
                FromCard = fromCard,
                PageTitle = "Patients Treated",
                TotalPatientsTreated = registered.Count + unregistered.Count
            };

            foreach (var r in registered)
            {
                var fullName = ((r.FirstName ?? "").Trim() + " " + (r.LastName ?? "").Trim()).Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = "—";

                vm.Patients.Add(new DoctorPatientsTreatedRowViewModel
                {
                    IsRegistered = true,
                    PatientProfileId = r.PatientProfileId,
                    PatientName = fullName,
                    PatientPhone = string.IsNullOrWhiteSpace(r.Phone) ? "—" : r.Phone!,
                    VisitsCount = r.Visits,
                    LastTreatedAt = r.LastVisit
                });
            }

            foreach (var u in unregistered)
            {
                vm.Patients.Add(new DoctorPatientsTreatedRowViewModel
                {
                    IsRegistered = false,
                    PatientProfileId = null,
                    PatientName = string.IsNullOrWhiteSpace(u.Name) ? "—" : u.Name!.Trim(),
                    PatientPhone = string.IsNullOrWhiteSpace(u.Phone) ? "—" : u.Phone.Trim(),
                    VisitsCount = u.Visits,
                    LastTreatedAt = u.LastVisit
                });
            }

            // Sort by last treated desc
            vm.Patients = vm.Patients
                .OrderByDescending(x => x.LastTreatedAt)
                .ToList();

            ViewBag.FromCard = fromCard;
            ViewBag.PageTitle = vm.PageTitle;

            return View(vm); // ✅ Views/DoctorPatientsTreated/Index.cshtml
        }

        private async Task<int?> GetDoctorIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var profile = await _context.DoctorProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

            return profile?.Id;
        }
    }
}
