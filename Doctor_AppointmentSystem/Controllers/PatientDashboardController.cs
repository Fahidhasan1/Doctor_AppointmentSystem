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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int DoctorPageSize = 6;

        public PatientDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /PatientDashboard
        public async Task<IActionResult> Index(
            string doctorNameFilter,
            int? specialtyIdFilter,
            string experienceFilter,
            int page = 1)
        {
            // ----------------------------
            // 1. Ensure logged-in + active patient
            // ----------------------------
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var patientProfile = await _context.PatientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patientProfile == null || !patientProfile.IsActive)
            {
                TempData["LoginError"] =
                    "Your patient account is currently inactive. Please contact the administrator.";

                return RedirectToAction("Index", "Home");
            }

            var patientId = patientProfile.Id;
            var today = DateTime.Today;
            var next7Days = today.AddDays(7);

            // ----------------------------
            // 2. Base appointment query
            // ----------------------------
            var patientAppointments = _context.Appointments
                .Where(a => a.IsActive && a.PatientProfileId == patientId);

            var upcomingAppointments = await patientAppointments
                .CountAsync(a =>
                    a.AppointmentDateTime >= today &&
                    a.AppointmentDateTime < next7Days &&
                    a.Status == AppointmentStatus.Confirmed);

            var completedVisits = await patientAppointments
                .CountAsync(a => a.Status == AppointmentStatus.Completed);

            var cancelledOrMissed = await patientAppointments
                .CountAsync(a =>
                    a.Status == AppointmentStatus.Cancelled ||
                    a.Status == AppointmentStatus.NoShow);

            var totalAppointments = await patientAppointments.CountAsync();

            // ----------------------------
            // 3. Payment stats
            // ----------------------------
            var patientApptIds = await patientAppointments
                .Select(a => a.Id)
                .ToListAsync();

            decimal digitalPaymentsTotal = 0m;

            if (patientApptIds.Count > 0)
            {
                digitalPaymentsTotal = await _context.Payments
                    .Where(p => p.IsActive &&
                                patientApptIds.Contains(p.AppointmentId) &&
                                p.Status == PaymentStatus.Paid &&
                                p.Method != PaymentMethod.Cash)
                    .SumAsync(p => p.Amount);
            }

            var notificationsCount = await _context.Notifications
                .CountAsync(n => n.IsActive && n.UserId == user.Id);

            ViewBag.MyAppointmentsBadge = upcomingAppointments;
            ViewBag.NotificationBadge = notificationsCount;

            // ----------------------------
            // 4. Filters (Specialty + Experience)
            // ----------------------------
            var specialties = await _context.Specialties
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var specialtyOptions = specialties
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name,
                    Selected = (specialtyIdFilter.HasValue && s.Id == specialtyIdFilter.Value)
                })
                .ToList();

            specialtyOptions.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "All Specialties",
                Selected = !specialtyIdFilter.HasValue
            });

            var experienceOptions = new[]
            {
                new SelectListItem { Value = "",    Text = "Any Experience", Selected = string.IsNullOrWhiteSpace(experienceFilter) },
                new SelectListItem { Value = "0-3", Text = "0 - 3 years",    Selected = experienceFilter == "0-3" },
                new SelectListItem { Value = "4-7", Text = "4 - 7 years",    Selected = experienceFilter == "4-7" },
                new SelectListItem { Value = "8+",  Text = "8+ years",       Selected = experienceFilter == "8+" }
            }.ToList();

            // ----------------------------
            // 5. Doctor query (NO reviews, NO ratings)
            // ----------------------------
            var doctorsQuery = _context.DoctorProfiles
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.DoctorSpecialties)
                    .ThenInclude(ds => ds.Specialty)
                .Where(d =>
                    d.IsActive &&
                    d.IsAvailable &&
                    d.User.IsActive);

            if (!string.IsNullOrWhiteSpace(doctorNameFilter))
            {
                var term = doctorNameFilter.Trim().ToLower();
                doctorsQuery = doctorsQuery.Where(d =>
                    (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(term));
            }

            if (specialtyIdFilter.HasValue)
            {
                doctorsQuery = doctorsQuery.Where(d =>
                    d.DoctorSpecialties.Any(ds => ds.SpecialtyId == specialtyIdFilter.Value));
            }

            if (!string.IsNullOrWhiteSpace(experienceFilter))
            {
                switch (experienceFilter)
                {
                    case "0-3":
                        doctorsQuery = doctorsQuery.Where(d => d.Experience <= 3);
                        break;
                    case "4-7":
                        doctorsQuery = doctorsQuery.Where(d => d.Experience >= 4 && d.Experience <= 7);
                        break;
                    case "8+":
                        doctorsQuery = doctorsQuery.Where(d => d.Experience >= 8);
                        break;
                }
            }

            doctorsQuery = doctorsQuery
                .OrderByDescending(d => d.Experience)
                .ThenBy(d => d.User.FirstName)
                .ThenBy(d => d.User.LastName);

            // ----------------------------
            // 6. Pagination
            // ----------------------------
            if (page < 1) page = 1;

            var totalDoctors = await doctorsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalDoctors / (double)DoctorPageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var doctorsPage = await doctorsQuery
                .Skip((page - 1) * DoctorPageSize)
                .Take(DoctorPageSize)
                .ToListAsync();

            // ----------------------------
            // 7. Map doctors to cards (VisitCharge)
            // ----------------------------
            var doctorCards = doctorsPage
                .Select(d =>
                {
                    var primarySpecialty = d.DoctorSpecialties
                        .OrderByDescending(ds => ds.IsPrimary)
                        .ThenBy(ds => ds.Specialty.Name)
                        .FirstOrDefault();

                    return new PatientDashboardViewModel.DoctorCardItem
                    {
                        DoctorProfileId = d.Id,
                        FullName = (d.User.FirstName + " " + d.User.LastName).Trim(),
                        PrimarySpecialty = primarySpecialty?.Specialty.Name ?? "General Physician",
                        ExperienceText = $"{d.Experience}+ years experience",
                        ClinicInfo = !string.IsNullOrWhiteSpace(d.RoomNo)
                            ? $"Room {d.RoomNo}"
                            : null,
                        Qualification = d.Qualification,
                        Bio = d.Description,
                        ProfileImagePath = d.User.ProfileImagePath,

                        // ✅ instead of rating
                        VisitCharge = d.VisitCharge
                    };
                })
                .ToList();

            // ----------------------------
            // 8. Build ViewModel
            // ----------------------------
            var vm = new PatientDashboardViewModel
            {
                UpcomingAppointments = upcomingAppointments,
                CompletedVisits = completedVisits,
                CancelledOrMissed = cancelledOrMissed,
                DigitalPaymentsTotal = digitalPaymentsTotal,
                TotalAppointments = totalAppointments,

                MyAppointmentsBadge = upcomingAppointments,
                NotificationBadge = notificationsCount,

                DoctorNameFilter = doctorNameFilter,
                SpecialtyIdFilter = specialtyIdFilter,
                ExperienceFilter = experienceFilter,
                SpecialtyOptions = specialtyOptions,
                ExperienceOptions = experienceOptions,

                Doctors = doctorCards,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalDoctors = totalDoctors,
                PageSize = DoctorPageSize
            };

            return View(vm);
        }
    }
}
