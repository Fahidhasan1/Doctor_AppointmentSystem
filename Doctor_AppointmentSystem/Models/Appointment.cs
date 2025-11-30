using Doctor_AppointmentSystem.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Doctor_AppointmentSystem.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        // Doctor & Patient Links
        [Required]
        public int DoctorProfileId { get; set; }

        [ForeignKey(nameof(DoctorProfileId))]
        public DoctorProfile Doctor { get; set; } = null!;

        [Required]
        public int PatientProfileId { get; set; }

        [ForeignKey(nameof(PatientProfileId))]
        public PatientProfile Patient { get; set; } = null!;

        // Schedule Information
        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [Range(5, 480)]
        public int DurationMinutes { get; set; } = 20;

        public string? VisitType { get; set; }

        // Status & Workflow
        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;

        public string? CancellationReason { get; set; }
        public bool IsFirstVisit { get; set; } = false;

        public string? BookedByUserId { get; set; }
        public string? LastStatusChangedByUserId { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
