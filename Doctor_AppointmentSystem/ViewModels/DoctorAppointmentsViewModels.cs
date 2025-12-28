using System;
using System.Collections.Generic;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class DoctorAppointmentListItemViewModel
    {
        public int Id { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }

        public string PatientName { get; set; } = "—";
        public string PatientPhone { get; set; } = "—";

        public AppointmentStatus Status { get; set; }

        // Payment display for table
        public string PaymentDisplay { get; set; } = "Unpaid";

        // ✅ For showing amount under Payment (per row)
        public decimal? PaidAmount { get; set; }
        public string Currency { get; set; } = "BDT";
    }

    public class DoctorAppointmentsIndexViewModel
    {
        public string Filter { get; set; } = "All";

        // ✅ For showing header title when opened from cards
        public string PageTitle { get; set; } = "Appointments";

        public List<DoctorAppointmentListItemViewModel> Appointments { get; set; } = new();
    }
}
