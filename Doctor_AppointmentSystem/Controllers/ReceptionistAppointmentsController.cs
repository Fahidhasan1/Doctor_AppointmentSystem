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

            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.IsActive && a.BookedByUserId == receptionistUserId);

            switch ((filter ?? "all").Trim().ToLower())
            {
                case "today":
                    query = query.Where(a => a.AppointmentDateTime.Date == today &&
                                             a.Status != AppointmentStatus.Cancelled);
                    break;

                case "upcoming":
                    query = query.Where(a => a.AppointmentDateTime.Date > today &&
                                             a.Status != AppointmentStatus.Cancelled);
                    break;

                case "cancelled":
                case "canceled":
                    query = query.Where(a => a.Status == AppointmentStatus.Cancelled);
                    break;

                default:
                    break;
            }

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

                    // defaults (filled after payment lookup)
                    PaymentDisplay = "Unpaid",
                    Amount = 0m,

                    CanShowActions = false,
                    CanCollectCash = false,
                    CanConfirmMobile = false,
                    CanCancel = false
                })
                .ToListAsync();

            // Load latest payment per appointment (if any)
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

            foreach (var row in appointments)
            {
                // Cancelled -> no actions
                var isCancelled = row.Status.Equals(AppointmentStatus.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase);
                if (isCancelled)
                {
                    row.CanShowActions = false;
                    continue;
                }

                // default unpaid behavior
                row.PaymentDisplay = "Unpaid";
                row.Amount = 0m;

                bool isPaid = false;

                if (payMap.TryGetValue(row.AppointmentId, out var pay))
                {
                    row.Amount = pay.Amount;

                    if (pay.Status == PaymentStatus.Paid)
                    {
                        isPaid = true;

                        row.PaymentDisplay = (pay.Method == PaymentMethod.Cash)
                            ? "Paid (Cash)"
                            : "Paid (Mobile Banking)";
                    }
                }

                // ✅ Actions show ONLY if NOT paid and NOT cancelled
                if (!isPaid)
                {
                    row.CanShowActions = true;
                    row.CanCollectCash = true;
                    row.CanConfirmMobile = true;
                    row.CanCancel = true;
                }
                else
                {
                    row.CanShowActions = false;
                }
            }

            var vm = new ReceptionistAppointmentViewModel
            {
                Filter = (filter ?? "all").ToLower(),
                Appointments = appointments
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

            _context.Payments.Add(new Payment
            {
                AppointmentId = appt.Id,
                Amount = fee,
                Currency = "BDT",
                Status = PaymentStatus.Paid,
                Method = PaymentMethod.Cash,
                PaidAtUtc = DateTime.UtcNow,
                StatusLastUpdatedUtc = DateTime.UtcNow,
                InitiatedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cash payment collected successfully.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: /ReceptionistAppointments/ConfirmMobile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMobile(int id, string? filter = "all")
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

            // ✅ Use your existing enum value (you used Bkash already)
            // UI will still show "Mobile Banking"
            _context.Payments.Add(new Payment
            {
                AppointmentId = appt.Id,
                Amount = fee,
                Currency = "BDT",
                Status = PaymentStatus.Paid,
                Method = PaymentMethod.Bkash,
                PaidAtUtc = DateTime.UtcNow,
                StatusLastUpdatedUtc = DateTime.UtcNow,
                InitiatedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

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
    }
}
