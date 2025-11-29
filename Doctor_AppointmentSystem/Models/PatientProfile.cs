using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class PatientProfile
    {
        [Key]
        public int Id { get; set; }

        // ================================
        //  LINK TO ASP.NET IDENTITY USER
        // ================================
        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        // ================================
        //  PATIENT HEALTH INFORMATION
        // ================================

        // Blood group (e.g., O+, AB-, etc.)
        [StringLength(10)]
        public string? BloodGroup { get; set; }

        // ================================
        //  EMERGENCY CONTACT INFORMATION
        // ================================

        [StringLength(100)]
        public string? EmergencyContactName { get; set; }

        [StringLength(20)]
        public string? EmergencyContact { get; set; }

        [StringLength(20)]
        public string? EmergencyContactRelation { get; set; }

        // ================================
        //  PATIENT STATUS
        // ================================

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
