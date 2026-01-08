
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Helpers;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.Services;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _email;

        public ReceptionistAppointmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService email)
        {
            _context = context;
            _userManager = userManager;
            _email = email;
        }

        // GET: /ReceptionistAppointments?filter=all|today|upcoming|cancelled|paidtoday|paidall&fromCard=true
        [HttpGet]
        public async Task<IActionResult> Index(string filter = "all", bool fromCard = false)
        {
            var receptionistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(receptionistUserId))
                return Challenge();

            filter = (filter ?? "all").Trim().ToLowerInvariant();

            // ==========================
            // OPTION A: "Today" = Booked today (CreatedAt)
            // ==========================
            var localTodayStart = DateTime.Today;
            var localTomorrowStart = localTodayStart.AddDays(1);

            // CreatedAt is typically stored as UTC (your models default CreatedAt = DateTime.UtcNow)
            var todayStartUtc = DateTime.SpecifyKind(localTodayStart, DateTimeKind.Local).ToUniversalTime();
            var tomorrowStartUtc = DateTime.SpecifyKind(localTomorrowStart, DateTimeKind.Local).ToUniversalTime();

            // ==========================
            // Base appointment query (only appointments booked by this receptionist)
            // ==========================
            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.IsActive && a.BookedByUserId == receptionistUserId);

            // ==========================
            // Apply appointment-level filters
            // ==========================
            switch (filter)
            {
                case "today":
                    // ✅ Booked today (CreatedAt), exclude cancelled
                    query = query.Where(a =>
                        a.CreatedAt >= todayStartUtc &&
                        a.CreatedAt < tomorrowStartUtc &&
                        a.Status != AppointmentStatus.Cancelled);
                    break;

                case "upcoming":
                    // scheduled in the future, not cancelled
                    query = query.Where(a =>
                        a.AppointmentDateTime.Date > localTodayStart &&
                        a.Status != AppointmentStatus.Cancelled);
                    break;

                case "cancelled":
                case "canceled":
                    query = query.Where(a => a.Status == AppointmentStatus.Cancelled);
                    break;

                case "paidtoday":
                case "paidall":
                    // handled after payment map is loaded (we need latest payment per appointment)
                    break;

                default:
                    // all
                    break;
            }

            // ==========================
            // Project to rows (payments filled later)
            // ==========================
            var appointments = await query
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new ReceptionistAppointmentRowViewModel
                {
                    AppointmentId = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status.ToString(),

                    DoctorName = "Dr. " + (a.Doctor.User.FirstName + " " + a.Doctor.User.LastName).Trim(),

                    PatientName =
                        a.PatientProfileId != null
                            ? (a.Patient.User.FirstName + " " + a.Patient.User.LastName).Trim()
                            : (a.UnregisteredPatientName ?? "Unregistered Patient").Trim(),

                    // defaults
                    IsPaid = false,
                    PaymentId = null,
                    PaymentDisplay = "Unpaid",
                    Amount = 0m,

                    CanShowActions = false,
                    CanCollectCash = false,
                    CanConfirmMobile = false,
                    CanCancel = false
                })
                .ToListAsync();

            // No rows → build vm quickly
            if (appointments.Count == 0)
            {
                ViewData["FromCard"] = fromCard;
                ViewData["CardTitle"] = filter switch
                {
                    "all" => "My Total Booked",
                    "today" => "Today's Booked",
                    "upcoming" => "Upcoming Appointments",
                    "cancelled" => "Cancelled Appointments",
                    "paidtoday" => "Today's Collection",
                    "paidall" => "My Total Collection",
                    _ => "Appointments"
                };

                return View(new ReceptionistAppointmentViewModel
                {
                    Filter = filter,
                    Appointments = appointments
                });
            }

            // ==========================
            // Load latest payment per appointment (if any)
            // ==========================
            var ids = appointments.Select(x => x.AppointmentId).ToList();

            var latestPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive && ids.Contains(p.AppointmentId))
                .GroupBy(p => p.AppointmentId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).FirstOrDefault())
                .ToListAsync();

            var payMap = latestPayments
                .Where(p => p != null)
                .ToDictionary(p => p!.AppointmentId, p => p!);

            // ==========================
            // Attach payment state + actions
            // ==========================
            foreach (var row in appointments)
            {
                var isCancelled = row.Status.Equals(AppointmentStatus.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase);
                if (isCancelled)
                {
                    row.CanShowActions = false;
                    row.IsPaid = false;
                    row.PaymentId = null;
                    row.PaymentDisplay = "Cancelled";
                    continue;
                }

                row.PaymentDisplay = "Unpaid";
                row.Amount = 0m;
                row.IsPaid = false;
                row.PaymentId = null;

                bool isPaid = false;

                if (payMap.TryGetValue(row.AppointmentId, out var pay))
                {
                    row.Amount = pay.Amount;

                    if (pay.Status == PaymentStatus.Paid)
                    {
                        isPaid = true;
                        row.IsPaid = true;
                        row.PaymentId = pay.Id;

                        var methodText = pay.Method switch
                        {
                            PaymentMethod.Cash => "Cash",
                            PaymentMethod.Bkash => "bKash",
                            PaymentMethod.Nagad => "Nagad",
                            PaymentMethod.Rocket => "Rocket",
                            _ => pay.Method.ToString()
                        };

                        row.PaymentDisplay = $"Paid ({methodText})";
                    }
                    else
                    {
                        row.PaymentDisplay = pay.Status.ToString();
                    }
                }

                if (!isPaid)
                {
                    // Unpaid → show actions
                    row.CanShowActions = true;
                    row.CanCollectCash = true;
                    row.CanConfirmMobile = true;
                    row.CanCancel = true;
                }
                else
                {
                    // Paid → hide payment actions (view will show Download Receipt)
                    row.CanShowActions = false;
                }
            }

            // ==========================
            // Apply payment-driven filters (paidtoday / paidall)
            // ==========================
            if (filter == "paidall" || filter == "paidtoday")
            {
                appointments = appointments
                    .Where(a => a.IsPaid && a.PaymentId.HasValue)
                    .ToList();

                if (filter == "paidtoday")
                {
                    // ✅ collected today by THIS receptionist
                    var paidTodayPaymentIds = latestPayments
                        .Where(p =>
                            p != null
                            && p.Status == PaymentStatus.Paid
                            && p.InitiatedByUserId == receptionistUserId
                            && p.PaidAtUtc.HasValue
                            && p.PaidAtUtc.Value >= todayStartUtc
                            && p.PaidAtUtc.Value < tomorrowStartUtc)
                        .Select(p => p!.Id)
                        .ToHashSet();

                    appointments = appointments
                        .Where(a => a.PaymentId.HasValue && paidTodayPaymentIds.Contains(a.PaymentId.Value))
                        .ToList();
                }
            }

            // ==========================
            // Build VM + Card title flags
            // ==========================
            var vm = new ReceptionistAppointmentViewModel
            {
                Filter = filter,
                Appointments = appointments
            };

            ViewData["FromCard"] = fromCard;
            ViewData["CardTitle"] = filter switch
            {
                "all" => "My Total Booked",
                "today" => "Today's Booked",
                "upcoming" => "Upcoming Appointments",
                "cancelled" => "Cancelled Appointments",
                "paidtoday" => "Today's Collection",
                "paidall" => "My Total Collection",
                _ => "Appointments"
            };

            return View(vm);
        }

        // POST: /ReceptionistAppointments/CollectCash
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectCash(int id, string? filter = "all")
        {
            var receptionistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.BookedByUserId == receptionistUserId);

            if (appt == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            if (appt.Status == AppointmentStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cancelled appointment cannot be paid.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var alreadyPaid = await _context.Payments.AnyAsync(p =>
                p.IsActive && p.AppointmentId == id && p.Status == PaymentStatus.Paid);

            if (alreadyPaid)
            {
                TempData["ErrorMessage"] = "This appointment is already paid.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var fee = await _context.DoctorProfiles
                .Where(d => d.Id == appt.DoctorProfileId)
                .Select(d => d.VisitCharge)
                .FirstOrDefaultAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newPayment = new Payment
            {
                AppointmentId = appt.Id,
                Amount = fee,
                Currency = "BDT",
                Status = PaymentStatus.Paid,
                Method = PaymentMethod.Cash,
                ProviderName = "Cash",
                GatewayTransactionId = null,
                PaidAtUtc = DateTime.UtcNow,
                StatusLastUpdatedUtc = DateTime.UtcNow,
                InitiatedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(newPayment);
            await _context.SaveChangesAsync();

            // ✅ Case-2 emails: Doctor + Receptionist
            await TrySendReceptionistPaidEmailsAsync(appt.Id, methodLabel: "Cash");

            TempData["SuccessMessage"] = "Cash payment collected successfully.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: /ReceptionistAppointments/ConfirmMobile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMobile(int id, string provider, string transactionId, string? filter = "all")
        {
            var receptionistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.BookedByUserId == receptionistUserId);

            if (appt == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            if (appt.Status == AppointmentStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cancelled appointment cannot be paid.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var alreadyPaid = await _context.Payments.AnyAsync(p =>
                p.IsActive && p.AppointmentId == id && p.Status == PaymentStatus.Paid);

            if (alreadyPaid)
            {
                TempData["ErrorMessage"] = "This appointment is already paid.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            provider = (provider ?? "").Trim();
            transactionId = (transactionId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(provider))
            {
                TempData["ErrorMessage"] = "Please select a provider (bKash/Nagad/Rocket).";
                return RedirectToAction(nameof(Index), new { filter });
            }

            if (string.IsNullOrWhiteSpace(transactionId))
            {
                TempData["ErrorMessage"] = "Transaction ID is required for mobile payment.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            PaymentMethod method = provider.ToLower() switch
            {
                "bkash" => PaymentMethod.Bkash,
                "nagad" => PaymentMethod.Nagad,
                "rocket" => PaymentMethod.Rocket,
                _ => PaymentMethod.Bkash
            };

            var fee = await _context.DoctorProfiles
                .Where(d => d.Id == appt.DoctorProfileId)
                .Select(d => d.VisitCharge)
                .FirstOrDefaultAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newPayment = new Payment
            {
                AppointmentId = appt.Id,
                Amount = fee,
                Currency = "BDT",
                Status = PaymentStatus.Paid,
                Method = method,
                ProviderName = provider,
                GatewayTransactionId = transactionId,
                PaidAtUtc = DateTime.UtcNow,
                StatusLastUpdatedUtc = DateTime.UtcNow,
                InitiatedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(newPayment);
            await _context.SaveChangesAsync();

            // ✅ Case-2 emails: Doctor + Receptionist
            await TrySendReceptionistPaidEmailsAsync(appt.Id, methodLabel: provider);

            TempData["SuccessMessage"] = "Mobile banking payment confirmed.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: /ReceptionistAppointments/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? filter = "all")
        {
            var receptionistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.BookedByUserId == receptionistUserId);

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

            TempData["SuccessMessage"] = "Appointment cancelled successfully.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // ==========================================================
        // EMAIL SENDER (Case 2): Doctor + Receptionist after Paid
        // ==========================================================
        private async Task TrySendReceptionistPaidEmailsAsync(int appointmentId, string methodLabel)
        {
            try
            {
                var apptFull = await _context.Appointments
                    .Include(a => a.Doctor).ThenInclude(d => d.User)
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.IsActive);

                if (apptFull == null)
                    return;

                // Doctor
                var doctorUser = apptFull.Doctor?.User;
                var doctorName = doctorUser == null ? "Doctor" : $"Dr. {doctorUser.FirstName} {doctorUser.LastName}".Trim();
                var doctorEmail = doctorUser?.Email;

                // Receptionist
                var receptionistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ApplicationUser? receptionistUser = null;

                if (!string.IsNullOrWhiteSpace(receptionistUserId))
                    receptionistUser = await _userManager.FindByIdAsync(receptionistUserId);

                var receptionistName = receptionistUser == null
                    ? "Receptionist"
                    : $"{receptionistUser.FirstName} {receptionistUser.LastName}".Trim();

                var receptionistEmail = receptionistUser?.Email;

                // Patient
                string patientName =
                    apptFull.PatientProfileId != null
                        ? (apptFull.Patient.User.FirstName + " " + apptFull.Patient.User.LastName).Trim()
                        : (apptFull.UnregisteredPatientName ?? "Unregistered Patient").Trim();

                var apptTime = apptFull.AppointmentDateTime;

                // Email to Doctor
                if (!string.IsNullOrWhiteSpace(doctorEmail))
                {
                    var subject = "Appointment Confirmed (Paid by Receptionist)";
                    var body = EmailTemplates.AppointmentPaidDoctor(
                        doctorName.Replace("Dr. ", "").Trim(),
                        patientName,
                        apptTime,
                        methodLabel);

                    await _email.SendAsync(doctorEmail, subject, body);
                }

                // Email to Receptionist
                if (!string.IsNullOrWhiteSpace(receptionistEmail))
                {
                    var subject = "Payment Confirmation Recorded";
                    var body = EmailTemplates.AppointmentPaidReceptionist(
                        receptionistName,
                        patientName,
                        doctorName,
                        apptTime,
                        methodLabel);

                    await _email.SendAsync(receptionistEmail, subject, body);
                }
            }
            catch
            {
                // Keep receptionist flow smooth even if email fails
            }
        }
    }
}
