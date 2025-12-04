using System;
using System.IO;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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

        // GET: /AdminProfile
        public async Task<IActionResult> Index()
        {
            SetPageHeader();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            SetLayoutUser(user);

            // Ensure AdminProfile exists
            var adminProfile = await _context.AdminProfiles
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (adminProfile == null)
            {
                adminProfile = new AdminProfile
                {
                    UserId = user.Id,
                    IsSuperAdmin = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AdminProfiles.Add(adminProfile);
                await _context.SaveChangesAsync();
            }

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AdminProfileViewModel model)
        {
            SetPageHeader();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            SetLayoutUser(user);

            var adminProfile = await _context.AdminProfiles
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (adminProfile == null)
            {
                adminProfile = new AdminProfile
                {
                    UserId = user.Id,
                    IsSuperAdmin = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminProfiles.Add(adminProfile);
            }

            if (!ModelState.IsValid)
            {
                // keep current image preview
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

            // --- Update AdminProfile fields ---
            adminProfile.OfficePhoneNo = string.IsNullOrWhiteSpace(model.OfficePhoneNo)
                ? null
                : model.OfficePhoneNo.Trim();

            adminProfile.OfficeRoomNo = string.IsNullOrWhiteSpace(model.OfficeRoomNo)
                ? null
                : model.OfficeRoomNo.Trim();

            adminProfile.UpdatedAt = DateTime.UtcNow;

            // --- Handle profile photo upload ---
            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "users");
                Directory.CreateDirectory(uploadsRoot);

                // delete old file if any
                if (!string.IsNullOrWhiteSpace(user.ProfileImagePath))
                {
                    var oldPhysicalPath = Path.Combine(
                        _env.WebRootPath,
                        user.ProfileImagePath.TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }

                var ext = Path.GetExtension(model.ProfileImageFile.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImageFile.CopyToAsync(stream);
                }

                // store web-relative path in DB
                user.ProfileImagePath = $"/uploads/users/{fileName}";
            }

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
    }
}
