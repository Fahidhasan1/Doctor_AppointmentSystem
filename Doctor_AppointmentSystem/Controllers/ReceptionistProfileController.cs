// File: Controllers/ReceptionistProfileController.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ReceptionistProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // ================== MY PROFILE ==================

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var (vm, redirect) = await BuildViewModelAsync();
            if (redirect != null) return redirect;

            ViewData["Title"] = "My Profile";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(ReceptionistProfileViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var receptionistProfile = await _context.ReceptionistProfiles
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);

            if (receptionistProfile == null)
            {
                TempData["ErrorMessage"] = "Receptionist profile not found or inactive.";
                return RedirectToAction("Index", "ReceptionistDashboard");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "My Profile";
                TempData["ErrorMessage"] = "Please correct the errors and try again.";

                // Keep display-only values after validation failure
                vm.ProfileImagePath = user.ProfileImagePath;
                vm.Email = user.Email;

                return View(vm);
            }

            // ---------- Editable: ApplicationUser fields ----------
            user.FirstName = (vm.FirstName ?? "").Trim();
            user.LastName = (vm.LastName ?? "").Trim();
            user.PhoneNumber = vm.PhoneNumber?.Trim();
            user.DateOfBirth = vm.DateOfBirth;
            user.Gender = vm.Gender;
            user.Address = vm.Address?.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            // ---------- Editable: ReceptionistProfile fields ----------
            receptionistProfile.OfficePhone = vm.OfficePhone?.Trim();
            receptionistProfile.CounterNumber = vm.CounterNumber?.Trim();
            receptionistProfile.UpdatedAt = DateTime.UtcNow;
            receptionistProfile.LastModifiedByUserId = user.Id;

            var userUpdate = await _userManager.UpdateAsync(user);
            if (!userUpdate.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to update your profile. Please try again.";
                ViewData["Title"] = "My Profile";

                vm.ProfileImagePath = user.ProfileImagePath;
                vm.Email = user.Email;

                return View(vm);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(MyProfile));
        }

        // ================== CHANGE PHOTO ==================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePhoto(IFormFile profilePhoto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (profilePhoto == null || profilePhoto.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction(nameof(MyProfile));
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();

            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only JPG, JPEG, PNG, or WEBP images are allowed.";
                return RedirectToAction(nameof(MyProfile));
            }

            // Ensure folder exists: wwwroot/uploads/profiles
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePhoto.CopyToAsync(stream);
            }

            // Remove old image (optional cleanup) if it's inside /uploads/profiles
            try
            {
                if (!string.IsNullOrWhiteSpace(user.ProfileImagePath) &&
                    user.ProfileImagePath.StartsWith("/uploads/profiles/", StringComparison.OrdinalIgnoreCase))
                {
                    var oldPhysical = Path.Combine(
                        _env.WebRootPath,
                        user.ProfileImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(oldPhysical))
                        System.IO.File.Delete(oldPhysical);
                }
            }
            catch
            {
                // ignore cleanup failure
            }

            user.ProfileImagePath = $"/uploads/profiles/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to update photo. Please try again.";
                return RedirectToAction(nameof(MyProfile));
            }

            TempData["SuccessMessage"] = "Profile photo updated successfully.";
            return RedirectToAction(nameof(MyProfile));
        }

        // ================== HELPER ==================

        private async Task<(ReceptionistProfileViewModel vm, IActionResult? redirect)> BuildViewModelAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (new ReceptionistProfileViewModel(), Challenge());

            var receptionistProfile = await _context.ReceptionistProfiles
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);

            if (receptionistProfile == null)
            {
                TempData["ErrorMessage"] = "Receptionist profile not found or inactive.";
                return (new ReceptionistProfileViewModel(), RedirectToAction("Index", "ReceptionistDashboard"));
            }

            var vm = new ReceptionistProfileViewModel
            {
                // Editable: ApplicationUser
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                ProfileImagePath = user.ProfileImagePath,

                // Editable: ReceptionistProfile
                OfficePhone = receptionistProfile.OfficePhone,
                CounterNumber = receptionistProfile.CounterNumber,

                // Display-only
                Email = user.Email
            };

            return (vm, null);
        }
    }
}
