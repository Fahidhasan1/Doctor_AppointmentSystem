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

        private const int DoctorPageSize = 8;

        public PatientDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /PatientDashboard
        // Doctor filters come from the query string / form as GET parameters
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
                // force re-login if something is wrong
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
            // 2. Base appointment query for this patient
            // ----------------------------
            var patientAppointments = _context.Appointments
                .Where(a => a.IsActive && a.PatientProfileId == patientId);

            // Upcoming within next 7 days – for top card + sidebar badge
            var upcomingAppointments = await patientAppointments
                .CountAsync(a =>
                    a.AppointmentDateTime >= today &&
                    a.AppointmentDateTime < next7Days &&
                    a.Status == AppointmentStatus.Confirmed);

            // Completed (all time)
            var completedVisits = await patientAppointments
                .CountAsync(a => a.Status == AppointmentStatus.Completed);

            // Cancelled or missed (Cancelled + NoShow)
            var cancelledOrMissed = await patientAppointments
                .CountAsync(a =>
                    a.Status == AppointmentStatus.Cancelled ||
                    a.Status == AppointmentStatus.NoShow);

            // Total appointments (for internal use if needed later)
            var totalAppointments = await patientAppointments.CountAsync();

            // Payment stats (for Digital Payments card)
            var patientApptIds = await patientAppointments
                .Select(a => a.Id)
                .ToListAsync();

            decimal digitalPaymentsTotal = 0m;

            if (patientApptIds.Count > 0)
            {
                var paymentsForPatient = _context.Payments
                    .Where(p => p.IsActive &&
                                patientApptIds.Contains(p.AppointmentId));

                digitalPaymentsTotal = await paymentsForPatient
                    .Where(p =>
                        p.Status == PaymentStatus.Paid &&
                        p.Method != PaymentMethod.Cash)
                    .SumAsync(p => p.Amount);
            }

            // Notification count (for badge / future use)
            var notificationsCount = await _context.Notifications
                .CountAsync(n => n.IsActive && n.UserId == user.Id);

            // These ViewBags can be used by the layout/sidebar if you want badges there
            ViewBag.MyAppointmentsBadge = upcomingAppointments;
            ViewBag.NotificationBadge = notificationsCount;

            // ----------------------------
            // 3. Doctor filter dropdowns (Specialties + Experience)
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

            // Insert "All Specialties" at the top
            specialtyOptions.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "All Specialties",
                Selected = !specialtyIdFilter.HasValue
            });

            // Experience dropdown values – matches the UI text
            var experienceOptions = new[]
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "Any Experience",
                    Selected = string.IsNullOrWhiteSpace(experienceFilter)
                },
                new SelectListItem
                {
                    Value = "0-3",
                    Text = "0 - 3 years",
                    Selected = experienceFilter == "0-3"
                },
                new SelectListItem
                {
                    Value = "4-7",
                    Text = "4 - 7 years",
                    Selected = experienceFilter == "4-7"
                },
                new SelectListItem
                {
                    Value = "8+",
                    Text = "8+ years",
                    Selected = experienceFilter == "8+"
                }
            }.ToList();

            // ----------------------------
            // 4. Base doctor query (for "Find Your Doctor")
            // ----------------------------
            var doctorsQuery = _context.DoctorProfiles
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.DoctorSpecialties)
                    .ThenInclude(ds => ds.Specialty)
                .Include(d => d.Reviews)
                .Where(d =>
                    d.IsActive &&
                    d.IsAvailable &&
                    d.User.IsActive);

            // Filter by doctor name
            if (!string.IsNullOrWhiteSpace(doctorNameFilter))
            {
                var term = doctorNameFilter.Trim().ToLower();
                doctorsQuery = doctorsQuery.Where(d =>
                    (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(term));
            }

            // Filter by specialty
            if (specialtyIdFilter.HasValue)
            {
                var sid = specialtyIdFilter.Value;
                doctorsQuery = doctorsQuery.Where(d =>
                    d.DoctorSpecialties.Any(ds => ds.SpecialtyId == sid));
            }

            // Filter by experience range
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

            // Order by experience desc, then name
            doctorsQuery = doctorsQuery
                .OrderByDescending(d => d.Experience)
                .ThenBy(d => d.User.FirstName)
                .ThenBy(d => d.User.LastName);

            // ----------------------------
            // 5. Pagination for doctors
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
            // 6. Map doctors to card items
            // ----------------------------
            var doctorCards = doctorsPage
                .Select(d =>
                {
                    var primarySpecialty = d.DoctorSpecialties
                        .OrderByDescending(ds => ds.IsPrimary)
                        .ThenBy(ds => ds.Specialty.Name)
                        .FirstOrDefault();

                    var reviews = d.Reviews
                        .Where(r => r.IsActive && r.IsVisible)
                        .ToList();

                    double averageRating = 0;
                    int reviewCount = reviews.Count;
                    if (reviewCount > 0)
                    {
                        averageRating = reviews.Average(r => (double)r.Rating);
                    }

                    return new PatientDashboardViewModel.DoctorCardItem
                    {
                        DoctorProfileId = d.Id,
                        FullName = (d.User.FirstName + " " + d.User.LastName).Trim(),
                        PrimarySpecialty = primarySpecialty?.Specialty.Name ?? "General Physician",
                        ExperienceText = $"{d.Experience}+ years experience",
                        ClinicInfo = !string.IsNullOrWhiteSpace(d.RoomNo)
                            ? $"Room {d.RoomNo}"
                            : null,
                        Bio = d.Description,
                        ProfileImagePath = d.User.ProfileImagePath,
                        AverageRating = Math.Round(averageRating, 1),
                        ReviewCount = reviewCount
                    };
                })
                .ToList();

            // ----------------------------
            // 7. Build ViewModel
            // ----------------------------
            var vm = new PatientDashboardViewModel
            {
                // top cards
                UpcomingAppointments = upcomingAppointments,
                CompletedVisits = completedVisits,
                CancelledOrMissed = cancelledOrMissed,
                DigitalPaymentsTotal = digitalPaymentsTotal,

                // filters (preserve current filter state)
                DoctorNameFilter = doctorNameFilter,
                SpecialtyIdFilter = specialtyIdFilter,
                ExperienceFilter = experienceFilter,
                SpecialtyOptions = specialtyOptions,
                ExperienceOptions = experienceOptions,

                // doctor cards + pagination
                Doctors = doctorCards,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalDoctors = totalDoctors
            };

            return View(vm);
        }

        // Later you’ll create separate controllers like:
        // PatientAppointmentsController, PatientNotificationsController, etc.
    }
}
