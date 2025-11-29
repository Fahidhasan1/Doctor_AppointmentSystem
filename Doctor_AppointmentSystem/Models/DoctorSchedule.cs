using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class DoctorSchedule
    {
        [Key]
        public int Id { get; set; }

        // ================================
        //  LINK TO DOCTOR
        // ================================
        [Required]
        public int DoctorProfileId { get; set; }

        [ForeignKey(nameof(DoctorProfileId))]
        public DoctorProfile Doctor { get; set; } = null!;

        // ================================
        //  WEEKLY SCHEDULE
        // ================================

        // Day of week for this schedule (Sunday, Monday, etc.)
        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        // Start time in local time (e.g. 10:00)
        [Required]
        public TimeSpan StartTime { get; set; }

        // End time in local time (e.g. 13:00)
        [Required]
        public TimeSpan EndTime { get; set; }

        // Optional: default slot duration for this block (e.g. 15 or 20 minutes)
        [Range(5, 240)]
        public int SlotDurationMinutes { get; set; } = 20;

        // Date from which this schedule is valid (optional)
        public DateTime? EffectiveFromDate { get; set; }

        // Date until which this schedule is valid (optional)
        public DateTime? EffectiveToDate { get; set; }

        public bool IsActive { get; set; } = true;

        // ================================
        //  AUDIT FIELDS
        // ================================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }

        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUser? CreatedByUser { get; set; }

        public string? LastModifiedByUserId { get; set; }

        [ForeignKey(nameof(LastModifiedByUserId))]
        public ApplicationUser? LastModifiedByUser { get; set; }
    }
}
