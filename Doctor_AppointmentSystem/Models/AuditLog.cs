using Doctor_AppointmentSystem.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Doctor_AppointmentSystem.Models
{
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        // When & Who
        [Required]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; }

        // What was changed
        [Required]
        [StringLength(150)]
        public string EntityName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string EntityKey { get; set; } = null!;

        [Required]
        public AuditAction Action { get; set; }

        [StringLength(500)]
        public string? Summary { get; set; }

        // Detailed changes (JSON)
        public string? ChangesJson { get; set; }

        // Context
        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(300)]
        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
