using System;
using System.Collections.Generic;

namespace Doctor_AppointmentSystem.ViewModels
{
    /// <summary>
    /// Single patient row in "Patients Treated" list
    /// </summary>
    public class DoctorPatientsTreatedRowViewModel
    {
        // Registered or Unregistered
        public bool IsRegistered { get; set; }

        // For registered patients
        public int? PatientProfileId { get; set; }

        // Display fields
        public string PatientName { get; set; } = "—";
        public string PatientPhone { get; set; } = "—";

        // Stats
        public int VisitsCount { get; set; }
        public DateTime LastTreatedAt { get; set; }
    }

    /// <summary>
    /// Index page VM for Patients Treated
    /// </summary>
    public class DoctorPatientsTreatedIndexViewModel
    {
        // Page meta
        public string PageTitle { get; set; } = "Patients Treated";
        public bool FromCard { get; set; }

        // Summary
        public int TotalPatientsTreated { get; set; }

        // Table rows
        public List<DoctorPatientsTreatedRowViewModel> Patients { get; set; }
            = new List<DoctorPatientsTreatedRowViewModel>();
    }
}
