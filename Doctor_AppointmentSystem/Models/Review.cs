using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        // ================================
        //  LINKS: DOCTOR, PATIENT, APPOINTMENT
        // ================================

        [Required]
        public int DoctorProfileId { get; set; }

        [ForeignKey(nameof(DoctorProfileId))]
        public DoctorProfile Doctor { get; set; } = null!;

        // Which patient gave this review
        [Required]
        public int PatientProfileId { get; set; }

        [ForeignKey(nameof(PatientProfileId))]
        public PatientProfile Patient { get; set; } = null!;

        // (Optional but recommended) link review to a specific appointment
        public int? AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment? Appointment { get; set; }

        // ================================
        //  REVIEW CONTENT
        // ================================

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        // Whether this review is visible to others (admin can hide)
        public bool IsVisible { get; set; } = true;

        // ================================
        //  FLAGS & AUDIT
        // ================================

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }

        // Who created the review (usually the patient’s ApplicationUserId)
        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUser? CreatedByUser { get; set; }

        public string? LastModifiedByUserId { get; set; }

        [ForeignKey(nameof(LastModifiedByUserId))]
        public ApplicationUser? LastModifiedByUser { get; set; }
    }
}
