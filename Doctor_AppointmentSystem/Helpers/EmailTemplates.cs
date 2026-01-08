using System;

namespace Doctor_AppointmentSystem.Helpers
{
    public static class EmailTemplates
    {
        // =========================
        // PATIENT EMAIL
        // =========================
        public static string AppointmentPaidPatient(string patientName, string doctorName, DateTime apptTime)
        {
            return $@"
<div style='font-family: Arial, sans-serif; line-height: 1.6;'>
  <h2>Appointment Confirmed</h2>

  <p>Hello <strong>{patientName}</strong>,</p>

  <p>Your appointment has been successfully confirmed and payment has been received.</p>

  <p>
    <strong>Doctor:</strong> {doctorName}<br/>
    <strong>Date & Time:</strong> {apptTime:dddd, MMMM dd, yyyy hh:mm tt}
  </p>

  <p>Please arrive at least 10 minutes before your scheduled time.</p>

  <p>
    —<br/>
    Doctor Appointment System
  </p>
</div>";
        }

        // =========================
        // DOCTOR EMAIL
        // =========================
        public static string AppointmentPaidDoctor(string doctorName, string patientName, DateTime apptTime, string paidVia)
        {
            return $@"
<div style='font-family: Arial, sans-serif; line-height: 1.6;'>
  <h2>New Appointment Confirmed</h2>

  <p>Hello <strong>Dr. {doctorName}</strong>,</p>

  <p>A new appointment has been confirmed and payment has been received.</p>

  <p>
    <strong>Patient:</strong> {patientName}<br/>
    <strong>Date & Time:</strong> {apptTime:dddd, MMMM dd, yyyy hh:mm tt}<br/>
    <strong>Payment Method:</strong> {paidVia}
  </p>

  <p>Please review your schedule accordingly.</p>

  <p>
    —<br/>
    Doctor Appointment System
  </p>
</div>";
        }

        // =========================
        // RECEPTIONIST EMAIL (UPDATED – Alternative 2 + extra line)
        // =========================
        public static string AppointmentPaidReceptionist(
            string receptionistName,
            string patientName,
            string doctorName,
            DateTime apptTime,
            string method)
        {
            return $@"
<div style='font-family: Arial, sans-serif; line-height: 1.6;'>
  <h2>Payment Recorded Successfully</h2>

  <p>Hello <strong>{receptionistName}</strong>,</p>

  <p>
    This is to confirm that the payment for the following appointment has been recorded successfully:
  </p>

  <p>
    <strong>Patient:</strong> {patientName}<br/>
    <strong>Doctor:</strong> {doctorName}<br/>
    <strong>Date & Time:</strong> {apptTime:dddd, MMMM dd, yyyy hh:mm tt}<br/>
    <strong>Payment Method:</strong> {method}
  </p>

  <p>
    Please ensure the patient is informed and the appointment proceeds as scheduled.
  </p>

  <p>
    —<br/>
    Doctor Appointment System
  </p>
</div>";
        }
    }
}
