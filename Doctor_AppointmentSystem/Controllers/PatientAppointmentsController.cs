//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Security.Claims;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//using Doctor_AppointmentSystem.Data;
//using Doctor_AppointmentSystem.Models;
//using Doctor_AppointmentSystem.Enums;
//using Doctor_AppointmentSystem.ViewModels;

//namespace Doctor_AppointmentSystem.Controllers
//{
//    [Authorize(Roles = "Patient")]
//    public class PatientAppointmentsController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;

//        public PatientAppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // GET: /PatientAppointments?filter=upcoming|completed|cancelled|payments
//        public async Task<IActionResult> Index(string? filter, bool fromCard = false)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            if (user == null) return Challenge();

//            var patientProfile = await _context.PatientProfiles
//                .AsNoTracking()
//                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);

//            if (patientProfile == null)
//            {
//                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
//                return RedirectToAction("Index", "PatientDashboard");
//            }

//            var normalized = (filter ?? "").Trim().ToLowerInvariant();
//            var now = DateTime.Now;

//            IQueryable<Appointment> baseQuery = _context.Appointments
//                .AsNoTracking()
//                .Where(a => a.PatientProfileId == patientProfile.Id && a.IsActive);

//            switch (normalized)
//            {
//                case "upcoming":
//                    baseQuery = baseQuery.Where(a =>
//                        a.AppointmentDateTime >= now &&
//                        (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Rescheduled));
//                    break;

//                case "completed":
//                    baseQuery = baseQuery.Where(a => a.Status == AppointmentStatus.Completed);
//                    break;

//                case "cancelled":
//                    baseQuery = baseQuery.Where(a =>
//                        a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow);
//                    break;

//                // ✅ NEW: payments mode (Digital Payments card)
//                // We filter after payment mapping because payments are in Payments table.
//                case "payments":
//                    // keep baseQuery as-is (patient's appointments)
//                    // we'll reduce the list to only paid after mapping.
//                    break;

//                default:
//                    normalized = "";
//                    break;
//            }

//            var list = await baseQuery
//                .OrderByDescending(a => a.AppointmentDateTime)
//                .Select(a => new PatientAppointmentListItemViewModel
//                {
//                    Id = a.Id,
//                    AppointmentDateTime = a.AppointmentDateTime,
//                    DurationMinutes = a.DurationMinutes,
//                    VisitType = a.VisitType,
//                    Status = a.Status,

//                    DoctorName = _context.DoctorProfiles
//                        .Where(d => d.Id == a.DoctorProfileId)
//                        .Select(d => ((d.User.FirstName ?? "") + " " + (d.User.LastName ?? "")).Trim())
//                        .FirstOrDefault() ?? "",

//                    SpecialtyName = _context.DoctorSpecialties
//                        .Where(ds => ds.DoctorProfileId == a.DoctorProfileId)
//                        .Select(ds => ds.Specialty.Name)
//                        .FirstOrDefault(),

//                    FeeAmount = _context.DoctorProfiles
//                        .Where(d => d.Id == a.DoctorProfileId)
//                        .Select(d => d.VisitCharge)
//                        .FirstOrDefault(),

//                    PaymentDisplay = "Unpaid",
//                    Amount = 0m,
//                    IsPaid = false,
//                    PaidPaymentId = null, // ✅ make sure this exists in your VM

//                    CanShowActions = false,
//                    CanPay = false,
//                    CanCancel = false
//                })
//                .ToListAsync();

//            var ids = list.Select(x => x.Id).ToList();

//            var latestPayments = await _context.Payments
//                .AsNoTracking()
//                .Where(p => p.IsActive && ids.Contains(p.AppointmentId))
//                .GroupBy(p => p.AppointmentId)
//                .Select(g => g.OrderByDescending(p => p.CreatedAt).FirstOrDefault())
//                .ToListAsync();

//            var payMap = latestPayments
//                .Where(p => p != null)
//                .ToDictionary(p => p!.AppointmentId, p => p!);

//            foreach (var row in list)
//            {
//                var isCancelled = row.Status == AppointmentStatus.Cancelled
//                                  || row.Status == AppointmentStatus.NoShow;

//                var isCompleted = row.Status == AppointmentStatus.Completed;
//                var isFuture = row.AppointmentDateTime >= DateTime.Now;

//                // -------------------------
//                // PAYMENT STATUS HANDLING
//                // -------------------------
//                if (payMap.TryGetValue(row.Id, out var pay))
//                {
//                    if (pay.Status == PaymentStatus.Paid)
//                    {
//                        row.PaymentDisplay = $"Paid ({pay.Method})";
//                        row.IsPaid = true;
//                        row.Amount = pay.Amount;
//                        row.PaidPaymentId = pay.Id; // 🔑 enables receipt
//                    }
//                    else
//                    {
//                        row.PaymentDisplay = "Unpaid";
//                        row.IsPaid = false;
//                        row.Amount = row.FeeAmount;
//                        row.PaidPaymentId = null;
//                    }
//                }
//                else
//                {
//                    row.PaymentDisplay = "Unpaid";
//                    row.Amount = row.FeeAmount;
//                    row.IsPaid = false;
//                    row.PaidPaymentId = null;
//                }

//                // -------------------------
//                // ACTION VISIBILITY LOGIC
//                // -------------------------
//                var isConfirmLike =
//                    row.Status == AppointmentStatus.Confirmed
//                    || row.Status == AppointmentStatus.Rescheduled;

//                row.CanCancel =
//                    isFuture
//                    && isConfirmLike
//                    && !isCancelled
//                    && !isCompleted
//                    && !row.IsPaid;

//                row.CanPay =
//                    isFuture
//                    && isConfirmLike
//                    && !isCancelled
//                    && !isCompleted
//                    && !row.IsPaid;

//                row.CanShowActions =
//                    row.CanCancel
//                    || row.CanPay
//                    || row.IsPaid;
//            }

//            // ✅ NEW: Apply payments filter AFTER payment mapping
//            if (normalized == "payments")
//            {
//                list = list
//                    .Where(x => x.IsPaid && x.PaidPaymentId.HasValue)
//                    .OrderByDescending(x => x.AppointmentDateTime)
//                    .ToList();
//            }

//            var vm = new PatientAppointmentsIndexViewModel
//            {
//                Filter = normalized,
//                Appointments = list,
//                FromCard = fromCard
//            };

//            return View(vm);
//        }



//        // POST: /PatientAppointments/Cancel
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Cancel(int id, string? filter = "")
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

//            var patientProfileId = await _context.PatientProfiles
//                .Where(p => p.UserId == userId && p.IsActive)
//                .Select(p => (int?)p.Id)
//                .FirstOrDefaultAsync();

//            if (patientProfileId == null)
//            {
//                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
//                return RedirectToAction(nameof(Index), new { filter });
//            }

//            var appt = await _context.Appointments
//                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.PatientProfileId == patientProfileId);

//            if (appt == null)
//            {
//                TempData["ErrorMessage"] = "Appointment not found.";
//                return RedirectToAction(nameof(Index), new { filter });
//            }

//            var alreadyPaid = await _context.Payments.AnyAsync(p =>
//                p.IsActive && p.AppointmentId == id && p.Status == PaymentStatus.Paid);

//            if (alreadyPaid)
//            {
//                TempData["ErrorMessage"] = "Cannot cancel after payment is completed.";
//                return RedirectToAction(nameof(Index), new { filter });
//            }

//            // This releases the slot because GetAvailableSlots ignores Cancelled
//            appt.Status = AppointmentStatus.Cancelled;
//            appt.UpdatedAt = DateTime.UtcNow;

//            await _context.SaveChangesAsync();

//            TempData["SuccessMessage"] = "Appointment cancelled successfully. The slot is now available.";
//            return RedirectToAction(nameof(Index), new { filter });
//        }

//        // POST: /PatientAppointments/Pay
//        // Option B: No DB payment created now. Just show message.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Pay(int id, string? filter = "")
//        {
//            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
//            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

//            var patientProfileId = await _context.PatientProfiles
//                .Where(p => p.UserId == userId && p.IsActive)
//                .Select(p => (int?)p.Id)
//                .FirstOrDefaultAsync();

//            if (patientProfileId == null)
//            {
//                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
//                return RedirectToAction(nameof(Index), new { filter });
//            }

//            var appt = await _context.Appointments
//                .AsNoTracking()
//                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.PatientProfileId == patientProfileId);

//            if (appt == null)
//            {
//                TempData["ErrorMessage"] = "Appointment not found.";
//                return RedirectToAction(nameof(Index), new { filter });
//            }

//            if (appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.NoShow || appt.Status == AppointmentStatus.Completed)
//            {
//                TempData["ErrorMessage"] = "Payment is not available for this appointment.";
//                return RedirectToAction(nameof(Index), new { filter });
//            }

//            var alreadyPaid = await _context.Payments.AnyAsync(p =>
//                p.IsActive && p.AppointmentId == id && p.Status == PaymentStatus.Paid);

//            if (alreadyPaid)
//            {
//                TempData["SuccessMessage"] = "This appointment is already paid.";
//                return RedirectToAction(nameof(Index), new { filter });
//            }

//            // Option B: No pending record created now
//            TempData["InfoMessage"] = "bKash payment will be added soon. Please pay later.";
//            return RedirectToAction(nameof(Index), new { filter });
//        }

//    }
//}


using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.ViewModels;
using Doctor_AppointmentSystem.Services;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // ✅ NEW: SSLCOMMERZ service + settings
        private readonly ISslCommerzService _ssl;
        private readonly SslCommerzSettings _sslSettings;

        public PatientAppointmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISslCommerzService ssl,
            IOptions<SslCommerzSettings> sslSettings)
        {
            _context = context;
            _userManager = userManager;
            _ssl = ssl;
            _sslSettings = sslSettings.Value;
        }

        // GET: /PatientAppointments?filter=upcoming|completed|cancelled|payments
        public async Task<IActionResult> Index(string? filter, bool fromCard = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var patientProfile = await _context.PatientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);

            if (patientProfile == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
                return RedirectToAction("Index", "PatientDashboard");
            }

            var normalized = (filter ?? "").Trim().ToLowerInvariant();
            var now = DateTime.Now;

            IQueryable<Appointment> baseQuery = _context.Appointments
                .AsNoTracking()
                .Where(a => a.PatientProfileId == patientProfile.Id && a.IsActive);

            switch (normalized)
            {
                case "upcoming":
                    baseQuery = baseQuery.Where(a =>
                        a.AppointmentDateTime >= now &&
                        (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Rescheduled));
                    break;

                case "completed":
                    baseQuery = baseQuery.Where(a => a.Status == AppointmentStatus.Completed);
                    break;

                case "cancelled":
                    baseQuery = baseQuery.Where(a =>
                        a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow);
                    break;

                // ✅ payments mode
                case "payments":
                    break;

                default:
                    normalized = "";
                    break;
            }

            var list = await baseQuery
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new PatientAppointmentListItemViewModel
                {
                    Id = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DurationMinutes = a.DurationMinutes,
                    VisitType = a.VisitType,
                    Status = a.Status,

                    DoctorName = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => ((d.User.FirstName ?? "") + " " + (d.User.LastName ?? "")).Trim())
                        .FirstOrDefault() ?? "",

                    SpecialtyName = _context.DoctorSpecialties
                        .Where(ds => ds.DoctorProfileId == a.DoctorProfileId)
                        .Select(ds => ds.Specialty.Name)
                        .FirstOrDefault(),

                    FeeAmount = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => d.VisitCharge)
                        .FirstOrDefault(),

                    PaymentDisplay = "Unpaid",
                    Amount = 0m,
                    IsPaid = false,
                    PaidPaymentId = null,

                    CanShowActions = false,
                    CanPay = false,
                    CanCancel = false
                })
                .ToListAsync();

            var ids = list.Select(x => x.Id).ToList();

            var latestPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive && ids.Contains(p.AppointmentId))
                .GroupBy(p => p.AppointmentId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).FirstOrDefault())
                .ToListAsync();

            var payMap = latestPayments
                .Where(p => p != null)
                .ToDictionary(p => p!.AppointmentId, p => p!);

            foreach (var row in list)
            {
                var isCancelled = row.Status == AppointmentStatus.Cancelled
                                  || row.Status == AppointmentStatus.NoShow;

                var isCompleted = row.Status == AppointmentStatus.Completed;
                var isFuture = row.AppointmentDateTime >= DateTime.Now;

                if (payMap.TryGetValue(row.Id, out var pay))
                {
                    if (pay.Status == PaymentStatus.Paid)
                    {
                        row.PaymentDisplay = $"Paid ({pay.Method})";
                        row.IsPaid = true;
                        row.Amount = pay.Amount;
                        row.PaidPaymentId = pay.Id;
                    }
                    else
                    {
                        row.PaymentDisplay = "Unpaid";
                        row.IsPaid = false;
                        row.Amount = row.FeeAmount;
                        row.PaidPaymentId = null;
                    }
                }
                else
                {
                    row.PaymentDisplay = "Unpaid";
                    row.Amount = row.FeeAmount;
                    row.IsPaid = false;
                    row.PaidPaymentId = null;
                }

                var isConfirmLike =
                    row.Status == AppointmentStatus.Confirmed
                    || row.Status == AppointmentStatus.Rescheduled;

                row.CanCancel =
                    isFuture
                    && isConfirmLike
                    && !isCancelled
                    && !isCompleted
                    && !row.IsPaid;

                row.CanPay =
                    isFuture
                    && isConfirmLike
                    && !isCancelled
                    && !isCompleted
                    && !row.IsPaid;

                row.CanShowActions =
                    row.CanCancel
                    || row.CanPay
                    || row.IsPaid;
            }

            if (normalized == "payments")
            {
                list = list
                    .Where(x => x.IsPaid && x.PaidPaymentId.HasValue)
                    .OrderByDescending(x => x.AppointmentDateTime)
                    .ToList();
            }

            var vm = new PatientAppointmentsIndexViewModel
            {
                Filter = normalized,
                Appointments = list,
                FromCard = fromCard
            };

            return View(vm);
        }

        // POST: /PatientAppointments/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? filter = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var patientProfileId = await _context.PatientProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (patientProfileId == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var appt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.PatientProfileId == patientProfileId);

            if (appt == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var alreadyPaid = await _context.Payments.AnyAsync(p =>
                p.IsActive && p.AppointmentId == id && p.Status == PaymentStatus.Paid);

            if (alreadyPaid)
            {
                TempData["ErrorMessage"] = "Cannot cancel after payment is completed.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            appt.Status = AppointmentStatus.Cancelled;
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment cancelled successfully. The slot is now available.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // ✅ UPDATED: POST /PatientAppointments/Pay → redirects to SSLCOMMERZ hosted UI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id, string? filter = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            // Load patient profile + user (needed for customer info)
            var patientProfile = await _context.PatientProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive);

            if (patientProfile == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found or inactive.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            // Load appointment (must belong to this patient)
            var appt = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.PatientProfileId == patientProfile.Id);

            if (appt == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            if (appt.Status == AppointmentStatus.Cancelled ||
                appt.Status == AppointmentStatus.NoShow ||
                appt.Status == AppointmentStatus.Completed)
            {
                TempData["ErrorMessage"] = "Payment is not available for this appointment.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            // Block double payment
            var alreadyPaid = await _context.Payments.AnyAsync(p =>
                p.IsActive && p.AppointmentId == id && p.Status == PaymentStatus.Paid);

            if (alreadyPaid)
            {
                TempData["SuccessMessage"] = "This appointment is already paid.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            // Amount = Doctor's declared VisitCharge
            var amount = await _context.DoctorProfiles
                .Where(d => d.Id == appt.DoctorProfileId && d.IsActive)
                .Select(d => d.VisitCharge)
                .FirstOrDefaultAsync();

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Doctor visiting charge is not set properly.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            // Create Pending Payment record
            var tranId = $"DAS-APPT-{id}-{DateTime.UtcNow.Ticks}";

            var payment = new Payment
            {
                AppointmentId = id,
                Amount = amount,
                Currency = "BDT",
                Status = PaymentStatus.Pending,
                Method = PaymentMethod.SslCommerz,
                GatewayTransactionId = tranId,
                ProviderName = "SSLCOMMERZ",
                InitiatedByUserId = userId,
                StatusLastUpdatedUtc = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Prepare gateway init fields
            var callbackBase = (_sslSettings.CallbackBaseUrl ?? "http://localhost:5243").TrimEnd('/');

            var cusName = $"{patientProfile.User?.FirstName ?? ""} {patientProfile.User?.LastName ?? ""}".Trim();
            if (string.IsNullOrWhiteSpace(cusName)) cusName = "Patient";

            var fields = new Dictionary<string, string>
            {
                ["store_id"] = _sslSettings.StoreId,
                ["store_passwd"] = _sslSettings.StorePassword,
                ["total_amount"] = amount.ToString("0.00"),
                ["currency"] = "BDT",
                ["tran_id"] = tranId,

                ["success_url"] = $"{callbackBase}/sslcommerz/success",
                ["fail_url"] = $"{callbackBase}/sslcommerz/fail",
                ["cancel_url"] = $"{callbackBase}/sslcommerz/cancel",
                ["ipn_url"] = $"{callbackBase}/sslcommerz/ipn",

                ["product_name"] = "Doctor Appointment",
                ["product_category"] = "Healthcare",
                ["product_profile"] = "general",

                ["cus_name"] = cusName,
                ["cus_email"] = patientProfile.User?.Email ?? "test@email.com",
                ["cus_add1"] = "N/A",
                ["cus_add2"] = "N/A",
                ["cus_city"] = "Dhaka",
                ["cus_state"] = "Dhaka",
                ["cus_postcode"] = "0000",
                ["cus_country"] = "Bangladesh",
                ["cus_phone"] = patientProfile.User?.PhoneNumber ?? "01700000000",

                ["shipping_method"] = "NO",
                ["num_of_item"] = "1"
            };

            var init = await _ssl.InitPaymentAsync(fields);

            if (!string.Equals(init.status, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(init.GatewayPageURL))
            {
                // Mark failed if gateway init fails
                payment.Status = PaymentStatus.Failed;
                payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = $"SSLCOMMERZ init failed: {init.failedreason ?? "Unknown error"}";
                return RedirectToAction(nameof(Index), new { filter });
            }

            // Redirect to SSLCOMMERZ hosted UI (matches your screenshot)
            return Redirect(init.GatewayPageURL);
        }
    }
}
