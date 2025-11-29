using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        // ================================
        //  LINK TO APPOINTMENT
        // ================================
        [Required]
        public int AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; } = null!;

        // ================================
        //  AMOUNT & CURRENCY
        // ================================
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 99999999)]
        public decimal Amount { get; set; }

        // e.g. "BDT"
        [StringLength(10)]
        public string Currency { get; set; } = "BDT";

        // ================================
        //  STATUS & METHOD
        // ================================
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        public PaymentMethod Method { get; set; } = PaymentMethod.Unknown;

        // For online/m-banking: transaction ID from gateway / provider
        [StringLength(100)]
        public string? GatewayTransactionId { get; set; }

        // Optional: which payment provider/gateway was used
        // e.g., "bKash", "Nagad", "SSLCommerz", etc.
        [StringLength(50)]
        public string? ProviderName { get; set; }

        // When payment was completed (if successful)
        public DateTime? PaidAtUtc { get; set; }

        // When status was last updated
        public DateTime? StatusLastUpdatedUtc { get; set; }

        // Who initiated the payment (patient usually, but could be admin)
        public string? InitiatedByUserId { get; set; }

        [ForeignKey(nameof(InitiatedByUserId))]
        public ApplicationUser? InitiatedByUser { get; set; }

        // ================================
        //  FLAGS
        // ================================
        public bool IsActive { get; set; } = true;

        // ================================
        //  AUDIT FIELDS
        // ================================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }
    }
}
