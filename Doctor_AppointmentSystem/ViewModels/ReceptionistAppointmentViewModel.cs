using System;
using System.Collections.Generic;

namespace Doctor_AppointmentSystem.ViewModels
{
    // =========================
    // ViewModel for single row
    // =========================
    public class ReceptionistAppointmentRowViewModel
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }

        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        // Payment display (simple)
        public bool IsPaid { get; set; }
        public string PaymentDisplay { get; set; } = "Unpaid";
        public decimal Amount { get; set; }

        // Action flags (for buttons)
        public bool CanShowActions { get; set; }
        public bool CanCollectCash { get; set; }
        public bool CanConfirmMobile { get; set; }
        public bool CanCancel { get; set; }
    }

    // =========================
    // ViewModel for Index page
    // =========================
    public class ReceptionistAppointmentViewModel
    {
        public string Filter { get; set; } = "all";

        public List<ReceptionistAppointmentRowViewModel> Appointments { get; set; }
            = new List<ReceptionistAppointmentRowViewModel>();
    }
}
