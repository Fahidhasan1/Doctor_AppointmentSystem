using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class DoctorProfileViewModel
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

        [StringLength(20)]
        public string? RoomNo { get; set; }

        [Range(0, 1000000)]
        public decimal VisitCharge { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        // ================= Read-only Fields =================

        [Required]
        [StringLength(150)]
        public string Qualification { get; set; } = null!;

        [StringLength(150)]
        public string? Designation { get; set; }

        [Range(0, 80)]
        public int Experience { get; set; }

        // Optional display-only identity info
        public string? Email { get; set; }
        public string? LicenseNumber { get; set; }
    }
}
