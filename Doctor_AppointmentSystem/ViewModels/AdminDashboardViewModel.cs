using System;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalAdmins { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalReceptionists { get; set; }
        public int TotalPatients { get; set; }
        public int TotalSpecialties { get; set; }
        public int TotalAppointments { get; set; }
        public int TodaysAppointments { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }
}
