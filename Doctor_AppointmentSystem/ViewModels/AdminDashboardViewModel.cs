using System;
using System.Collections.Generic;


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

        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> RevenueByMonth { get; set; } = new(); // keep (don’t break anything)
        public List<int> AppointmentsByMonth { get; set; } = new();

        // ✅ NEW (rolling 12 months revenue)
        public List<string> RevenueMonthLabels { get; set; } = new();
        public List<decimal> RevenueLast12Months { get; set; } = new();
    }
}
