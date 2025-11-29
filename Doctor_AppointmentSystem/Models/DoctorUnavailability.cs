using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class DoctorUnavailability
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
        //  UNAVAILABILITY WINDOW
        // ================================

        // When the unavailability starts (local date/time)
        [Required]
        public DateTime StartDateTime { get; set; }

        // When the unavailability ends (local date/time)
        [Required]
        public DateTime EndDateTime { get; set; }

        // Is this a full-day block (e.g., full day leave)
        public bool IsFullDay { get; set; } = false;

        // Optional short reason, e.g. "Leave", "Conference", "Emergency"
        [StringLength(200)]
        public string? Reason { get; set; }

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
