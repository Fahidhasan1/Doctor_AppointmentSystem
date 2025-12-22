using System;
using System.Collections.Generic;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class ReceptionistAppointmentRowViewModel
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        // registered patient OR receptionist typed name
        public string PatientName { get; set; } = string.Empty;

        public string StatusText { get; set; } = string.Empty;

        // Payment summary shown in table
        // Examples: "Unpaid", "Paid (Cash) ৳360", "Paid (Mobile Banking) ৳360"
        public string PaymentSummary { get; set; } = "Unpaid";

        public bool IsPaid { get; set; }
        public DateTime? PaidAtUtc { get; set; }

        // Action dropdown flags
        public bool CanShowActions { get; set; }
        public bool CanCollectCash { get; set; }
        public bool CanConfirmMobileBanking { get; set; }
        public bool CanCancel { get; set; }
    }

    public class ReceptionistAppointmentViewModel
    {
        // all / today / upcoming / cancelled
        public string Filter { get; set; } = "all";

        public List<ReceptionistAppointmentRowViewModel> Appointments { get; set; }
            = new List<ReceptionistAppointmentRowViewModel>();
    }
}
