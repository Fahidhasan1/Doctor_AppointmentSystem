using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class AdminProfileViewModel
    {
        // Basic identity fields
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Address")]
        [StringLength(200)]
        public string? Address { get; set; }

        // Image
        public string? ProfileImagePath { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfileImageFile { get; set; }

        // Admin-specific info (from AdminProfile table)
        [Display(Name = "Office Phone")]
        [Phone]
        public string? OfficePhoneNo { get; set; }

        [Display(Name = "Office Room No")]
        [StringLength(50)]
        public string? OfficeRoomNo { get; set; }
    }
}
