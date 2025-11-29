using System.Collections.Generic;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Top-left admin info
        public string AdminFullName { get; set; } = "Admin User";
        public string AdminInitial { get; set; } = "A";

        // Top stat cards
        public int TotalAdmins { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalReceptionists { get; set; }
        public int TotalPatients { get; set; }
        public int TotalSpecialties { get; set; }
        public int TotalAppointments { get; set; }
        public int TodaysAppointments { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // Chart data
        public List<decimal> MonthlyRevenueSeries { get; set; } = new();
        public List<int> MonthlyAppointmentSeries { get; set; } = new();

        // Table & activity data
        public List<DashboardAppointmentRow> TodaysAppointmentsList { get; set; } = new();
        public List<DashboardActivityItem> RecentActivities { get; set; } = new();
    }

    public class DashboardAppointmentRow
    {
        public string Time { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string StatusText { get; set; } = "";      // e.g. "Accepted"
        public string StatusCssClass { get; set; } = "";  // e.g. "pill-status--accepted"
    }

    public class DashboardActivityItem
    {
        public string Text { get; set; } = "";
        public string TimeAgo { get; set; } = "";
        public string DotCssClass { get; set; } = "";     // e.g. "", "activity-dot--orange"
    }
}
