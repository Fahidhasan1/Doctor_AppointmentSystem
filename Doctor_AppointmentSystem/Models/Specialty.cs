using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class Specialty
    {
        [Key]
        public int Id { get; set; }

        // Name of the specialty (Cardiology, Neurology, Pediatrics...)
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        // Optional longer description
        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // ===== Audit =====
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
