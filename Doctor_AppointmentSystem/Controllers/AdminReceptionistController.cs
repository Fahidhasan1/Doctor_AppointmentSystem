using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReceptionistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public AdminReceptionistController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // Reuse sidebar header info
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

        // ---------- INDEX (list + search + filter) ----------
        // GET: /AdminReceptionist
        public async Task<IActionResult> Index(string? search, string? filter)
        {
            await SetCurrentAdminInViewBagAsync();
            ViewBag.PageTitle = "Manage Receptionist";

            var baseQuery = _context.ReceptionistProfiles
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                baseQuery = baseQuery.Where(r =>
                    (r.User!.FirstName + " " + r.User!.LastName).ToLower().Contains(search) ||
                    r.User.Email!.ToLower().Contains(search) ||
                    (r.User.PhoneNumber ?? "").ToLower().Contains(search) ||
                    (r.OfficePhone ?? "").ToLower().Contains(search) ||
                    (r.CounterNumber ?? "").ToLower().Contains(search));
            }

            // counts for pills
            int total = await baseQuery.CountAsync();
            int active = await baseQuery.CountAsync(r => r.IsActive && r.User!.IsActive);
            int inactive = total - active;

            if (filter == "active")
            {
                baseQuery = baseQuery.Where(r => r.IsActive && r.User!.IsActive);
            }
            else if (filter == "inactive")
            {
                baseQuery = baseQuery.Where(r => !r.IsActive || !r.User!.IsActive);
            }

            // after applying search + filter to baseQuery
            var vmList = await baseQuery
                .OrderBy(r => r.User!.FirstName)
                .ThenBy(r => r.User!.LastName)
                .Select(r => new ReceptionistListItemViewModel
                {
                    ReceptionistProfileId = r.Id,
                    UserId = r.UserId,
                    FullName = (r.User!.FirstName + " " + r.User!.LastName).Trim(),
                    Email = r.User.Email!,
                    PhoneNumber = r.User.PhoneNumber,
                    OfficePhone = r.OfficePhone,
                    CounterNumber = r.CounterNumber,
                    ProfileImagePath = r.User.ProfileImagePath,
                    IsActive = r.IsActive && r.User.IsActive
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Filter = filter;
            ViewBag.TotalReceptionists = total;
            ViewBag.ActiveReceptionists = active;
            ViewBag.InactiveReceptionists = inactive;

            return View(vmList);

        }

        [HttpGet]
        public async Task<IActionResult> Search(string? search, string? filter)
        {
            var allReceptionists = await _context.ReceptionistProfiles
                .Include(r => r.User)
                .ToListAsync();

            IEnumerable<ReceptionistProfile> list = allReceptionists;

            // Filter (active / inactive)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                switch (filter.ToLower())
                {
                    case "active":
                        list = list.Where(r => r.IsActive && r.User.IsActive);
                        break;

                    case "inactive":
                        list = list.Where(r => !r.IsActive || !r.User.IsActive);
                        break;
                }
            }

            // Live search: name, email, phone, office phone, counter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                list = list.Where(r =>
                {
                    var u = r.User;
                    var fullName = $"{u.FirstName} {u.LastName}".ToLower();
                    var email = (u.Email ?? "").ToLower();
                    var phone = (u.PhoneNumber ?? "").ToLower();
                    var officePhone = (r.OfficePhone ?? "").ToLower();
                    var counter = (r.CounterNumber ?? "").ToLower();

                    return fullName.Contains(term)
                           || email.Contains(term)
                           || phone.Contains(term)
                           || officePhone.Contains(term)
                           || counter.Contains(term);
                });
            }

            var vmList = list.Select(r =>
            {
                var u = r.User;

                return new ReceptionistListItemViewModel
                {
                    ReceptionistProfileId = r.Id,
                    UserId = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    OfficePhone = r.OfficePhone,
                    CounterNumber = r.CounterNumber,
                    ProfileImagePath = u.ProfileImagePath,
                    IsActive = r.IsActive && u.IsActive
                };
            }).ToList();

            return PartialView("_ReceptionistTable", vmList);
        }



        // ---------- CREATE ----------
        // GET: /AdminReceptionist/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await SetCurrentAdminInViewBagAsync();
            ViewBag.PageTitle = "Add New Receptionist";

            var vm = new ReceptionistCreateViewModel
            {
                Gender = Gender.Male // default, change if you want
            };

            return View(vm);
        }

        // POST: /AdminReceptionist/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceptionistCreateViewModel vm)
        {
            await SetCurrentAdminInViewBagAsync();

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            // ensure email unique
            var existingUser = await _userManager.FindByEmailAsync(vm.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "This email is already in use.");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Gender = vm.Gender,
                DateOfBirth = vm.DateOfBirth,
                IsActive = true,
            };

            var createResult = await _userManager.CreateAsync(user, vm.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Receptionist");

            // profile image upload (optional)
            string? profileImagePath = null;
            if (vm.ProfileImage != null && vm.ProfileImage.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "users");
                Directory.CreateDirectory(uploadsRoot);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(vm.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ProfileImage.CopyToAsync(stream);
                }

                profileImagePath = $"/uploads/users/{fileName}";
                user.ProfileImagePath = profileImagePath;
                await _userManager.UpdateAsync(user);
            }

            var profile = new ReceptionistProfile
            {
                UserId = user.Id,
                OfficePhone = vm.OfficePhone,
                CounterNumber = vm.CounterNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ReceptionistProfiles.Add(profile);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Receptionist created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ---------- EDIT ----------
        // GET: /AdminReceptionist/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            await SetCurrentAdminInViewBagAsync();
            ViewBag.PageTitle = "Edit Receptionist";

            var profile = await _context.ReceptionistProfiles
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (profile == null || profile.User == null)
            {
                return NotFound();
            }

            var vm = new ReceptionistEditViewModel
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FirstName = profile.User.FirstName,
                LastName = profile.User.LastName,
                Email = profile.User.Email!,
                PhoneNumber = profile.User.PhoneNumber ?? string.Empty,
                
                Gender = (Doctor_AppointmentSystem.Enums.Gender)profile.User.Gender,

                DateOfBirth = profile.User.DateOfBirth,
                OfficePhone = profile.OfficePhone,
                CounterNumber = profile.CounterNumber,
                ExistingProfileImagePath = profile.User.ProfileImagePath
            };

            return View(vm);
        }

        // POST: /AdminReceptionist/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReceptionistEditViewModel vm)
        {
            await SetCurrentAdminInViewBagAsync();

            if (id != vm.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var profile = await _context.ReceptionistProfiles
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (profile == null || profile.User == null)
            {
                return NotFound();
            }

            var user = profile.User;

            // email changed? ensure unique
            if (!string.Equals(user.Email, vm.Email, StringComparison.OrdinalIgnoreCase))
            {
                var another = await _userManager.FindByEmailAsync(vm.Email);
                if (another != null && another.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(vm.Email), "This email is already in use by another account.");
                    return View(vm);
                }
                user.Email = vm.Email;
                user.UserName = vm.Email;
            }

            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.PhoneNumber = vm.PhoneNumber;
            user.Gender = vm.Gender;
            user.DateOfBirth = vm.DateOfBirth;

            // handle new profile image if uploaded
            if (vm.NewProfileImage != null && vm.NewProfileImage.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "users");
                Directory.CreateDirectory(uploadsRoot);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(vm.NewProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.NewProfileImage.CopyToAsync(stream);
                }

                user.ProfileImagePath = $"/uploads/users/{fileName}";
            }

            profile.OfficePhone = vm.OfficePhone;
            profile.CounterNumber = vm.CounterNumber;
            profile.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            _context.ReceptionistProfiles.Update(profile);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Receptionist updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ACTIVATE / DEACTIVATE ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            // Optional: if you use this elsewhere
            await SetCurrentAdminInViewBagAsync();

            var adminUser = await _userManager.GetUserAsync(User);

            var profile = await _context.ReceptionistProfiles
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (profile == null || profile.User == null)
            {
                TempData["ErrorMessage"] = "Receptionist not found.";
                return RedirectToAction(nameof(Index));
            }

            profile.IsActive = true;
            profile.UpdatedAt = DateTime.UtcNow;          // ✅ use UpdatedAt, not UpdatedAtUtc
          

            profile.User.IsActive = true;
            profile.User.UpdatedAt = DateTime.UtcNow;     // same here
           

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Receptionist activated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            await SetCurrentAdminInViewBagAsync();

            var adminUser = await _userManager.GetUserAsync(User);

            var profile = await _context.ReceptionistProfiles
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (profile == null || profile.User == null)
            {
                TempData["ErrorMessage"] = "Receptionist not found.";
                return RedirectToAction(nameof(Index));
            }

            profile.IsActive = false;
            profile.UpdatedAt = DateTime.UtcNow;
           

            profile.User.IsActive = false;
            profile.User.UpdatedAt = DateTime.UtcNow;
            

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Receptionist deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

    }
}
