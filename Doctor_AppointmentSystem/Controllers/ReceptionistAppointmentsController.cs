using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReceptionistAppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ReceptionistAppointments?filter=all|today|upcoming|cancelled
        [HttpGet]
        public async Task<IActionResult> Index(string? filter = "all")
        {
            var receptionistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            filter = (filter ?? "all").Trim().ToLower();

            // Base appointment query (only booked by this receptionist)
            var apptQuery = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.IsActive && a.BookedByUserId == receptionistUserId);

            // Apply filter
            switch (filter)
            {
                case "today":
                    apptQuery = apptQuery.Where(a =>
                        a.AppointmentDateTime >= today &&
                        a.AppointmentDateTime < tomorrow &&
                        a.Status != AppointmentStatus.Cancelled);
                    break;

                case "upcoming":
                    apptQuery = apptQuery.Where(a =>
                        a.AppointmentDateTime >= tomorrow &&
                        a.Status != AppointmentStatus.Cancelled);
                    break;

                case "cancelled":
                case "canceled":
                    apptQuery = apptQuery.Where(a => a.Status == AppointmentStatus.Cancelled);
                    filter = "cancelled";
                    break;

                default:
                    filter = "all";
                    break;
            }

            var appointments = await apptQuery
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            var appointmentIds = appointments.Select(a => a.Id).ToList();

            // Get latest payment per appointment (Payments table)
            var latestPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.IsActive && appointmentIds.Contains(p.AppointmentId))
                .GroupBy(p => p.AppointmentId)
                .Select(g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault())
                .ToListAsync();

            var paymentsByAppointmentId = latestPayments
                .Where(p => p != null)
                .ToDictionary(p => p!.AppointmentId, p => p!);

            var vm = new ReceptionistAppointmentViewModel
            {
                Filter = filter!,
                Appointments = appointments.Select(a =>
                {
                    paymentsByAppointmentId.TryGetValue(a.Id, out var pay);

                    var isPaid = pay != null && pay.Status == PaymentStatus.Paid;

                    // UI display: show "Mobile Banking" instead of "bKash"
                    string paymentSummary;
                    if (!isPaid)
                    {
                        paymentSummary = "Unpaid";
                    }
                    else
                    {
                        var methodText = pay!.Method == PaymentMethod.Cash
                            ? "Cash"
                            : "Mobile Banking";

                        paymentSummary = $"Paid ({methodText}) ৳{pay.Amount:0}";
                    }

                    // Patient name logic (registered OR unregistered)
                    var patientName =
                        a.PatientProfileId != null && a.Patient != null && a.Patient.User != null
                            ? (a.Patient.User.FirstName + " " + a.Patient.User.LastName).Trim()
                            : (a.UnregisteredPatientName ?? "Unregistered Patient").Trim();

                    // actions:
                    // - if cancelled => no actions
                    // - if paid => no cancel, no collect/confirm
                    // - if unpaid & not cancelled => allow collect cash / confirm mobile banking / cancel
                    var isCancelled = a.Status == AppointmentStatus.Cancelled;

                    var canCollectCash = !isCancelled && !isPaid;
                    var canConfirmMobile = !isCancelled && !isPaid;
                    var canCancel = !isCancelled && !isPaid;

                    return new ReceptionistAppointmentRowViewModel
                    {
                        AppointmentId = a.Id,
                        AppointmentDateTime = a.AppointmentDateTime,
                        DurationMinutes = a.DurationMinutes,

                        DoctorName = ("Dr. " + (a.Doctor.User.FirstName + " " + a.Doctor.User.LastName)).Trim(),
                        PatientName = patientName,

                        StatusText = a.Status.ToString(),
                        IsPaid = isPaid,
                        PaidAtUtc = pay?.PaidAtUtc,
                        PaymentSummary = paymentSummary,

                        CanCollectCash = canCollectCash,
                        CanConfirmMobileBanking = canConfirmMobile,
                        CanCancel = canCancel,
                        CanShowActions = canCollectCash || canConfirmMobile || canCancel
                    };
                }).ToList()
            };

            return View(vm);
        }

        // POST: /ReceptionistAppointments/CollectCash/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectCash(int id, string? filter = "all")
        {
            var receptionistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appt = await _context.Appointments
                .Include(a => a.Doctor)
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
                p.IsActive && p.AppointmentId == appt.Id && p.Status == PaymentStatus.Paid);

            if (alreadyPaid)
            {
                TempData["ErrorMessage"] = "Payment already completed.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            // Amount from doctor profile
            var doctorProfile = await _context.DoctorProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == appt.DoctorProfileId && d.IsActive);

            if (doctorProfile == null)
            {
                TempData["ErrorMessage"] = "Doctor profile not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var amount = appt.IsFirstVisit
                ? doctorProfile.VisitCharge
                : (doctorProfile.FollowUpCharge ?? doctorProfile.VisitCharge);

            _context.Payments.Add(new Payment
            {
                AppointmentId = appt.Id,
                Amount = amount,
                Currency = "BDT",
                Status = PaymentStatus.Paid,
                Method = PaymentMethod.Cash,

                PaidAtUtc = DateTime.UtcNow,
                StatusLastUpdatedUtc = DateTime.UtcNow,

                InitiatedByUserId = receptionistUserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cash payment received successfully.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: /ReceptionistAppointments/ConfirmMobileBanking/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMobileBanking(int id, string? filter = "all")
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
                p.IsActive && p.AppointmentId == appt.Id && p.Status == PaymentStatus.Paid);

            if (alreadyPaid)
            {
                TempData["ErrorMessage"] = "Payment already completed.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var doctorProfile = await _context.DoctorProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == appt.DoctorProfileId && d.IsActive);

            if (doctorProfile == null)
            {
                TempData["ErrorMessage"] = "Doctor profile not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var amount = appt.IsFirstVisit
                ? doctorProfile.VisitCharge
                : (doctorProfile.FollowUpCharge ?? doctorProfile.VisitCharge);

            // IMPORTANT:
            // We store Method = Bkash in DB (because enum already exists),
            // but UI will display it as "Mobile Banking".
            _context.Payments.Add(new Payment
            {
                AppointmentId = appt.Id,
                Amount = amount,
                Currency = "BDT",
                Status = PaymentStatus.Paid,
                Method = PaymentMethod.Bkash,

                PaidAtUtc = DateTime.UtcNow,
                StatusLastUpdatedUtc = DateTime.UtcNow,

                InitiatedByUserId = receptionistUserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mobile banking payment confirmed.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: /ReceptionistAppointments/Cancel/5
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
                p.IsActive && p.AppointmentId == appt.Id && p.Status == PaymentStatus.Paid);

            if (alreadyPaid)
            {
                TempData["ErrorMessage"] = "Cannot cancel after payment is completed.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            if (appt.Status == AppointmentStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Appointment already cancelled.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            appt.Status = AppointmentStatus.Cancelled;
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Slot becomes available again automatically because your slot query excludes Cancelled.
            TempData["SuccessMessage"] = "Appointment cancelled successfully.";
            return RedirectToAction(nameof(Index), new { filter });
        }
    }
}
