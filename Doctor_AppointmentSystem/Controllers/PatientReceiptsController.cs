using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient,Receptionist")]
    public class PatientReceiptsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientReceiptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /PatientReceipts/Download?paymentId=123
        [HttpGet]
        public async Task<IActionResult> Download(int paymentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var isReceptionist = User.IsInRole("Receptionist");
            var isPatient = User.IsInRole("Patient");

            // Load payment + appointment
            var payment = await _context.Payments
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.IsActive);

            if (payment == null)
                return NotFound();

            // Receipt only for PAID payments
            if (payment.Status != PaymentStatus.Paid)
                return Forbid();

            var appt = payment.Appointment;
            if (appt == null)
                return NotFound();

            // If patient, enforce ownership (patient can only see their own receipt)
            if (isPatient && !isReceptionist)
            {
                var patientProfileId = await _context.PatientProfiles
                    .Where(p => p.UserId == userId && p.IsActive)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync();

                if (patientProfileId == null)
                    return Forbid();

                if (appt.PatientProfileId != patientProfileId.Value)
                    return Forbid();
            }

            // Load patient profile + user (ApplicationUser)
            var patient = await _context.PatientProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == appt.PatientProfileId && p.IsActive);

            if (patient == null || patient.User == null)
                return NotFound();

            // Load doctor profile + user
            var doctor = await _context.DoctorProfiles
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == appt.DoctorProfileId && d.IsActive);

            if (doctor == null || doctor.User == null)
                return NotFound();

            // Get specialty name
            var specialty = await _context.DoctorSpecialties
                .Where(ds => ds.DoctorProfileId == appt.DoctorProfileId)
                .Select(ds => ds.Specialty.Name)
                .FirstOrDefaultAsync();

            // Issued time
            var issuedUtc = payment.PaidAtUtc ?? payment.StatusLastUpdatedUtc ?? DateTime.UtcNow;

            // PhoneNumber exists in IdentityUser
            var phone = string.IsNullOrWhiteSpace(patient.User.PhoneNumber)
                ? "-"
                : patient.User.PhoneNumber;

            // Gender is in your ApplicationUser model
            var gender = patient.User.Gender == null
                ? "-"
                : patient.User.Gender.ToString();

            // RoomNo is in DoctorProfile
            var roomNo = string.IsNullOrWhiteSpace(doctor.RoomNo)
                ? "-"
                : doctor.RoomNo;

            var vm = new PatientReceiptViewModel
            {
                // Header
                HospitalName = "Sunshine Hospital",
                ReceiptNo = $"RCPT-{payment.Id:000000}",
                IssuedAt = issuedUtc.ToLocalTime(),

                // Patient
                PatientName = $"{patient.User.FirstName} {patient.User.LastName}".Trim(),
                PatientCode = $"P-{patient.Id:000000}",
                PatientPhone = phone,
                PatientGender = gender,

                // Appointment
                DoctorName = $"{doctor.User.FirstName} {doctor.User.LastName}".Trim(),
                Specialty = specialty ?? "-",
                RoomNo = roomNo,
                AppointmentDateTime = appt.AppointmentDateTime,

                // Payment
                Amount = payment.Amount,
                Method = payment.Method.ToString(),
                ProviderName = string.IsNullOrWhiteSpace(payment.ProviderName) ? "-" : payment.ProviderName,
                TransactionId = string.IsNullOrWhiteSpace(payment.GatewayTransactionId) ? "-" : payment.GatewayTransactionId
            };

            // If receptionist is viewing, show issuer info
            if (isReceptionist)
            {
                var issuer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                vm.ShowIssuer = true;
                vm.IssuerRole = "Receptionist";
                vm.IssuedBy = issuer == null
                    ? "-"
                    : $"{issuer.FirstName} {issuer.LastName}".Trim();
            }
            else
            {
                vm.ShowIssuer = false;
            }

            return View(vm);
        }
    }
}
