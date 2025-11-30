using Doctor_AppointmentSystem.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        // Link to Appointment
        [Required]
        public int AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; } = null!;

        // Amount
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "BDT";

        // Status & Method
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        public PaymentMethod Method { get; set; }

        public string? GatewayTransactionId { get; set; }
        public string? ProviderName { get; set; }

        public DateTime? PaidAtUtc { get; set; }
        public DateTime? StatusLastUpdatedUtc { get; set; }

        // User tracking
        public string? InitiatedByUserId { get; set; }

        // Flags
        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
