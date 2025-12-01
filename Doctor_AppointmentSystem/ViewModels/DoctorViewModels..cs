using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Doctor_AppointmentSystem.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Doctor_AppointmentSystem.ViewModels
{
    // =========================
    //  Doctor list item (Index)
    // =========================
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

    // =========================
    //  Doctor create
    // =========================
    public class DoctorCreateViewModel
    {
        // ========= ApplicationUser fields =========

        [Required]
        [Display(Name = "First Name")]
        [MaxLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [Display(Name = "Last Name")]
        [MaxLength(50)]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public Gender Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = null!;

        // ========= DoctorProfile fields =========

        [Required]
        [StringLength(50)]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = null!;

        [Required]
        [StringLength(150)]
        [Display(Name = "Qualification")]
        public string Qualification { get; set; } = null!;

        [Display(Name = "Designation (e.g. Consultant Cardiologist)")]
        [MaxLength(100)]
        public string? Designation { get; set; }

        [Range(0, 60)]
        [Display(Name = "Years of Experience")]
        public int Experience { get; set; }

        [Required]
        [Range(0, 999999)]
        [DataType(DataType.Currency)]
        [Display(Name = "Visit Charge")]
        public decimal VisitCharge { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Specialties")]
        public List<int> SelectedSpecialtyIds { get; set; } = new();

        public List<SelectListItem> SpecialtyOptions { get; set; } = new();
    }
    // =========================
    //  Doctor edit
    // =========================
    public class DoctorEditViewModel
    {
        public int DoctorProfileId { get; set; }
        public string UserId { get; set; } = null!;

        // ========= ApplicationUser fields =========
        [Required]
        [MaxLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;   // shown as read-only in UI

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public Gender Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        // ========= DoctorProfile fields =========

        [Required]
        [StringLength(50)]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = null!;  // read-only in UI, not changed in POST

        [Required]
        [StringLength(150)]
        [Display(Name = "Qualification")]
        public string Qualification { get; set; } = null!;

        [StringLength(100)]
        [Display(Name = "Designation (e.g. Consultant Cardiologist)")]
        public string? Designation { get; set; }

        [Range(0, 60)]
        [Display(Name = "Years of Experience")]
        public int Experience { get; set; }

        [Required]
        [Range(0, 999999)]
        [DataType(DataType.Currency)]
        [Display(Name = "Visit Charge")]
        public decimal VisitCharge { get; set; }

        [Display(Name = "Current Profile Photo")]
        public string? ExistingProfileImagePath { get; set; }

        [Display(Name = "New Profile Photo")]
        public IFormFile? NewProfileImage { get; set; }

        [Display(Name = "Specialties")]
        public List<int> SelectedSpecialtyIds { get; set; } = new();

        public List<SelectListItem> SpecialtyOptions { get; set; } = new();
    }
}
