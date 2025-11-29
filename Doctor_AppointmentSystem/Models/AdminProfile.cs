using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class AdminProfile
    {
        [Key]
        public int Id { get; set; }

        // ============================================
        //  LINK TO ASP.NET IDENTITY USER
        // ============================================
        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        // ============================================
        //  ADMIN-SPECIFIC INFORMATION
        // ============================================

        // Office room number for admin (optional)
        [StringLength(20)]
        public string? OfficeRoomNo { get; set; }

        // Does this admin have full access permissions?
        public bool IsSuperAdmin { get; set; } = false;

        // Active / soft delete
        public bool IsActive { get; set; } = true;

        // ============================================
        //  AUDIT FIELDS
        // ============================================

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
