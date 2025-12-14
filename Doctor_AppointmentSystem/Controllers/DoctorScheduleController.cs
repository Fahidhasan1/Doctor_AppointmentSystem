using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorScheduleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ----------------------------
        // GET: /DoctorSchedule/ManageTimeSlots
        // ----------------------------
        [HttpGet]
        public async Task<IActionResult> ManageTimeSlots()
        {
            var doctorProfileId = await GetCurrentDoctorProfileIdAsync();
            if (doctorProfileId == null) return Forbid();

            // Load active schedules (weekly recurring)
            var schedules = await _context.DoctorSchedules
                .Where(x => x.DoctorProfileId == doctorProfileId.Value && x.IsActive)
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            // Slot duration (if empty, default 20)
            var slotDuration = schedules.FirstOrDefault()?.SlotDurationMinutes ?? 20;

            // Build VM
            var vm = new DoctorManageTimeSlotsViewModel
            {
                DoctorProfileId = doctorProfileId.Value,
                SlotDurationMinutes = slotDuration,
                Days = new List<DoctorWeeklyDaySlotViewModel>()
            };

            // We will populate one row per schedule block (so multiple blocks/day works)
            foreach (var s in schedules)
            {
                vm.Days.Add(new DoctorWeeklyDaySlotViewModel
                {
                    ScheduleId = s.Id,
                    DayOfWeek = s.DayOfWeek,
                    IsAvailable = true,
                    StartTime = TimeHelpers.ToHHmm(s.StartTime),
                    EndTime = TimeHelpers.ToHHmm(s.EndTime),

                    // Breaks ignored (not stored)
                    BreakStartTime = null,
                    BreakEndTime = null
                });
            }

            // If doctor has no schedule yet, show 7 empty rows (one per day) as starter
            if (!vm.Days.Any())
            {
                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    vm.Days.Add(new DoctorWeeklyDaySlotViewModel
                    {
                        DayOfWeek = day,
                        IsAvailable = false
                    });
                }
            }

            return View(vm);
        }

        // ----------------------------
        // POST: /DoctorSchedule/ManageTimeSlots
        // ----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageTimeSlots(DoctorManageTimeSlotsViewModel vm)
        {
            var doctorProfileId = await GetCurrentDoctorProfileIdAsync();
            if (doctorProfileId == null)
                return Forbid();

            // ✅ STEP 1: Extract ONLY rows that have actual time values
            var validRows = vm.Days
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.StartTime) &&
                    !string.IsNullOrWhiteSpace(d.EndTime))
                .ToList();

            if (!validRows.Any())
            {
                TempData["ErrorMessage"] = "Please add at least one valid time slot.";
                return RedirectToAction(nameof(ManageTimeSlots));
            }

            // ✅ STEP 2: Convert & validate time blocks
            var blocks = new List<(DayOfWeek Day, TimeSpan Start, TimeSpan End)>();

            foreach (var row in validRows)
            {
                if (!TimeHelpers.TryParseHHmm(row.StartTime!, out var start) ||
                    !TimeHelpers.TryParseHHmm(row.EndTime!, out var end))
                {
                    TempData["ErrorMessage"] = $"{row.DayOfWeek}: Invalid time format.";
                    return RedirectToAction(nameof(ManageTimeSlots));
                }

                if (start >= end)
                {
                    TempData["ErrorMessage"] = $"{row.DayOfWeek}: Start time must be earlier than End time.";
                    return RedirectToAction(nameof(ManageTimeSlots));
                }

                blocks.Add((row.DayOfWeek, start, end));
            }

            // ✅ STEP 3: Overlap validation (per day)
            foreach (var grp in blocks.GroupBy(b => b.Day))
            {
                var ordered = grp.OrderBy(x => x.Start).ToList();
                for (int i = 1; i < ordered.Count; i++)
                {
                    if (ordered[i].Start < ordered[i - 1].End)
                    {
                        TempData["ErrorMessage"] = $"{grp.Key}: Time blocks overlap.";
                        return RedirectToAction(nameof(ManageTimeSlots));
                    }
                }
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ✅ STEP 4: Deactivate old schedules
                var existing = await _context.DoctorSchedules
                    .Where(x => x.DoctorProfileId == doctorProfileId && x.IsActive)
                    .ToListAsync();

                foreach (var ex in existing)
                {
                    ex.IsActive = false;
                    ex.UpdatedAt = DateTime.UtcNow;
                    ex.LastModifiedByUserId = userId;
                }

                // ✅ STEP 5: Insert new schedules
                var newSchedules = blocks.Select(b => new DoctorSchedule
                {
                    DoctorProfileId = doctorProfileId.Value,
                    DayOfWeek = b.Day,
                    StartTime = b.Start,
                    EndTime = b.End,
                    SlotDurationMinutes = vm.SlotDurationMinutes,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                }).ToList();

                _context.DoctorSchedules.AddRange(newSchedules);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Time slots saved successfully.";
                return RedirectToAction(nameof(ManageTimeSlots));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["ErrorMessage"] = "Failed to save time slots.";
                return RedirectToAction(nameof(ManageTimeSlots));
            }
        }


        // ----------------------------
        // Helpers
        // ----------------------------
        private async Task<int?> GetCurrentDoctorProfileIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            // Adjust this if your DoctorProfile uses different field name for user link
            var doctorProfileId = await _context.DoctorProfiles
                .Where(d => d.UserId == userId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            return doctorProfileId;
        }
    }
}
