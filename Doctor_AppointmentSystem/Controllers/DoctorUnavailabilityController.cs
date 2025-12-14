using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorUnavailabilityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorUnavailabilityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /DoctorUnavailability/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var doctorProfileId = await GetCurrentDoctorProfileIdAsync();
            if (doctorProfileId == null) return Forbid();

            var items = await _context.DoctorUnavailabilities
                .Where(x => x.DoctorProfileId == doctorProfileId.Value && x.IsActive && x.IsFullDay)
                .OrderByDescending(x => x.StartDateTime)
                .Select(x => new DoctorUnavailabilityListItemViewModel
                {
                    Id = x.Id,
                    Date = x.StartDateTime.Date,
                    Reason = x.Reason
                })
                .ToListAsync();

            var vm = new DoctorUnavailabilityIndexViewModel
            {
                Items = items
            };

            return View(vm);
        }

        // POST: /DoctorUnavailability/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(DoctorUnavailabilityIndexViewModel vm)
        {
            var doctorProfileId = await GetCurrentDoctorProfileIdAsync();
            if (doctorProfileId == null) return Forbid();

            if (!vm.UnavailableDate.HasValue)
            {
                TempData["ErrorMessage"] = "Please select a date.";
                return RedirectToAction(nameof(Index));
            }

            var date = vm.UnavailableDate.Value.Date;
            var start = date; // 00:00
            var end = date.AddDays(1).AddSeconds(-1); // 23:59:59

            var exists = await _context.DoctorUnavailabilities.AnyAsync(x =>
                x.DoctorProfileId == doctorProfileId.Value &&
                x.IsActive &&
                x.IsFullDay &&
                x.StartDateTime.Date == date);

            if (exists)
            {
                TempData["ErrorMessage"] = "This date is already marked as unavailable.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _context.DoctorUnavailabilities.Add(new DoctorUnavailability
            {
                DoctorProfileId = doctorProfileId.Value,
                StartDateTime = start,
                EndDateTime = end,
                IsFullDay = true,
                Reason = vm.Reason?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Unavailable date added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /DoctorUnavailability/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var doctorProfileId = await GetCurrentDoctorProfileIdAsync();
            if (doctorProfileId == null) return Forbid();

            var item = await _context.DoctorUnavailabilities
                .FirstOrDefaultAsync(x => x.Id == id && x.DoctorProfileId == doctorProfileId.Value && x.IsActive);

            if (item == null)
            {
                TempData["ErrorMessage"] = "Unavailable date not found.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
            item.LastModifiedByUserId = userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Unavailable date removed.";
            return RedirectToAction(nameof(Index));
        }

        // Helper
        private async Task<int?> GetCurrentDoctorProfileIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _context.DoctorProfiles
                .Where(d => d.UserId == userId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }
    }
}
