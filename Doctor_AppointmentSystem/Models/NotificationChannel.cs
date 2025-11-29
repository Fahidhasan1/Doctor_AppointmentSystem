namespace Doctor_AppointmentSystem.Models
{
    public enum NotificationChannel
    {
        Email = 0,
        Sms = 1,
        Push = 2,      // for future, if you add app notifications
        InApp = 3      // for dashboard/in-system alerts
    }
}
