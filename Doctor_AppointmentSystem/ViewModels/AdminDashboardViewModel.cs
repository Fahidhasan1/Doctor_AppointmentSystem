//using System;

//namespace Doctor_AppointmentSystem.ViewModels
//{
//    public class AdminDashboardViewModel
//    {
//        public int TotalAdmins { get; set; }
//        public int TotalDoctors { get; set; }
//        public int TotalReceptionists { get; set; }
//        public int TotalPatients { get; set; }
//        public int TotalSpecialties { get; set; }
//        public int TotalAppointments { get; set; }
//        public int TodaysAppointments { get; set; }
//        public decimal MonthlyRevenue { get; set; }
//    }
//}


using System;
using System.Collections.Generic;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        // ===== Existing summary cards =====
        public int TotalAdmins { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalReceptionists { get; set; }
        public int TotalPatients { get; set; }
        public int TotalSpecialties { get; set; }
        public int TotalAppointments { get; set; }
        public int TodaysAppointments { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // ===== NEW: Chart data (dynamic graphs) =====

        /// <summary>
        /// Month labels (Jan, Feb, Mar, ...)
        /// Used by both Revenue & Appointment charts
        /// </summary>
        public List<string> MonthLabels { get; set; } = new();

        /// <summary>
        /// Revenue per month (Paid payments only)
        /// Index aligned with MonthLabels
        /// </summary>
        public List<decimal> RevenueByMonth { get; set; } = new();

        /// <summary>
        /// Total appointments per month
        /// Index aligned with MonthLabels
        /// </summary>
        public List<int> AppointmentsByMonth { get; set; } = new();
    }
}
