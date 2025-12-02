using System;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminPatientController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Reuse sidebar header info (same pattern as other Admin controllers)
        private async Task SetCurrentAdminInViewBagAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var name = (currentUser?.FirstName + " " + currentUser?.LastName)?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = User.Identity?.Name ?? "Admin";
            }

            ViewBag.CurrentUserName = name;
            ViewBag.ProfileImagePath = currentUser?.ProfileImagePath;
        }

        // ===========================
        // GET: /AdminPatient
        // ===========================
        public async Task<IActionResult> Index(string? search, string? filter)
        {
            await SetCurrentAdminInViewBagAsync();
            ViewBag.PageTitle = "Manage Patients";
            ViewBag.PageSubtitle = "View and manage patient accounts.";

            // Load all patient profiles with their linked ApplicationUser
            var allPatients = await _context.PatientProfiles
                .Include(p => p.User)
                .ToListAsync();

            // Global counters
            ViewBag.TotalPatients = allPatients.Count;
            ViewBag.ActivePatients = allPatients.Count(p => p.IsActive && p.User != null && p.User.IsActive);
            ViewBag.InactivePatients = allPatients.Count(p => !p.IsActive || (p.User != null && !p.User.IsActive));

            // Base query (in memory)
            var query = allPatients.AsEnumerable();

            // Filter pill (active / inactive)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                switch (filter.ToLower())
                {
                    case "active":
                        query = query.Where(p => p.IsActive && p.User != null && p.User.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(p => !p.IsActive || (p.User != null && !p.User.IsActive));
                        break;
                }
            }

            // Search by name / email / phone
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                query = query.Where(p =>
                {
                    var u = p.User;
                    if (u == null) return false;

                    var fullName = ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty))
                        .Trim()
                        .ToLower();

                    var email = (u.Email ?? string.Empty).ToLower();
                    var phone = (u.PhoneNumber ?? string.Empty).ToLower();

                    return fullName.Contains(term)
                           || email.Contains(term)
                           || phone.Contains(term);
                });
            }

            // Map to view model
            var vmList = query
                .Where(p => p.User != null)
                .OrderBy(p => p.User!.FirstName)
                .ThenBy(p => p.User!.LastName)
                .Select(p =>
                {
                    var u = p.User!;
                    var fullName = (u.FirstName + " " + u.LastName).Trim();
                    var genderStr = u.Gender.HasValue ? u.Gender.Value.ToString() : "Not specified";

                    return new PatientListItemViewModel
                    {
                        PatientProfileId = p.Id,
                        UserId = p.UserId,
                        FullName = fullName,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber,
                        Gender = genderStr,
                        ProfileImagePath = u.ProfileImagePath,
                        IsActive = p.IsActive && u.IsActive,
                        CreatedAt = p.CreatedAt
                    };
                })
                .ToList();

            ViewBag.Search = search;
            ViewBag.Filter = filter;

            return View(vmList);
        }



        [HttpGet]
        public async Task<IActionResult> Search(string? search, string? filter)
        {
            var allPatients = await _context.PatientProfiles
                .Include(p => p.User)
                .ToListAsync();

            IEnumerable<PatientProfile> patientsList = allPatients;

            // Filter (Active / Inactive)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                switch (filter.ToLower())
                {
                    case "active":
                        patientsList = patientsList.Where(p => p.IsActive && p.User.IsActive);
                        break;
                    case "inactive":
                        patientsList = patientsList.Where(p => !p.IsActive || !p.User.IsActive);
                        break;
                }
            }

            // LIVE SEARCH: name, email, phone
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();

                patientsList = patientsList.Where(p =>
                {
                    var u = p.User;
                    var fullName = $"{u.FirstName} {u.LastName}".ToLower();
                    var email = (u.Email ?? "").ToLower();
                    var phone = (u.PhoneNumber ?? "").ToLower();

                    return fullName.Contains(term)
                           || email.Contains(term)
                           || phone.Contains(term);
                });
            }

            var vmList = patientsList.Select(p =>
            {
                var u = p.User;

                return new PatientListItemViewModel
                {
                    PatientProfileId = p.Id,
                    UserId = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Gender = u.Gender.HasValue ? u.Gender.ToString() : "Not specified",
                    ProfileImagePath = u.ProfileImagePath,
                    IsActive = p.IsActive && u.IsActive,
                    CreatedAt = p.CreatedAt
                };
            }).ToList();

            return PartialView("_PatientTable", vmList);
        }

        // ===========================
        // POST: /AdminPatient/Activate/{id}
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            await SetCurrentAdminInViewBagAsync();

            var profile = await _context.PatientProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null || profile.User == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction(nameof(Index));
            }

            profile.IsActive = true;
            profile.UpdatedAt = DateTime.UtcNow;

            profile.User.IsActive = true;
            profile.User.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Patient activated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // POST: /AdminPatient/Deactivate/{id}
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            await SetCurrentAdminInViewBagAsync();

            var profile = await _context.PatientProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null || profile.User == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction(nameof(Index));
            }

            profile.IsActive = false;
            profile.UpdatedAt = DateTime.UtcNow;

            profile.User.IsActive = false;
            profile.User.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Patient deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
