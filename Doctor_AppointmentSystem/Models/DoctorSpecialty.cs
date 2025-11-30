using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class DoctorSpecialty
    {
        [Key]
        public int Id { get; set; }

        // Link to DoctorProfile
        [Required]
        public int DoctorProfileId { get; set; }

        [ForeignKey(nameof(DoctorProfileId))]
        public DoctorProfile DoctorProfile { get; set; } = null!;

        // Link to Specialty
        [Required]
        public int SpecialtyId { get; set; }

        [ForeignKey(nameof(SpecialtyId))]
        public Specialty Specialty { get; set; } = null!;

        // Optional: mark the primary specialty
        public bool IsPrimary { get; set; } = false;
    }
}
