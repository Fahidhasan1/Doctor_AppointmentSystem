using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public PatientProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (vm, redirect) = await BuildViewModelAsync();
            if (redirect != null) return redirect;

            ViewData["Title"] = "My Profile";
            return View(vm);
        }

        // Save profile (same page)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PatientProfileViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var patientProfile = await _context.PatientProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);

            if (patientProfile == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
                return RedirectToAction("Index", "PatientDashboard");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "My Profile";
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                // Ensure left summary fields are present even after validation error
                vm.ProfileImagePath = user.ProfileImagePath;
                vm.Email = user.Email;
                return View(vm);
            }

            // ApplicationUser fields
            user.FirstName = (vm.FirstName ?? "").Trim();
            user.LastName = (vm.LastName ?? "").Trim();
            user.PhoneNumber = vm.PhoneNumber?.Trim();
            user.DateOfBirth = vm.DateOfBirth;
            user.Gender = vm.Gender;
            user.Address = vm.Address?.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            // PatientProfile fields
            patientProfile.BloodGroup = vm.BloodGroup?.Trim();
            patientProfile.EmergencyContactName = vm.EmergencyContactName?.Trim();
            patientProfile.EmergencyContact = vm.EmergencyContact?.Trim();
            patientProfile.EmergencyContactRelation = vm.EmergencyContactRelation?.Trim();

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
            return RedirectToAction(nameof(Index));
        }

        // Change Photo (upload)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePhoto(IFormFile profilePhoto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (profilePhoto == null || profilePhoto.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction(nameof(Index));
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();

            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only JPG, JPEG, PNG, or WEBP images are allowed.";
                return RedirectToAction(nameof(Index));
            }

            if (profilePhoto.Length > 3 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Image size must be 3MB or less.";
                return RedirectToAction(nameof(Index));
            }

            // Ensure upload folder exists
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // Save new image
            var fileName = $"patient_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
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
                    var oldPhysical = Path.Combine(_env.WebRootPath, user.ProfileImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPhysical))
                        System.IO.File.Delete(oldPhysical);
                }
            }
            catch { /* ignore cleanup failure */ }

            user.ProfileImagePath = $"/uploads/profiles/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to update photo. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Profile photo updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<(PatientProfileViewModel vm, IActionResult? redirect)> BuildViewModelAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (new PatientProfileViewModel(), Challenge());

            var patientProfile = await _context.PatientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);

            if (patientProfile == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
                return (new PatientProfileViewModel(), RedirectToAction("Index", "PatientDashboard"));
            }

            var vm = new PatientProfileViewModel
            {
                // ApplicationUser
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                ProfileImagePath = user.ProfileImagePath,

                // PatientProfile
                BloodGroup = patientProfile.BloodGroup,
                EmergencyContactName = patientProfile.EmergencyContactName,
                EmergencyContact = patientProfile.EmergencyContact,
                EmergencyContactRelation = patientProfile.EmergencyContactRelation
            };

            return (vm, null);
        }
    }
}
