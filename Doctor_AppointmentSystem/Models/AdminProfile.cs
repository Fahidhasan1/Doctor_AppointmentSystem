using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class AdminProfile
    {
        [Key]
        public int Id { get; set; }

        // Link to Identity User
        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        // Admin Information
        public string? OfficeRoomNo { get; set; }
        public string? OfficePhoneNo { get; set; }   // ✅ Added
        public bool IsSuperAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? LastModifiedByUserId { get; set; }
    }
}
