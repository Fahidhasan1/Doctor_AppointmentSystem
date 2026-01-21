// File: Controllers/AdminProfileController.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
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
    [Authorize(Roles = "Admin")]
    public class AdminProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public AdminProfileController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _env = env;
            _context = context;
        }

        // Common header text for layout
        private void SetPageHeader()
        {
            ViewBag.PageTitle = "My Profile";
            ViewBag.PageSubtitle = "View and update your admin profile information.";
        }

        // Common sidebar user info for _Layout_Admin
        private void SetLayoutUser(ApplicationUser user)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = user.Email ?? user.UserName ?? "Admin";

            ViewBag.CurrentUserName = fullName;
            ViewBag.ProfileImagePath = user.ProfileImagePath;
        }

        // Ensures AdminProfile exists for the current user
        private async Task<AdminProfile> EnsureAdminProfileAsync(ApplicationUser user)
        {
            var adminProfile = await _context.AdminProfiles
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (adminProfile != null) return adminProfile;

            adminProfile = new AdminProfile
            {
                UserId = user.Id,
                IsSuperAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminProfiles.Add(adminProfile);
            await _context.SaveChangesAsync();

            return adminProfile;
        }

        // GET: /AdminProfile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SetPageHeader();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            SetLayoutUser(user);

            var adminProfile = await EnsureAdminProfileAsync(user);

            var vm = new AdminProfileViewModel
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender.ToString(),
                Address = user.Address,
                ProfileImagePath = user.ProfileImagePath,

                OfficePhoneNo = adminProfile.OfficePhoneNo,
                OfficeRoomNo = adminProfile.OfficeRoomNo
            };

            return View(vm);
        }

        // POST: /AdminProfile
        // (Photo upload is handled ONLY by ChangePhoto to avoid 2 upload systems.)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AdminProfileViewModel model)
        {
            SetPageHeader();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            SetLayoutUser(user);

            var adminProfile = await EnsureAdminProfileAsync(user);

            if (!ModelState.IsValid)
            {
                // keep current image preview
                model.ProfileImagePath = user.ProfileImagePath;
                return View(model);
            }

            // DOB must not be in the future
            if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth cannot be in the future.");
                model.ProfileImagePath = user.ProfileImagePath;
                return View(model);
            }

            // --- Update Identity user fields ---
            user.FirstName = model.FirstName?.Trim();
            user.LastName = model.LastName?.Trim();

            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim();

            user.DateOfBirth = model.DateOfBirth;

            user.Address = string.IsNullOrWhiteSpace(model.Address)
                ? null
                : model.Address.Trim();

            if (!string.IsNullOrWhiteSpace(model.Gender) &&
                Enum.TryParse<Gender>(model.Gender, out var parsedGender))
            {
                user.Gender = parsedGender;
            }

            user.UpdatedAt = DateTime.UtcNow;

            // --- Update AdminProfile fields ---
            adminProfile.OfficePhoneNo = string.IsNullOrWhiteSpace(model.OfficePhoneNo)
                ? null
                : model.OfficePhoneNo.Trim();

            adminProfile.OfficeRoomNo = string.IsNullOrWhiteSpace(model.OfficeRoomNo)
                ? null
                : model.OfficeRoomNo.Trim();

            adminProfile.UpdatedAt = DateTime.UtcNow;

            // --- Persist changes ---
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                model.ProfileImagePath = user.ProfileImagePath;
                return View(model);
            }

            _context.AdminProfiles.Update(adminProfile);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminProfile/ChangePhoto
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

            if (Array.IndexOf(allowed, ext) < 0)
            {
                TempData["ErrorMessage"] = "Only JPG, JPEG, PNG, or WEBP images are allowed.";
                return RedirectToAction(nameof(Index));
            }

            // Folder: wwwroot/uploads/profiles  (consistent with patient/receptionist)
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePhoto.CopyToAsync(stream);
            }

            // Delete old image if it was inside /uploads/profiles
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
                // ignore cleanup errors
            }

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
    }
}
