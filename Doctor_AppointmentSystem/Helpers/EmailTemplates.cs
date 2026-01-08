using System;

namespace Doctor_AppointmentSystem.Helpers
{
    public static class EmailTemplates
    {
        public static string AppointmentPaidPatient(string patientName, string doctorName, DateTime apptTime)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                  <h2>Payment Successful ✅</h2>
                  <p>Hi <b>{patientName}</b>,</p>
                  <p>Congratulations! Your appointment has been confirmed.</p>
                  <p><b>Doctor:</b> {doctorName}<br/>
                     <b>Time:</b> {apptTime:dddd, MMMM dd, yyyy hh:mm tt}
                  </p>
                  <p>Please arrive 10 minutes early.</p>
                  <p>— Doctor Appointment System</p>
                </div>";
        }

        public static string AppointmentPaidDoctor(string doctorName, string patientName, DateTime apptTime, string paidVia)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                  <h2>Appointment Confirmed ✅</h2>
                  <p>Hi <b>{doctorName}</b>,</p>
                  <p>A paid appointment has been confirmed.</p>
                  <p><b>Patient:</b> {patientName}<br/>
                     <b>Time:</b> {apptTime:dddd, MMMM dd, yyyy hh:mm tt}<br/>
                     <b>Payment:</b> {paidVia}
                  </p>
                  <p>— Doctor Appointment System</p>
                </div>";
        }

        public static string AppointmentPaidReceptionist(string receptionistName, string patientName, string doctorName, DateTime apptTime, string method)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                  <h2>Payment Recorded ✅</h2>
                  <p>Hi <b>{receptionistName}</b>,</p>
                  <p>You confirmed a payment for an appointment.</p>
                  <p><b>Patient:</b> {patientName}<br/>
                     <b>Doctor:</b> {doctorName}<br/>
                     <b>Time:</b> {apptTime:dddd, MMMM dd, yyyy hh:mm tt}<br/>
                     <b>Method:</b> {method}
                  </p>
                  <p>— Doctor Appointment System</p>
                </div>";
        }
    }
}
