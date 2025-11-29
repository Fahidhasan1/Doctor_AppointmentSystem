using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class ReceptionistProfile
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
        //  RECEPTIONIST BASIC INFORMATION
        // ================================

        // Office landline or extension (optional)
        [StringLength(30)]
        public string? OfficePhone { get; set; }

        // Which counter the receptionist handles
        [StringLength(30)]
        public string? CounterNumber { get; set; }

        // Active / soft delete
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
