using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Doctor_AppointmentSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // We are not using full-page login/register views at all.
        // If someone browses to /Account/Login manually, just send them Home.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Login (from modal)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = "Please enter email and password.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,          // ✅ this makes the cookie persistent when checked
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Index", "AdminDashboard");

                    if (await _userManager.IsInRoleAsync(user, "Doctor"))
                        return RedirectToAction("Index", "DoctorDashboard");

                    if (await _userManager.IsInRoleAsync(user, "Receptionist"))
                        return RedirectToAction("Index", "ReceptionistDashboard");

                    if (await _userManager.IsInRoleAsync(user, "Patient"))
                        return RedirectToAction("Index", "PatientDashboard");
                }

                return RedirectToAction("Index", "Home");
            }

            TempData["LoginError"] = "Invalid email or password.";
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Register (from modal – always Patient)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["RegisterError"] = "Please fill all required fields correctly.";
                return RedirectToAction("Index", "Home");
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                const string PatientRoleName = "Patient";

                if (!await _roleManager.RoleExistsAsync(PatientRoleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(PatientRoleName));
                }

                await _userManager.AddToRoleAsync(user, PatientRoleName);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "PatientDashboard");
            }

            TempData["RegisterError"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
