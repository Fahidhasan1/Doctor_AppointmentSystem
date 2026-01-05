using System;
using System.Collections.Generic;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class AdminAppointmentListItemViewModel
    {
        public int Id { get; set; }

        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }

        public string DoctorName { get; set; } = "—";
        public string DoctorPhone { get; set; } = "—";

        public string PatientName { get; set; } = "—";
        public string PatientPhone { get; set; } = "—";

        public AppointmentStatus Status { get; set; }

        public string PaymentDisplay { get; set; } = "Unpaid";
        public decimal? PaidAmount { get; set; }
        public string Currency { get; set; } = "BDT";
    }

    public class AdminAppointmentsIndexViewModel
    {
        public string Filter { get; set; } = "All";
        public List<AdminAppointmentListItemViewModel> Appointments { get; set; } = new();
    }
}
