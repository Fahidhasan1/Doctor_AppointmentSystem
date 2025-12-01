using System;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class DoctorListItemViewModel
    {
        public int DoctorProfileId { get; set; }
        public string UserId { get; set; } = null!;

        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string? ProfileImagePath { get; set; }
        public string? Designation { get; set; }
        public int Experience { get; set; }

        public string Specialties { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
