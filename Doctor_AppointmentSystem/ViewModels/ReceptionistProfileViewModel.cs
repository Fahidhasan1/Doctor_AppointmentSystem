// File: ViewModels/ReceptionistProfileViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class ReceptionistProfileViewModel
    {
        // ================= Editable Fields =================

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public string? ProfileImagePath { get; set; }
        public IFormFile? ProfileImageFile { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        // ================= Receptionist Fields =================

        [Phone]
        [StringLength(20)]
        public string? OfficePhone { get; set; }

        [StringLength(20)]
        public string? CounterNumber { get; set; }

        // ================= Display-only Fields =================

        public string? Email { get; set; }
    }
}
