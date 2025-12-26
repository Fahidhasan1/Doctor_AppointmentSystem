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

        // ✅ Control what to show (for unregistered patient)
        public bool ShowPatientCode { get; set; } = true;
        public bool ShowPatientGender { get; set; } = true;

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
        public bool ShowIssuer { get; set; } = false;
        public string IssuerRole { get; set; } = "Receptionist";
        public string IssuedBy { get; set; } = "-";

        // ✅ NEW: receptionist ID + counter no
        public string IssuerId { get; set; } = "-";
        public string IssuerCounterNo { get; set; } = "-";

        // Note
        public string Note { get; set; } = "Printed for patient verification";
    }
}
