using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        // ================================
        //  WHEN & WHO
        // ================================
        [Required]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        // User who performed the action (can be null e.g. system task)
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        // ================================
        //  WHAT WAS CHANGED
        // ================================

        // Name of the entity, e.g. "DoctorProfile", "Appointment", "PaymentTransaction"
        [Required]
        [StringLength(150)]
        public string EntityName { get; set; } = null!;

        // Primary key of the entity as string (e.g. "5", "b9f3-...")
        [Required]
        [StringLength(100)]
        public string EntityKey { get; set; } = null!;

        [Required]
        public AuditAction Action { get; set; }

        // Optional summary, e.g. "Status changed from Pending to Confirmed"
        [StringLength(500)]
        public string? Summary { get; set; }

        // ================================
        //  DETAILED CHANGES (OPTIONAL JSON)
        // ================================

        // Could store JSON with old/new values:
        // { "Status": { "Old": "Pending", "New": "Confirmed" }, "VisitDate": {...} }
        public string? ChangesJson { get; set; }

        // ================================
        //  CONTEXT
        // ================================

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(300)]
        public string? UserAgent { get; set; }

        // To mark if this log record is still relevant
        public bool IsActive { get; set; } = true;
    }
}
