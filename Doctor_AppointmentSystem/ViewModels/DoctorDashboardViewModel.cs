namespace Doctor_AppointmentSystem.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public int TodaysAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int CompletedThisMonth { get; set; }
        public int CancelledThisMonth { get; set; }
        public int NoShowThisMonth { get; set; }
        public int TotalPatientsTreated { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
