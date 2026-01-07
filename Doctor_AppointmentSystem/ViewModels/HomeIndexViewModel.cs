using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class HomeIndexViewModel
    {
        // Filters
        public string DoctorNameFilter { get; set; }
        public int? SpecialtyIdFilter { get; set; }
        public string ExperienceFilter { get; set; }

        public List<SelectListItem> SpecialtyOptions { get; set; } = new();
        public List<SelectListItem> ExperienceOptions { get; set; } = new();

        // Doctor Cards
        public List<DoctorCardItem> Doctors { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalDoctors { get; set; }
        public int PageSize { get; set; }

        public class DoctorCardItem
        {
            public int DoctorProfileId { get; set; }
            public string FullName { get; set; }
            public string PrimarySpecialty { get; set; }
            public string ExperienceText { get; set; }
            public string ClinicInfo { get; set; }
            public string Qualification { get; set; }
            public string ProfileImagePath { get; set; }

            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
        }
    }
}
