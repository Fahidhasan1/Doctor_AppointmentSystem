using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class DoctorUnavailabilityListItemViewModel
    {
        public int Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public string? Reason { get; set; }
    }

    public class DoctorUnavailabilityIndexViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime? UnavailableDate { get; set; }

        [StringLength(200)]
        public string? Reason { get; set; }

        public List<DoctorUnavailabilityListItemViewModel> Items { get; set; }
            = new List<DoctorUnavailabilityListItemViewModel>();
    }
}
