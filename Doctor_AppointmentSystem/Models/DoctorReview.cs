using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class DoctorReview
    {
        [Key]
        public int Id { get; set; }

        // Doctor & Patient
        [Required]
        public int DoctorProfileId { get; set; }

        [Required]
        public int PatientProfileId { get; set; }

        // Link to Appointment (optional)
        public int? AppointmentId { get; set; }

        // Review Content
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? LastModifiedByUserId { get; set; }
    }
}
