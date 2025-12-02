using System;
namespace Doctor_AppointmentSystem.ViewModels
{
    public class PatientListItemViewModel
    {
        public int PatientProfileId { get; set; }
        public string UserId { get; set; } = null!;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public string Gender { get; set; } = "Not specified";

        public bool IsActive { get; set; }

        public string? ProfileImagePath { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
