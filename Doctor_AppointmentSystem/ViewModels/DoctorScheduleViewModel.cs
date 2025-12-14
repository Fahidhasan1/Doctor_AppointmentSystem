using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Doctor_AppointmentSystem.ViewModels
{
    // =============== Manage Weekly Time Slots (Main VM) ===============
    public class DoctorManageTimeSlotsViewModel : IValidatableObject
    {
        public int DoctorProfileId { get; set; }
        public string? DoctorName { get; set; }

        [Range(5, 240)]
        [Display(Name = "Slot Duration (Minutes)")]
        public int SlotDurationMinutes { get; set; } = 20;

        // Optional: only if you want effective range later.
        // If you don't need it, keep but ignore in controller.
        [DataType(DataType.Date)]
        [Display(Name = "Effective From")]
        public DateTime? EffectiveFromDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Effective To")]
        public DateTime? EffectiveToDate { get; set; }

        // 7 days schedule rows
        public List<DoctorWeeklyDaySlotViewModel> Days { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validate date range (optional)
            if (EffectiveFromDate.HasValue && EffectiveToDate.HasValue &&
                EffectiveFromDate.Value.Date > EffectiveToDate.Value.Date)
            {
                yield return new ValidationResult(
                    "Effective From date cannot be after Effective To date.",
                    new[] { nameof(EffectiveFromDate), nameof(EffectiveToDate) }
                );
            }

            // Validate each day rule
            foreach (var d in Days)
            {
                if (!d.IsAvailable)
                    continue;

                // Start/End required
                if (string.IsNullOrWhiteSpace(d.StartTime) || string.IsNullOrWhiteSpace(d.EndTime))
                {
                    yield return new ValidationResult(
                        $"{d.DayOfWeek}: Start Time and End Time are required when Available is checked.",
                        new[] { nameof(Days) }
                    );
                    continue;
                }

                if (!TimeHelpers.TryParseHHmm(d.StartTime!, out var start) ||
                    !TimeHelpers.TryParseHHmm(d.EndTime!, out var end))
                {
                    yield return new ValidationResult(
                        $"{d.DayOfWeek}: Invalid time format. Use HH:mm (e.g., 09:30).",
                        new[] { nameof(Days) }
                    );
                    continue;
                }

                if (start >= end)
                {
                    yield return new ValidationResult(
                        $"{d.DayOfWeek}: Start Time must be earlier than End Time.",
                        new[] { nameof(Days) }
                    );
                }

                // Break validation (optional)
                var breakProvided = !string.IsNullOrWhiteSpace(d.BreakStartTime) ||
                                   !string.IsNullOrWhiteSpace(d.BreakEndTime);

                if (breakProvided)
                {
                    if (string.IsNullOrWhiteSpace(d.BreakStartTime) ||
                        string.IsNullOrWhiteSpace(d.BreakEndTime))
                    {
                        yield return new ValidationResult(
                            $"{d.DayOfWeek}: Both Break Start and Break End must be provided.",
                            new[] { nameof(Days) }
                        );
                        continue;
                    }

                    if (!TimeHelpers.TryParseHHmm(d.BreakStartTime!, out var bStart) ||
                        !TimeHelpers.TryParseHHmm(d.BreakEndTime!, out var bEnd))
                    {
                        yield return new ValidationResult(
                            $"{d.DayOfWeek}: Invalid break time format. Use HH:mm.",
                            new[] { nameof(Days) }
                        );
                        continue;
                    }

                    if (bStart >= bEnd)
                    {
                        yield return new ValidationResult(
                            $"{d.DayOfWeek}: Break Start must be earlier than Break End.",
                            new[] { nameof(Days) }
                        );
                    }

                    // Break must be within working hours
                    if (bStart < start || bEnd > end)
                    {
                        yield return new ValidationResult(
                            $"{d.DayOfWeek}: Break must be within Start and End time.",
                            new[] { nameof(Days) }
                        );
                    }
                }
            }

            // (Optional) If all days are unavailable -> allow or block
            // If you want to prevent "no availability at all", uncomment:
            // if (Days.All(x => !x.IsAvailable))
            // {
            //     yield return new ValidationResult("You must set at least one available day.", new[] { nameof(Days) });
            // }
        }
    }

    // =============== Weekly Day Row VM ===============
    public class DoctorWeeklyDaySlotViewModel
    {
        // If you later edit day entries individually, keep ScheduleId
        public int? ScheduleId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; }

        // Use "HH:mm" strings for simpler Razor form binding
        [Display(Name = "Start Time")]
        public string? StartTime { get; set; }

        [Display(Name = "End Time")]
        public string? EndTime { get; set; }

        // Optional: break/lunch time (date-independent)
        [Display(Name = "Break Start")]
        public string? BreakStartTime { get; set; }

        [Display(Name = "Break End")]
        public string? BreakEndTime { get; set; }
    }

    // =============== For showing saved schedule in a table (optional) ===============
    public class DoctorScheduleListItemViewModel
    {
        public int Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int SlotDurationMinutes { get; set; }

        public DateTime? EffectiveFromDate { get; set; }
        public DateTime? EffectiveToDate { get; set; }

        public bool IsActive { get; set; }
    }

    // =============== Helper for parsing HH:mm -> TimeSpan ===============
    public static class TimeHelpers
    {
        public static bool TryParseHHmm(string value, out TimeSpan time)
        {
            // Supports 09:30, 18:05 etc.
            return TimeSpan.TryParseExact(value.Trim(),
                new[] { @"hh\:mm", @"h\:mm" },
                CultureInfo.InvariantCulture,
                out time);
        }

        public static string ToHHmm(TimeSpan time)
        {
            return time.ToString(@"hh\:mm");
        }
    }
}
