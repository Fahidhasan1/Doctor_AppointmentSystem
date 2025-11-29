using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class NotificationLog
    {
        [Key]
        public int Id { get; set; }

        // ================================
        //  WHO IS THIS NOTIFICATION ABOUT?
        // ================================

        // Optional: the user in the system this notification belongs to
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        // Optional: tie notification to an appointment (e.g. reminder, confirmation)
        public int? AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment? Appointment { get; set; }

        // ================================
        //  DELIVERY DETAILS
        // ================================
        [Required]
        public NotificationChannel Channel { get; set; }

        // For email notifications
        [StringLength(200)]
        public string? ToEmail { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        // For SMS notifications
        [StringLength(30)]
        public string? ToPhoneNumber { get; set; }

        // Message body (email content or SMS text, possibly truncated)
        [StringLength(2000)]
        public string? MessageBody { get; set; }

        // Optional: the template name/key used (e.g. "AppointmentReminder")
        [StringLength(100)]
        public string? TemplateName { get; set; }

        // ================================
        //  STATUS & PROVIDER INFO
        // ================================
        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        // Which external provider was used (e.g., "SMTP", "Twilio", "Nexmo")
        [StringLength(50)]
        public string? ProviderName { get; set; }

        // Provider message ID / tracking ID if any
        [StringLength(100)]
        public string? ProviderMessageId { get; set; }

        // Error message if sending failed
        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        // When the message was actually sent (UTC)
        public DateTime? SentAtUtc { get; set; }

        // ================================
        //  FLAGS & AUDIT
        // ================================
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedDate { get; set; }
    }
}
