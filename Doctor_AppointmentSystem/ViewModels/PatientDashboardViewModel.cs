using System;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class PatientDashboardViewModel
    {
        public int UpcomingAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int TotalAppointments { get; set; }

        public int PendingPayments { get; set; }
        public decimal TotalPaid { get; set; }

        public DateTime? NextAppointmentDate { get; set; }
        public DateTime? LastAppointmentDate { get; set; }

        public int NotificationCount { get; set; }
    }
}
