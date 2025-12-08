using System;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class ReceptionistDashboardViewModel
    {
        public int TodaysAppointments { get; set; }
        public int PendingCheckins { get; set; }
        public int CompletedToday { get; set; }
        public int CancelledToday { get; set; }

        public int PendingPaymentsToday { get; set; }
        public decimal CashCollectedToday { get; set; }

        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
    }
}
