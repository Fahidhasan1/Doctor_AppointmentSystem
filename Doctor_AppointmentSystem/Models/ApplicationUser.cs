using Doctor_AppointmentSystem.Enums;

using Microsoft.AspNetCore.Identity;
using System;
using System.Reflection;

namespace Doctor_AppointmentSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Basic user info
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }

        public string? Address { get; set; }
        public string? ProfileImagePath { get; set; }


        // Status
        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string? CreatedByUserId { get; set; }
        public string? LastModifiedByUserId { get; set; }

        // Optional
        public DateTime? LastLoginDate { get; set; }
    }
}
