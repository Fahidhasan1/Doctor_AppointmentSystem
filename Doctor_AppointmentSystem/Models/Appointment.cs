using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        // ================================
        //  DOCTOR & PATIENT LINKS
        // ================================
        [Required]
        public int DoctorProfileId { get; set; }

        [ForeignKey(nameof(DoctorProfileId))]
        public DoctorProfile Doctor { get; set; } = null!;

        [Required]
        public int PatientProfileId { get; set; }

        [ForeignKey(nameof(PatientProfileId))]
        public PatientProfile Patient { get; set; } = null!;

        // ================================
        //  SCHEDULE INFORMATION
        // ================================
        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [Range(5, 480)]
        public int DurationMinutes { get; set; } = 20;

        [StringLength(50)]
        public string? VisitType { get; set; }

        // ================================
        //  STATUS & WORKFLOW
        // ================================
        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(250)]
        public string? CancellationReason { get; set; }

        public bool IsFirstVisit { get; set; } = false;

        public string? BookedByUserId { get; set; }

        [ForeignKey(nameof(BookedByUserId))]
        public ApplicationUser? BookedByUser { get; set; }

        public string? LastStatusChangedByUserId { get; set; }

        [ForeignKey(nameof(LastStatusChangedByUserId))]
        public ApplicationUser? LastStatusChangedByUser { get; set; }

        public bool IsActive { get; set; } = true;

        // ================================
        //  AUDIT FIELDS
        // ================================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedDate { get; set; }
    }
}
