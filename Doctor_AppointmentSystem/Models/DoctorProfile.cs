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

        // Link to Identity User
        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        // Professional Details
        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Qualification { get; set; } = null!;

        [StringLength(150)]
        public string? Designation { get; set; }

        [Range(0, 80)]
        public int Experience { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? RoomNo { get; set; }

        // Consultation Fees
        [Column(TypeName = "decimal(18,2)")]
        public decimal VisitCharge { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FollowUpCharge { get; set; }

        public double? Rating { get; set; }
        public int? TotalReviews { get; set; }


        // Availability Logic
        public bool IsTelemedicineAvailable { get; set; } = false;
        public int MaxAppointmentsPerDay { get; set; }
        public bool AutoAcceptAppointments { get; set; } = true;
        public bool IsAvailable { get; set; } = true;

        // Navigation
        public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
        public ICollection<DoctorUnavailability> Unavailabilities { get; set; } = new List<DoctorUnavailability>();
        public ICollection<DoctorReview> Reviews { get; set; } = new List<DoctorReview>();

        // Audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? LastModifiedByUserId { get; set; }
    }
}
