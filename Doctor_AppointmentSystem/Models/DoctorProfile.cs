using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class DoctorProfile
    {
        [Key]
        public int Id { get; set; }

        // ================================
        //  LINK TO ASP.NET IDENTITY USER
        // ================================
        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        // =====================================
        //  DOCTOR BASIC PROFESSIONAL DETAILS
        // =====================================

        // BMDC / Medical License Number
        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; } = null!;

        // Educational degree and qualification
        [Required]
        [StringLength(150)]
        public string Qualification { get; set; } = null!;

        // Main designation example: Consultant Cardiologist
        [StringLength(150)]
        public string? Designation { get; set; }

        // Main specialization (for quick display/filter)
        [StringLength(100)]
        public string? PrimarySpecialty { get; set; }

        // Optional extra specialties text (can be used along with DoctorSpecialty many-to-many)
        [StringLength(250)]
        public string? OtherSpecialties { get; set; }

        // Years of experience (0–80)
        [Range(0, 80)]
        public int Experience { get; set; }

        // ==============================
        //  FEES & CONSULTATION DETAILS
        // ==============================
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999)]
        public decimal VisitCharge { get; set; }

        // Optional follow-up fee
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FollowUpCharge { get; set; }

        // Telemedicine / Video call service available?
        public bool IsTelemedicineAvailable { get; set; } = false;

        // Short bio/description
        [StringLength(1000)]
        public string? Description { get; set; }

        // ==============================
        //  HOSPITAL & APPOINTMENT LOGIC
        // ==============================

        [StringLength(20)]
        public string? RoomNo { get; set; }

        // Max appointments allowed per day
        [Range(0, 500)]
        public int MaxAppointmentsPerDay { get; set; }

        // Automatically accept appointments or require manual approval?
        public bool AutoAcceptAppointments { get; set; } = true;

        // Is this doctor currently available for booking?
        public bool IsAvailable { get; set; } = true;

        // ==============================
        //  RATING
        // ==============================
        [Range(0, 5)]
        public double? Rating { get; set; }

        public int? TotalReviews { get; set; }

        // ==================================
        //  NAVIGATION (FROM YOUR OLD MODEL)
        // ==================================
        public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
        public ICollection<DoctorUnavailability> Unavailabilities { get; set; } = new List<DoctorUnavailability>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // ==============================
        //  FULL AUDIT FIELDS
        // ==============================
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime? LastModifiedDate { get; set; }
        public string? LastModifiedByUserId { get; set; }

        [ForeignKey(nameof(LastModifiedByUserId))]
        public ApplicationUser? LastModifiedByUser { get; set; }
    }
}
