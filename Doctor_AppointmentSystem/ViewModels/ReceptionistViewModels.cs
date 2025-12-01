using System;
using System.ComponentModel.DataAnnotations;
using Doctor_AppointmentSystem.Enums;
using Microsoft.AspNetCore.Http;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class ReceptionistListItemViewModel
    {
        public int ReceptionistProfileId { get; set; }   // primary key for profile
        public string? UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public string? OfficePhone { get; set; }
        public string? CounterNumber { get; set; }

        public string? ProfileImagePath { get; set; }

        public bool IsActive { get; set; }
    }

    public class ReceptionistCreateViewModel
    {
        // -------- ApplicationUser fields ----------
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public Gender Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        // -------- ReceptionistProfile fields ----------
        [StringLength(50)]
        public string? OfficePhone { get; set; }

        [StringLength(50)]
        public string? CounterNumber { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }

    public class ReceptionistEditViewModel
    {
        // primary key of ReceptionistProfile
        public int Id { get; set; }

        public string? UserId { get; set; }

        // -------- ApplicationUser fields ----------
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public Gender Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        // -------- ReceptionistProfile fields ----------
        [StringLength(50)]
        public string? OfficePhone { get; set; }

        [StringLength(50)]
        public string? CounterNumber { get; set; }

        public string? ExistingProfileImagePath { get; set; }

        public IFormFile? NewProfileImage { get; set; }
    }
}
