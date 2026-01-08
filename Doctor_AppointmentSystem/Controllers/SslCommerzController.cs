
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Helpers;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [AllowAnonymous]
    public class SslCommerzController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISslCommerzService _ssl;
        private readonly IEmailService _email;

        public SslCommerzController(ApplicationDbContext context, ISslCommerzService ssl, IEmailService email)
        {
            _context = context;
            _ssl = ssl;
            _email = email;
        }

        // =========================
        // SUCCESS CALLBACK
        // =========================
        [HttpPost]
        [Route("sslcommerz/success")]
        public async Task<IActionResult> Success()
        {
            var tranId = Request.Form["tran_id"].ToString();
            var valId = Request.Form["val_id"].ToString();

            if (string.IsNullOrWhiteSpace(tranId) || string.IsNullOrWhiteSpace(valId))
            {
                TempData["ErrorMessage"] = "Invalid payment response received.";
                return RedirectToAction("Index", "PatientAppointments");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.IsActive && p.GatewayTransactionId == tranId);

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment record not found.";
                return RedirectToAction("Index", "PatientAppointments");
            }

            // ✅ Prevent double-processing
            if (payment.Status == PaymentStatus.Paid)
            {
                TempData["SuccessMessage"] = "Payment already completed.";
                return RedirectToAction("Index", "PatientAppointments");
            }

            var validation = await _ssl.ValidateAsync(valId);

            if (!string.Equals(validation.status, "VALID", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = PaymentStatus.Failed;
                payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "Payment validation failed.";
                return RedirectToAction("Index", "PatientAppointments");
            }

            // Extra safety: amount check
            if (decimal.TryParse(validation.amount, out var paidAmount))
            {
                if (paidAmount != payment.Amount)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] = "Payment amount mismatch.";
                    return RedirectToAction("Index", "PatientAppointments");
                }
            }

            // ✅ Mark paid
            payment.Status = PaymentStatus.Paid;
            payment.PaidAtUtc = DateTime.UtcNow;
            payment.StatusLastUpdatedUtc = DateTime.UtcNow;
            payment.ProviderName = "SSLCOMMERZ";
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ✅ Send emails (Patient + Doctor) for Case-1 flow
            await TrySendPaidEmailsAsync(payment, paidVia: "SSLCOMMERZ");

            TempData["SuccessMessage"] = "Payment successful!";
            return RedirectToAction("Index", "PatientAppointments");
        }

        // =========================
        // FAIL CALLBACK
        // =========================
        [HttpPost]
        [Route("sslcommerz/fail")]
        public async Task<IActionResult> Fail()
        {
            var tranId = Request.Form["tran_id"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.IsActive && p.GatewayTransactionId == tranId);

                if (payment != null && payment.Status != PaymentStatus.Paid)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["ErrorMessage"] = "Payment failed.";
            return RedirectToAction("Index", "PatientAppointments");
        }

        // =========================
        // CANCEL CALLBACK
        // =========================
        [HttpPost]
        [Route("sslcommerz/cancel")]
        public async Task<IActionResult> Cancel()
        {
            var tranId = Request.Form["tran_id"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.IsActive && p.GatewayTransactionId == tranId);

                if (payment != null && payment.Status != PaymentStatus.Paid)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["InfoMessage"] = "Payment cancelled.";
            return RedirectToAction("Index", "PatientAppointments");
        }

        // =========================
        // IPN CALLBACK (SERVER TO SERVER)
        // =========================
        [HttpPost]
        [Route("sslcommerz/ipn")]
        public async Task<IActionResult> Ipn()
        {
            var tranId = Request.Form["tran_id"].ToString();
            var valId = Request.Form["val_id"].ToString();

            if (string.IsNullOrWhiteSpace(tranId) || string.IsNullOrWhiteSpace(valId))
                return Ok();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.IsActive && p.GatewayTransactionId == tranId);

            if (payment == null)
                return Ok();

            // ✅ Prevent duplicate processing + duplicate emails
            if (payment.Status == PaymentStatus.Paid)
                return Ok();

            var validation = await _ssl.ValidateAsync(valId);

            if (string.Equals(validation.status, "VALID", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = PaymentStatus.Paid;
                payment.PaidAtUtc = DateTime.UtcNow;
                payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                payment.ProviderName = "SSLCOMMERZ";
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // ✅ Send emails even if IPN marks Paid first
                await TrySendPaidEmailsAsync(payment, paidVia: "SSLCOMMERZ");
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        // ==========================================================
        // EMAIL SENDER (Case 1): Patient + Doctor after Paid
        // ==========================================================
        private async Task TrySendPaidEmailsAsync(Payment payment, string paidVia)
        {
            try
            {
                // Load appointment + doctor + patient info
                var appt = await _context.Appointments
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(a => a.Id == payment.AppointmentId && a.IsActive);

                if (appt == null)
                    return;

                // Doctor
                var doctorUser = appt.Doctor?.User;
                var doctorEmail = doctorUser?.Email;
                var doctorName = doctorUser == null
                    ? "Doctor"
                    : $"{doctorUser.FirstName} {doctorUser.LastName}".Trim();

                // Patient (registered or unregistered)
                string patientName = appt.UnregisteredPatientName ?? "Patient";
                string? patientEmail = null;

                if (appt.PatientProfileId != null)
                {
                    var patientProfile = await _context.PatientProfiles
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.Id == appt.PatientProfileId);

                    if (patientProfile?.User != null)
                    {
                        patientName = $"{patientProfile.User.FirstName} {patientProfile.User.LastName}".Trim();
                        patientEmail = patientProfile.User.Email;
                    }
                }

                var apptTime = appt.AppointmentDateTime;

                // Email to Patient
                if (!string.IsNullOrWhiteSpace(patientEmail))
                {
                    var subject = "Appointment Confirmed – Payment Successful";
                    var body = EmailTemplates.AppointmentPaidPatient(patientName, doctorName, apptTime);
                    await _email.SendAsync(patientEmail, subject, body);
                }

                // Email to Doctor
                if (!string.IsNullOrWhiteSpace(doctorEmail))
                {
                    var subject = "New Paid Appointment Booked";
                    var body = EmailTemplates.AppointmentPaidDoctor(doctorName, patientName, apptTime, paidVia);
                    await _email.SendAsync(doctorEmail, subject, body);
                }
            }
            catch
            {
                // Payment should succeed even if email fails.
            }
        }
    }
}

