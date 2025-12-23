using System;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class PatientReceiptViewModel
    {
        // Header
        public string HospitalName { get; set; } = "Sunshine Hospital";
        public string ReceiptNo { get; set; } = "-";
        public DateTime IssuedAt { get; set; }

        // Patient
        public string PatientName { get; set; } = "-";
        public string PatientCode { get; set; } = "-";
        public string PatientPhone { get; set; } = "-";
        public string PatientGender { get; set; } = "-";

        // Appointment
        public string DoctorName { get; set; } = "-";
        public string Specialty { get; set; } = "-";
        public string RoomNo { get; set; } = "-";
        public DateTime AppointmentDateTime { get; set; }

        // Payment
        public decimal Amount { get; set; }
        public string Method { get; set; } = "-";
        public string ProviderName { get; set; } = "-";
        public string TransactionId { get; set; } = "-";

        // Issuer (Receptionist)
        public bool ShowIssuer { get; set; }
        public string IssuedBy { get; set; } = "-";
        public string IssuerRole { get; set; } = "-";
    }
}
