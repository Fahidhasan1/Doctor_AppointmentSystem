using System;
using System.ComponentModel.DataAnnotations;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class PatientProfileViewModel
    {
        public string? ProfileImagePath { get; set; }

        [Required] public string FirstName { get; set; } = "";
        [Required] public string LastName { get; set; } = "";

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        public string? Address { get; set; }

        public string? BloodGroup { get; set; }

        public string? EmergencyContactName { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyContactRelation { get; set; }
    }
}
