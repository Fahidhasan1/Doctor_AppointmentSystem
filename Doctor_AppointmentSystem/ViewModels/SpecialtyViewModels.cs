using System.ComponentModel.DataAnnotations;

namespace Doctor_AppointmentSystem.ViewModels
{
    // For the list page
    public class SpecialtyListItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        // How many doctors are linked to this specialty
        public int DoctorCount { get; set; }
    }

    // For Create / Edit forms
    public class SpecialtyFormViewModel
    {
        public int? Id { get; set; }        // null = create

        [Required]
        [StringLength(100)]
        [Display(Name = "Specialty Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
