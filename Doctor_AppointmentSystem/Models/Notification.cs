using Doctor_AppointmentSystem.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Doctor_AppointmentSystem.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // Who is this notification for?
        public string? UserId { get; set; }
        public int? AppointmentId { get; set; }

        // Channel (Email or SMS only)
        [Required]
        public NotificationChannel Channel { get; set; }

        // Email fields
        public string? ToEmail { get; set; }
        public string? Subject { get; set; }

        // SMS fields
        public string? ToPhoneNumber { get; set; }

        // Message Body
        public string? MessageBody { get; set; }

        // Template Name
        public string? TemplateName { get; set; }

        // Status & Provider Info
        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        public string? ProviderName { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime? SentAtUtc { get; set; }

        // Flags & Audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
