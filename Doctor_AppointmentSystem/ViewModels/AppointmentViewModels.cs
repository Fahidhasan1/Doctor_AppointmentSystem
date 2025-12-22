using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    // =========================
    // 0) Doctor Slots Page VM (View Slots screen)
    // =========================
    public class DoctorSlotsPageViewModel
    {
        public int DoctorProfileId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
    }

    // =========================
    // 1) Slot option shown in UI
    // =========================
    public class AppointmentSlotOptionViewModel
    {
        // "08:00", "08:30" etc. (easy for dropdown binding)
        public string Value { get; set; } = string.Empty;

        // "8:00 AM - 8:30 AM" or "8:00 AM" (nice UI label)
        public string Text { get; set; } = string.Empty;

        // If later you want to show disabled items (booked/unavailable)
        public bool IsDisabled { get; set; } = false;
    }

    // =========================
    // 2) Doctor dropdown option
    // =========================
    public class AppointmentDoctorOptionViewModel
    {
        public int DoctorProfileId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? SpecialtyName { get; set; }
    }

    // =========================
    // 3) Patient dropdown option
    // (kept for any future use, but receptionist booking now uses typed name)
    // =========================
    public class AppointmentPatientOptionViewModel
    {
        public int PatientProfileId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    // =========================
    // 4) Create Appointment VM
    // =========================
    public class AppointmentCreateViewModel
    {
        // Dropdown sources (useful for Filter page / Book Appointment page)
        public List<AppointmentDoctorOptionViewModel> Doctors { get; set; } = new();
        public List<AppointmentPatientOptionViewModel> Patients { get; set; } = new();

        // Slots filled after doctor+date selected (server-side or AJAX)
        public List<AppointmentSlotOptionViewModel> AvailableSlots { get; set; } = new();

        // ----- Extra display fields (not required by DB) -----
        // Needed because controller/view shows selected doctor's name on Confirm page
        public string DoctorName { get; set; } = string.Empty;

        // ----- User input -----

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorProfileId { get; set; }

        // ✅ IMPORTANT CHANGE:
        // This is now nullable because receptionist booking does NOT require a registered patient.
        // Patient role will still set this automatically in controller POST.
        [Display(Name = "Patient")]
        public int? PatientProfileId { get; set; }

        // ✅ NEW: receptionist typed patient name (unregistered patient)
        [StringLength(120)]
        [Display(Name = "Patient Name")]
        public string? UnregisteredPatientName { get; set; }

        // Date only (UI picks date, then slot time separately)
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime? AppointmentDate { get; set; }

        // Time slot chosen from list, ex: "08:00"
        [Required(ErrorMessage = "Please select a time slot.")]
        [Display(Name = "Time Slot")]
        public string? SelectedSlot { get; set; }

        // Optional fields (matches your Appointment model)
        // NOTE: In your controller we can override this with doctor schedule duration
        [Range(5, 480)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 20;

        [StringLength(50)]
        [Display(Name = "Visit Type")]
        public string? VisitType { get; set; }

        [Display(Name = "First Visit")]
        public bool IsFirstVisit { get; set; } = false;

        // If you want to keep default workflow consistent
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    }

    // =========================
    // 5) Index/List item VM (optional, for listing appointments)
    // =========================
    public class AppointmentListItemViewModel
    {
        public int Id { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        // This will be filled from Patient profile name OR UnregisteredPatientName
        public string PatientName { get; set; } = string.Empty;

        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }

        public AppointmentStatus Status { get; set; }
        public bool IsActive { get; set; }
    }
}
