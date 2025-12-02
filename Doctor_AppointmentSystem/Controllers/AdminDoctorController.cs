using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public AdminDoctorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // -----------------------------------
        // Helpers
        // -----------------------------------
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

        private async Task<List<SelectListItem>> GetSpecialtyOptionsAsync()
        {
            var specialties = await _context.Specialties
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return specialties
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();
        }

        // -----------------------------------
        // LIST: /AdminDoctor
        // -----------------------------------
        public async Task<IActionResult> Index(string? search, string? filter)
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Manage Doctors";
            // Load ALL doctors once
            var allDoctors = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
                .ToListAsync();

            // ---- Global counters (always real values) ----
            ViewBag.TotalDoctors = allDoctors.Count;
            ViewBag.ActiveDoctors = allDoctors.Count(d => d.IsActive && d.User.IsActive);
            ViewBag.InactiveDoctors = allDoctors.Count(d => !d.IsActive || !d.User.IsActive);

            // Start from all doctors, then apply filter + search in-memory
            IEnumerable<DoctorProfile> doctorsList = allDoctors;

            // Filter pill (active / inactive)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                switch (filter.ToLower())
                {
                    case "active":
                        doctorsList = doctorsList.Where(d => d.IsActive && d.User.IsActive);
                        break;
                    case "inactive":
                        doctorsList = doctorsList.Where(d => !d.IsActive || !d.User.IsActive);
                        break;
                }
            }

            // Search by name / email / specialties / phone
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();

                doctorsList = doctorsList.Where(d =>
                {
                    var user = d.User;
                    var fullName = $"{user.FirstName} {user.LastName}".ToLower();
                    var email = (user.Email ?? string.Empty).ToLower();
                    var phone = (user.PhoneNumber ?? string.Empty).ToLower();
                    var specialties = string.Join(",",
                        d.DoctorSpecialties.Select(s => s.Specialty.Name)).ToLower();

                    // partial search on all fields
                    return fullName.Contains(term)
                           || email.Contains(term)
                           || phone.Contains(term)
                           || specialties.Contains(term);
                });
            }


            // Map to view model
            var vmList = doctorsList.Select(d =>
            {
                var user = d.User;
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                var specialtyStr = string.Join(", ",
                    d.DoctorSpecialties.Select(s => s.Specialty.Name));

                return new Doctor_AppointmentSystem.ViewModels.DoctorListItemViewModel
                {
                    DoctorProfileId = d.Id,
                    UserId = d.UserId,
                    FullName = fullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImagePath = user.ProfileImagePath,
                    Designation = d.Designation,
                    Experience = d.Experience,
                    Specialties = specialtyStr,
                    IsActive = d.IsActive && user.IsActive
                };
            }).ToList();

            ViewBag.Search = search;
            ViewBag.Filter = filter;

            return View(vmList);
        }



        [HttpGet]
        public async Task<IActionResult> Search(string? search, string? filter)
        {
            // same logic as Index, but return only the table partial

            var allDoctors = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
                .ToListAsync();

            IEnumerable<DoctorProfile> doctorsList = allDoctors;

            // apply filter (active / inactive)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                switch (filter.ToLower())
                {
                    case "active":
                        doctorsList = doctorsList.Where(d => d.IsActive && d.User.IsActive);
                        break;
                    case "inactive":
                        doctorsList = doctorsList.Where(d => !d.IsActive || !d.User.IsActive);
                        break;
                }
            }

            // search by name / email / specialties / phone
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();

                doctorsList = doctorsList.Where(d =>
                {
                    var user = d.User;
                    var fullName = $"{user.FirstName} {user.LastName}".ToLower();
                    var email = (user.Email ?? string.Empty).ToLower();
                    var phone = (user.PhoneNumber ?? string.Empty).ToLower();
                    var specialties = string.Join(",",
                        d.DoctorSpecialties.Select(s => s.Specialty.Name)).ToLower();

                    return fullName.Contains(term)
                           || email.Contains(term)
                           || phone.Contains(term)
                           || specialties.Contains(term);
                });
            }

            var vmList = doctorsList.Select(d =>
            {
                var user = d.User;
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                var specialtyStr = string.Join(", ",
                    d.DoctorSpecialties.Select(s => s.Specialty.Name));

                return new DoctorListItemViewModel
                {
                    DoctorProfileId = d.Id,
                    UserId = d.UserId,
                    FullName = fullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImagePath = user.ProfileImagePath,
                    Designation = d.Designation,
                    Experience = d.Experience,
                    Specialties = specialtyStr,
                    IsActive = d.IsActive && user.IsActive
                };
            }).ToList();

            return PartialView("_DoctorTable", vmList);
        }


        // -----------------------------------
        // GET: /AdminDoctor/Create
        // -----------------------------------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Add New Doctor";

            var vm = new DoctorCreateViewModel
            {
                Experience = 0,
                VisitCharge = 0
            };

            vm.SpecialtyOptions = await GetSpecialtyOptionsAsync();
            return View(vm);
        }

        // -----------------------------------
        // POST: /AdminDoctor/Create
        // -----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorCreateViewModel model)
        {
            await SetCurrentAdminInViewBag();
            model.SpecialtyOptions = await GetSpecialtyOptionsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ensure email is unique
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(model);
            }

            var adminUser = await _userManager.GetUserAsync(User);

            var newUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = adminUser?.Id
            };

            // Handle profile image upload
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsRootFolder = Path.Combine(_env.WebRootPath, "uploads", "users");
                if (!Directory.Exists(uploadsRootFolder))
                {
                    Directory.CreateDirectory(uploadsRootFolder);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsRootFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }

                newUser.ProfileImagePath = $"/uploads/users/{fileName}";
            }

            // Create identity user
            var createResult = await _userManager.CreateAsync(newUser, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Assign Doctor role
            await _userManager.AddToRoleAsync(newUser, "Doctor");

            // Create DoctorProfile
            var doctorProfile = new DoctorProfile
            {
                UserId = newUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = adminUser?.Id,
                Designation = model.Designation,
                Experience = model.Experience,
                LicenseNumber = model.LicenseNumber,
                Qualification = model.Qualification,
                VisitCharge = model.VisitCharge
            };

            _context.DoctorProfiles.Add(doctorProfile);
            await _context.SaveChangesAsync(); // get doctorProfile.Id

            // Link specialties
            if (model.SelectedSpecialtyIds != null && model.SelectedSpecialtyIds.Any())
            {
                var doctorSpecialties = model.SelectedSpecialtyIds.Select(sid => new DoctorSpecialty
                {
                    DoctorProfileId = doctorProfile.Id,
                    SpecialtyId = sid
                });

                _context.DoctorSpecialties.AddRange(doctorSpecialties);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Doctor created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------
        // GET: /AdminDoctor/Edit/{id}
        // -----------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            await SetCurrentAdminInViewBag();
            ViewBag.PageTitle = "Edit Doctor";

            var doctorProfile = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.DoctorSpecialties)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctorProfile == null)
            {
                return NotFound();
            }

            var user = doctorProfile.User;

            var vm = new DoctorEditViewModel
            {
                DoctorProfileId = doctorProfile.Id,
                UserId = doctorProfile.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                
                Gender = user.Gender ?? Doctor_AppointmentSystem.Enums.Gender.Male,

                DateOfBirth = user.DateOfBirth,
                LicenseNumber = doctorProfile.LicenseNumber,
                Qualification = doctorProfile.Qualification,
                Designation = doctorProfile.Designation,
                Experience = doctorProfile.Experience,
                VisitCharge = doctorProfile.VisitCharge,
                ExistingProfileImagePath = user.ProfileImagePath,
                SelectedSpecialtyIds = doctorProfile.DoctorSpecialties
                    .Select(ds => ds.SpecialtyId)
                    .ToList()
            };

            vm.SpecialtyOptions = await GetSpecialtyOptionsAsync();
            return View(vm);
        }

        // -----------------------------------
        // POST: /AdminDoctor/Edit
        // -----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DoctorEditViewModel model)
        {
            await SetCurrentAdminInViewBag();
            model.SpecialtyOptions = await GetSpecialtyOptionsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var doctorProfile = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.DoctorSpecialties)
                .FirstOrDefaultAsync(d => d.Id == model.DoctorProfileId);

            if (doctorProfile == null)
            {
                return NotFound();
            }

            var user = doctorProfile.User;
            var adminUser = await _userManager.GetUserAsync(User);

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.UpdatedAt = DateTime.UtcNow;

            doctorProfile.Qualification = model.Qualification;
            doctorProfile.Designation = model.Designation;
            doctorProfile.Experience = model.Experience;
            doctorProfile.VisitCharge = model.VisitCharge;
            doctorProfile.UpdatedAt = DateTime.UtcNow;


            // Handle new profile image
            if (model.NewProfileImage != null && model.NewProfileImage.Length > 0)
            {
                var uploadsRootFolder = Path.Combine(_env.WebRootPath, "uploads", "users");
                if (!Directory.Exists(uploadsRootFolder))
                {
                    Directory.CreateDirectory(uploadsRootFolder);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.NewProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsRootFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.NewProfileImage.CopyToAsync(stream);
                }

                user.ProfileImagePath = $"/uploads/users/{fileName}";
            }

            // Update specialties: remove old, add new
            var existingSpecs = doctorProfile.DoctorSpecialties.ToList();
            _context.DoctorSpecialties.RemoveRange(existingSpecs);

            if (model.SelectedSpecialtyIds != null && model.SelectedSpecialtyIds.Any())
            {
                var newSpecs = model.SelectedSpecialtyIds.Select(sid => new DoctorSpecialty
                {
                    DoctorProfileId = doctorProfile.Id,
                    SpecialtyId = sid
                });

                await _context.DoctorSpecialties.AddRangeAsync(newSpecs);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Doctor updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------
        // POST: /AdminDoctor/Deactivate/{id}
        // -----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var doctorProfile = await _context.DoctorProfiles
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctorProfile == null)
            {
                return NotFound();
            }

            var adminUser = await _userManager.GetUserAsync(User);

            doctorProfile.IsActive = false;
            doctorProfile.UpdatedAt = DateTime.UtcNow;

            if (doctorProfile.User != null)
            {
                doctorProfile.User.IsActive = false;
                doctorProfile.User.UpdatedAt = DateTime.UtcNow;
            }


            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Doctor deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------
        // POST: /AdminDoctor/Activate/{id}
        // -----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var doctorProfile = await _context.DoctorProfiles
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctorProfile == null)
            {
                return NotFound();
            }

            var adminUser = await _userManager.GetUserAsync(User);

            doctorProfile.IsActive = true;
            doctorProfile.UpdatedAt = DateTime.UtcNow;

            if (doctorProfile.User != null)
            {
                doctorProfile.User.IsActive = true;
                doctorProfile.User.UpdatedAt = DateTime.UtcNow;
            }


            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Doctor activated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
