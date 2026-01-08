namespace Doctor_AppointmentSystem.Models
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;

        // Gmail uses STARTTLS on port 587
        public bool UseStartTls { get; set; } = true;

        // Display name on outgoing emails
        public string SenderName { get; set; } = "Doctor Appointment System";

        // Gmail address used to send mail
        public string SenderEmail { get; set; } = string.Empty;

        // Usually same as SenderEmail for Gmail SMTP
        public string Username { get; set; } = string.Empty;

        // Store in User Secrets / ENV (not in appsettings.json)
        public string Password { get; set; } = string.Empty;
    }
}
