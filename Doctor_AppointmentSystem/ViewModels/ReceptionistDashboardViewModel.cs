namespace Doctor_AppointmentSystem.ViewModels
{
    public class ReceptionistDashboardViewModel
    {
        public int TotalAppointments { get; set; }
        public int TodaysAppointments { get; set; }
        public decimal TodaysCollections { get; set; }
        public decimal MonthlyCollections { get; set; }
    }
}
