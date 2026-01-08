using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
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

        public SslCommerzController(ApplicationDbContext context, ISslCommerzService ssl)
        {
            _context = context;
            _ssl = ssl;
        }

        // SSLCOMMERZ will POST here
        [HttpPost]
        [Route("sslcommerz/success")]
        public async Task<IActionResult> Success()
        {
            var tranId = Request.Form["tran_id"].ToString();
            var valId = Request.Form["val_id"].ToString();

            if (string.IsNullOrWhiteSpace(tranId) || string.IsNullOrWhiteSpace(valId))
                return BadRequest("Missing tran_id or val_id");

            var payment = await _context.Payments.FirstOrDefaultAsync(p =>
                p.IsActive && p.GatewayTransactionId == tranId);

            if (payment == null)
                return NotFound("Payment record not found.");

            var validation = await _ssl.ValidateAsync(valId);

            // SSLCOMMERZ: status should be VALID
            if (!string.Equals(validation.status, "VALID", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = PaymentStatus.Failed;
                payment.StatusLastUpdatedUtc = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "Payment validation failed.";
                return RedirectToAction("Index", "PatientAppointments");
            }

            // OPTIONAL: extra checks for safety
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

            payment.Status = PaymentStatus.Paid;
            payment.PaidAtUtc = DateTime.UtcNow;
            payment.StatusLastUpdatedUtc = DateTime.UtcNow;
            payment.ProviderName = "SSLCOMMERZ";
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment successful!";
            return RedirectToAction("Index", "PatientAppointments");
        }

        [HttpPost]
        [Route("sslcommerz/fail")]
        public async Task<IActionResult> Fail()
        {
            var tranId = Request.Form["tran_id"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var payment = await _context.Payments.FirstOrDefaultAsync(p =>
                    p.IsActive && p.GatewayTransactionId == tranId);

                if (payment != null)
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

        [HttpPost]
        [Route("sslcommerz/cancel")]
        public async Task<IActionResult> Cancel()
        {
            var tranId = Request.Form["tran_id"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var payment = await _context.Payments.FirstOrDefaultAsync(p =>
                    p.IsActive && p.GatewayTransactionId == tranId);

                if (payment != null)
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

        // Recommended: IPN for reliability
        [HttpPost]
        [Route("sslcommerz/ipn")]
        public async Task<IActionResult> Ipn()
        {
            var tranId = Request.Form["tran_id"].ToString();
            var valId = Request.Form["val_id"].ToString();

            if (string.IsNullOrWhiteSpace(tranId) || string.IsNullOrWhiteSpace(valId))
                return Ok(); // don't break IPN calls

            var payment = await _context.Payments.FirstOrDefaultAsync(p =>
                p.IsActive && p.GatewayTransactionId == tranId);

            if (payment == null)
                return Ok();

            // If already paid, do nothing
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
    }
}
