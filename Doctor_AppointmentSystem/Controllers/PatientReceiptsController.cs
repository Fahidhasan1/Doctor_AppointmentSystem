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
            if (paymentId <= 0) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var isReceptionist = User.IsInRole("Receptionist");
            var isPatient = User.IsInRole("Patient");

            // Load payment + appointment + doctor + doctor user
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(p =>
                    p.Id == paymentId &&
                    p.IsActive &&
                    p.Status == PaymentStatus.Paid);

            if (payment == null) return NotFound();

            var appt = payment.Appointment;
            if (appt == null) return NotFound();

            // -------------------------
            // Authorization
            // -------------------------
            if (isPatient && !isReceptionist)
            {
                var patientProfileId = await _context.PatientProfiles
                    .AsNoTracking()
                    .Where(p => p.UserId == userId && p.IsActive)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync();

                if (patientProfileId == null) return Forbid();

                // Patient can only download if appointment is their registered profile
                if (appt.PatientProfileId == null) return Forbid();
                if (appt.PatientProfileId != patientProfileId.Value) return Forbid();
            }

            if (isReceptionist)
            {
                // Optional tightening: receptionist can view if they booked it OR they initiated the payment
                // (keeps it safe and correct)
                var bookedBySameReceptionist = appt.BookedByUserId == userId;
                var paidBySameReceptionist = payment.InitiatedByUserId == userId;

                if (!bookedBySameReceptionist && !paidBySameReceptionist)
                    return Forbid();
            }

            // Doctor profile + user
            var doctor = await _context.DoctorProfiles
                .AsNoTracking()
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == appt.DoctorProfileId && d.IsActive);

            if (doctor == null || doctor.User == null) return NotFound();

            // Specialty from DoctorSpecialties (primary first)
            var specialty = await _context.DoctorSpecialties
                .AsNoTracking()
                .Where(ds =>
                    ds.DoctorProfileId == appt.DoctorProfileId &&
                    ds.Specialty.IsActive)
                .OrderByDescending(ds => ds.IsPrimary)
                .Select(ds => ds.Specialty.Name)
                .FirstOrDefaultAsync();

            specialty ??= "-";

            // Issued time
            var issuedUtc = payment.PaidAtUtc ?? payment.StatusLastUpdatedUtc ?? payment.CreatedAt;

            // -------------------------
            // Patient data (Registered OR Unregistered)
            // -------------------------
            string patientName;
            string patientCode;
            string patientPhone;
            string patientGender;

            bool showPatientCode;
            bool showPatientGender;

            if (appt.PatientProfileId != null)
            {
                // Registered patient
                var patient = await _context.PatientProfiles
                    .AsNoTracking()
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == appt.PatientProfileId && p.IsActive);

                if (patient == null || patient.User == null) return NotFound();

                patientName = $"{patient.User.FirstName} {patient.User.LastName}".Trim();
                patientCode = $"P-{patient.Id:000000}";
                patientPhone = string.IsNullOrWhiteSpace(patient.User.PhoneNumber) ? "-" : patient.User.PhoneNumber;
                patientGender = patient.User.Gender == null ? "-" : patient.User.Gender.ToString();

                showPatientCode = true;
                showPatientGender = true;
            }
            else
            {
                // ✅ Unregistered patient (Receptionist booking)
                patientName = string.IsNullOrWhiteSpace(appt.UnregisteredPatientName)
                    ? "Unregistered Patient"
                    : appt.UnregisteredPatientName.Trim();

                patientPhone = string.IsNullOrWhiteSpace(appt.UnregisteredPatientPhone)
                    ? "-"
                    : appt.UnregisteredPatientPhone.Trim();

                patientCode = "-";
                patientGender = "-";

                // ✅ hide these two on receipt for unregistered
                showPatientCode = false;
                showPatientGender = false;
            }

            var roomNo = string.IsNullOrWhiteSpace(doctor.RoomNo) ? "-" : doctor.RoomNo;

            var vm = new PatientReceiptViewModel
            {
                // Header
                HospitalName = "Sunshine Hospital",
                ReceiptNo = $"RCPT-{payment.Id:000000}",
                IssuedAt = issuedUtc.ToLocalTime(),

                // Patient
                PatientName = patientName,
                PatientCode = patientCode,
                PatientPhone = patientPhone,
                PatientGender = patientGender,
                ShowPatientCode = showPatientCode,
                ShowPatientGender = showPatientGender,

                // Appointment
                DoctorName = $"{doctor.User.FirstName} {doctor.User.LastName}".Trim(),
                Specialty = specialty,
                RoomNo = roomNo,
                AppointmentDateTime = appt.AppointmentDateTime,

                // Payment
                Amount = payment.Amount,
                Method = payment.Method.ToString(),
                ProviderName = string.IsNullOrWhiteSpace(payment.ProviderName) ? "-" : payment.ProviderName,
                TransactionId = string.IsNullOrWhiteSpace(payment.GatewayTransactionId) ? "-" : payment.GatewayTransactionId,

                // Note (you can change later)
                Note = "Printed for patient verification"
            };

            // -------------------------
            // Issued By (Receptionist receipt)
            // -------------------------
            if (isReceptionist)
            {
                vm.ShowIssuer = true;
                vm.IssuerRole = "Receptionist";

                // Issuer name
                if (!string.IsNullOrWhiteSpace(payment.InitiatedByUserId))
                {
                    var issuerUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == payment.InitiatedByUserId);

                    vm.IssuedBy = issuerUser == null
                        ? "-"
                        : $"{issuerUser.FirstName} {issuerUser.LastName}".Trim();

                    // ✅ receptionist profile (ID + counter)
                    var recProfile = await _context.ReceptionistProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.UserId == payment.InitiatedByUserId && r.IsActive);

                    if (recProfile != null)
                    {
                        vm.IssuerId = recProfile.Id.ToString(); // ✅ receptionist ID
                        vm.IssuerCounterNo = string.IsNullOrWhiteSpace(recProfile.CounterNumber) ? "-" : recProfile.CounterNumber;
                    }
                    else
                    {
                        vm.IssuerId = "-";
                        vm.IssuerCounterNo = "-";
                    }
                }
                else
                {
                    vm.IssuedBy = "-";
                    vm.IssuerId = "-";
                    vm.IssuerCounterNo = "-";
                }
            }
            else
            {
                vm.ShowIssuer = false;
            }

            return View("Download", vm);
        }
    }
}
