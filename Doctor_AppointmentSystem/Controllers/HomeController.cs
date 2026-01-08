
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int DoctorPageSize = 6;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string doctorNameFilter,
            int? specialtyIdFilter,
            string experienceFilter,
            int page = 1)
        {
            // ----------------------------
            // 1) Dropdowns (Specialty + Experience)
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
            // 2) Doctor Query
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

            if (!string.IsNullOrWhiteSpace(doctorNameFilter))
            {
                var term = doctorNameFilter.Trim().ToLower();
                doctorsQuery = doctorsQuery.Where(d =>
                    (d.User.FirstName + " " + d.User.LastName).ToLower().Contains(term));
            }

            if (specialtyIdFilter.HasValue)
            {
                var sid = specialtyIdFilter.Value;
                doctorsQuery = doctorsQuery.Where(d =>
                    d.DoctorSpecialties.Any(ds => ds.SpecialtyId == sid));
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
            // 3) Pagination
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
            // 4) Map cards (same fields used by PatientDashboard markup)
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

                    return new HomeIndexViewModel.DoctorCardItem
                    {
                        DoctorProfileId = d.Id,
                        FullName = (d.User.FirstName + " " + d.User.LastName).Trim(),
                        PrimarySpecialty = primarySpecialty?.Specialty.Name ?? "General Physician",
                        ExperienceText = $"{d.Experience}+ years experience",
                        ClinicInfo = !string.IsNullOrWhiteSpace(d.RoomNo) ? $"Room {d.RoomNo}" : null,
                        Qualification = d.Qualification,
                        ProfileImagePath = d.User.ProfileImagePath,
                        AverageRating = Math.Round(averageRating, 1),
                        ReviewCount = reviewCount
                    };
                })
                .ToList();

            // ----------------------------
            // 5) ViewModel
            // ----------------------------
            var vm = new HomeIndexViewModel
            {
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
