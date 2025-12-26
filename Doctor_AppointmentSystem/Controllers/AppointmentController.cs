using System;
using System.Globalization;
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
using Microsoft.Extensions.Logging;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Patient,Receptionist")]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(ApplicationDbContext context, ILogger<AppointmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // (A) View Slots Page
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> DoctorSlots(int doctorProfileId)
        {
            var doctor = await _context.DoctorProfiles
                .Include(d => d.User)
                .Where(d => d.Id == doctorProfileId && d.IsActive)
                .Select(d => new DoctorSlotsPageViewModel
                {
                    DoctorProfileId = d.Id,
                    DoctorName = (d.User.FirstName + " " + d.User.LastName).Trim()
                })
                .FirstOrDefaultAsync();

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToDashboard();
            }

            return View(doctor);
        }

        // ============================================================
        // (B) AJAX: Get Available Slots
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int doctorProfileId, DateTime date)
        {
            var bookingDate = date.Date;

            if (bookingDate < DateTime.Today)
                return Json(new { error = "Past dates are not allowed." });

            var doctorExists = await _context.DoctorProfiles
                .AnyAsync(d => d.Id == doctorProfileId && d.IsActive);

            if (!doctorExists)
                return Json(new { error = "Doctor not found." });

            var isFullDayOff = await _context.DoctorUnavailabilities.AnyAsync(u =>
                u.DoctorProfileId == doctorProfileId &&
                u.IsActive &&
                u.IsFullDay &&
                u.StartDateTime.Date <= bookingDate &&
                u.EndDateTime.Date >= bookingDate);

            if (isFullDayOff)
                return Json(new object[0]);

            var schedules = await _context.DoctorSchedules
                .Where(s => s.DoctorProfileId == doctorProfileId &&
                            s.IsActive &&
                            s.DayOfWeek == bookingDate.DayOfWeek)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!schedules.Any())
                return Json(new object[0]);

            var slotDuration = schedules.First().SlotDurationMinutes > 0
                ? schedules.First().SlotDurationMinutes
                : 20;

            TimeSpan minStartToday = TimeSpan.Zero;
            if (bookingDate == DateTime.Today)
            {
                minStartToday = RoundUpToSlotBoundary(DateTime.Now.TimeOfDay, slotDuration);
            }

            var partialUnavailabilities = await _context.DoctorUnavailabilities
                .Where(u => u.DoctorProfileId == doctorProfileId &&
                            u.IsActive &&
                            !u.IsFullDay &&
                            u.StartDateTime.Date <= bookingDate &&
                            u.EndDateTime.Date >= bookingDate)
                .ToListAsync();

            var bookedAppointments = await _context.Appointments
                .Where(a => a.DoctorProfileId == doctorProfileId &&
                            a.IsActive &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.AppointmentDateTime.Date == bookingDate)
                .Select(a => new
                {
                    Start = a.AppointmentDateTime,
                    End = a.AppointmentDateTime.AddMinutes(a.DurationMinutes)
                })
                .ToListAsync();

            var available = schedules.SelectMany(s =>
            {
                var slots = new System.Collections.Generic.List<AppointmentSlotOptionViewModel>();
                var cursor = bookingDate.Add(s.StartTime);
                var end = bookingDate.Add(s.EndTime);

                while (cursor.AddMinutes(slotDuration) <= end)
                {
                    var slotStart = cursor;
                    var slotEnd = cursor.AddMinutes(slotDuration);

                    if (bookingDate == DateTime.Today &&
                        slotStart.TimeOfDay < minStartToday)
                    {
                        cursor = cursor.AddMinutes(slotDuration);
                        continue;
                    }

                    bool overlapsUnavail = partialUnavailabilities.Any(u =>
                        slotStart < u.EndDateTime && slotEnd > u.StartDateTime);

                    bool overlapsBooked = bookedAppointments.Any(b =>
                        slotStart < b.End && slotEnd > b.Start);

                    if (!overlapsUnavail && !overlapsBooked)
                    {
                        slots.Add(new AppointmentSlotOptionViewModel
                        {
                            Value = slotStart.ToString("HH:mm"),
                            Text = slotStart.ToString("hh:mm tt")
                        });
                    }

                    cursor = cursor.AddMinutes(slotDuration);
                }

                return slots;
            });

            return Json(available.OrderBy(a => a.Value));
        }

        // ============================================================
        // (C) Create Appointment (GET)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Create(int doctorProfileId, DateTime date, string slot)
        {
            if (!TryParseSlot(slot, out var slotTime))
                return BadRequest("Invalid slot.");

            var bookingDate = date.Date;
            if (bookingDate < DateTime.Today)
                return BadRequest("Past dates are not allowed.");

            var doctor = await _context.DoctorProfiles
                .Include(d => d.User)
                .Where(d => d.Id == doctorProfileId && d.IsActive)
                .Select(d => new
                {
                    d.Id,
                    Name = (d.User.FirstName + " " + d.User.LastName).Trim()
                })
                .FirstOrDefaultAsync();

            if (doctor == null)
                return NotFound();

            var vm = new AppointmentCreateViewModel
            {
                DoctorProfileId = doctor.Id,
                DoctorName = doctor.Name,
                AppointmentDate = bookingDate,
                SelectedSlot = slotTime.ToString(@"hh\:mm")
            };

            if (!User.IsInRole("Receptionist"))
            {
                var pid = await GetCurrentPatientProfileIdAsync();
                if (pid == null) return Forbid();
                vm.PatientProfileId = pid.Value;
            }

            return View(vm);
        }

        // ============================================================
        // (D) Create Appointment (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel vm)
        {
            // Patient booking: keep your existing behavior
            if (User.IsInRole("Patient"))
            {
                var pid = await GetCurrentPatientProfileIdAsync();
                if (pid == null)
                {
                    TempData["ErrorMessage"] = "Patient profile not found.";
                    return RedirectToDashboard();
                }

                vm.PatientProfileId = pid.Value;

                // Ensure we don't store unregistered fields for patients
                vm.UnregisteredPatientName = null;
                vm.UnregisteredPatientPhone = null;
            }

            // Receptionist booking: require name + phone
            if (User.IsInRole("Receptionist"))
            {
                ModelState.Remove(nameof(vm.PatientProfileId));

                if (string.IsNullOrWhiteSpace(vm.UnregisteredPatientName))
                {
                    TempData["ErrorMessage"] = "Patient name is required.";
                    return RedirectToDashboard();
                }

                if (string.IsNullOrWhiteSpace(vm.UnregisteredPatientPhone))
                {
                    TempData["ErrorMessage"] = "Patient phone is required.";
                    return RedirectToDashboard();
                }
            }

            if (!vm.AppointmentDate.HasValue || string.IsNullOrWhiteSpace(vm.SelectedSlot))
            {
                TempData["ErrorMessage"] = "Invalid booking information.";
                return RedirectToDashboard();
            }

            if (!TryParseSlot(vm.SelectedSlot, out var slotTime))
            {
                TempData["ErrorMessage"] = "Invalid slot selected.";
                return RedirectToDashboard();
            }

            var bookingDate = vm.AppointmentDate.Value.Date;
            if (bookingDate < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Past dates are not allowed.";
                return RedirectToDashboard();
            }

            var duration = await GetDoctorSlotDurationMinutes(vm.DoctorProfileId, bookingDate.DayOfWeek) ?? 20;
            var startDateTime = bookingDate.Add(slotTime);

            var slotAvailable = await IsSlotAvailableAsync(vm.DoctorProfileId, startDateTime, duration);
            if (!slotAvailable)
            {
                TempData["ErrorMessage"] = "This slot is no longer available.";
                return RedirectToDashboard();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int? patientProfileIdToSave = null;
            if (User.IsInRole("Patient"))
            {
                patientProfileIdToSave = vm.PatientProfileId;
            }

            var appointment = new Appointment
            {
                DoctorProfileId = vm.DoctorProfileId,
                PatientProfileId = patientProfileIdToSave,

                // receptionist typed info
                UnregisteredPatientName = User.IsInRole("Receptionist") ? vm.UnregisteredPatientName?.Trim() : null,
                UnregisteredPatientPhone = User.IsInRole("Receptionist") ? vm.UnregisteredPatientPhone?.Trim() : null,

                AppointmentDateTime = startDateTime,
                DurationMinutes = duration,
                VisitType = vm.VisitType,
                IsFirstVisit = vm.IsFirstVisit,

                Status = AppointmentStatus.Confirmed,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                BookedByUserId = userId
            };

            try
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment booked successfully.";
                return RedirectToDashboard();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Appointment save failed. DoctorProfileId={DoctorId}, PatientProfileId={PatientId}, Start={Start}",
                    vm.DoctorProfileId, patientProfileIdToSave, startDateTime);

                TempData["ErrorMessage"] = "Could not save appointment. Please try again.";
                return RedirectToDashboard();
            }
        }

        // ============================================================
        // Helpers
        // ============================================================
        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Receptionist"))
                return RedirectToAction("Index", "ReceptionistDashboard");

            return RedirectToAction("Index", "PatientDashboard");
        }

        private async Task<int?> GetCurrentPatientProfileIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.PatientProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<int?> GetDoctorSlotDurationMinutes(int doctorProfileId, DayOfWeek dayOfWeek)
        {
            return await _context.DoctorSchedules
                .Where(s => s.DoctorProfileId == doctorProfileId &&
                            s.IsActive &&
                            s.DayOfWeek == dayOfWeek)
                .Select(s => (int?)s.SlotDurationMinutes)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> IsSlotAvailableAsync(int doctorProfileId, DateTime slotStart, int durationMinutes)
        {
            var slotEnd = slotStart.AddMinutes(durationMinutes);

            return !await _context.Appointments.AnyAsync(a =>
                a.DoctorProfileId == doctorProfileId &&
                a.IsActive &&
                a.Status != AppointmentStatus.Cancelled &&
                a.AppointmentDateTime < slotEnd &&
                a.AppointmentDateTime.AddMinutes(a.DurationMinutes) > slotStart);
        }

        private static TimeSpan RoundUpToSlotBoundary(TimeSpan now, int slotMinutes)
        {
            var totalMinutes = (int)Math.Ceiling(now.TotalMinutes);
            var remainder = totalMinutes % slotMinutes;
            return TimeSpan.FromMinutes(remainder == 0
                ? totalMinutes
                : totalMinutes + (slotMinutes - remainder));
        }

        private static bool TryParseSlot(string slot, out TimeSpan time)
        {
            if (TimeSpan.TryParse(slot, out time))
                return true;

            if (DateTime.TryParseExact(slot, "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                DateTime.TryParseExact(slot, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                time = dt.TimeOfDay;
                return true;
            }

            time = default;
            return false;
        }
    }
}
