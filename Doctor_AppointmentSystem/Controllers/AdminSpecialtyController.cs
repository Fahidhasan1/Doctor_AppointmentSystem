using System;
using System.Collections.Generic;
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
    public class AdminSpecialtyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminSpecialtyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task SetCurrentAdminInViewBag()
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

        // -------------------------
        // LIST: /AdminSpecialty
        // -------------------------
        public async Task<IActionResult> Index(string? search)
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Manage Specialties";
            ViewBag.PageSubtitle = "Distinct medical departments and their status.";

            var query = _context.Specialties.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.Description ?? string.Empty).ToLower().Contains(term));
            }

            var list = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            int total = await _context.Specialties.CountAsync();
            ViewBag.TotalSpecialties = total;
            ViewBag.Search = search;

            var doctorCounts = await _context.DoctorSpecialties
                .GroupBy(ds => ds.SpecialtyId)
                .Select(g => new { SpecialtyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SpecialtyId, x => x.Count);

            var vm = list.Select(s => new SpecialtyListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                DoctorCount = doctorCounts.TryGetValue(s.Id, out var c) ? c : 0
            }).ToList();

            return View(vm);
        }

        // -------------------------
        // LIVE SEARCH (partial)
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> Search(string? search)
        {
            var query = _context.Specialties.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.Description ?? string.Empty).ToLower().Contains(term));
            }

            var list = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            var doctorCounts = await _context.DoctorSpecialties
                .GroupBy(ds => ds.SpecialtyId)
                .Select(g => new { SpecialtyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SpecialtyId, x => x.Count);

            var vm = list.Select(s => new SpecialtyListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                DoctorCount = doctorCounts.TryGetValue(s.Id, out var c) ? c : 0
            }).ToList();

            return PartialView("_SpecialtyTable", vm);
        }

        // -------------------------
        // GET: /AdminSpecialty/Create
        // -------------------------
        public async Task<IActionResult> Create()
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Add Specialty";
            ViewBag.PageSubtitle = "Create a new doctor specialty.";

            return View(new SpecialtyFormViewModel());
        }

        // -------------------------
        // POST: /AdminSpecialty/Create
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpecialtyFormViewModel model)
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Add Specialty";
            ViewBag.PageSubtitle = "Create a new doctor specialty.";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameTrim = model.Name.Trim();

            bool exists = await _context.Specialties
                .AnyAsync(s => s.Name.ToLower() == nameTrim.ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "A specialty with this name already exists.");
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var entity = new Specialty
            {
                Name = nameTrim,
                Description = string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),
                IsActive = true, // we keep the flag but do not expose it in UI
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser?.Id
            };

            _context.Specialties.Add(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Specialty created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // GET: /AdminSpecialty/Edit/5
        // -------------------------
        public async Task<IActionResult> Edit(int id)
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Edit Specialty";
            ViewBag.PageSubtitle = "Update specialty information.";

            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null)
            {
                return NotFound();
            }

            var vm = new SpecialtyFormViewModel
            {
                Id = specialty.Id,
                Name = specialty.Name,
                Description = specialty.Description,
                IsActive = specialty.IsActive
            };

            return View(vm);
        }

        // -------------------------
        // POST: /AdminSpecialty/Edit/5
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SpecialtyFormViewModel model)
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Edit Specialty";
            ViewBag.PageSubtitle = "Update specialty information.";

            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null)
            {
                return NotFound();
            }

            var nameTrim = model.Name.Trim();

            bool exists = await _context.Specialties
                .AnyAsync(s => s.Id != id &&
                               s.Name.ToLower() == nameTrim.ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "Another specialty with this name already exists.");
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            specialty.Name = nameTrim;
            specialty.Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();
            // NOTE: we are not changing IsActive from the form anymore
            specialty.UpdatedAt = DateTime.UtcNow;
            specialty.LastModifiedByUserId = currentUser?.Id;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Specialty updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
