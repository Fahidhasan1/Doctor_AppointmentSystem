using System;
using System.Collections.Generic;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class PatientAppointmentListItemViewModel
    {
        public int Id { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }
        public string? VisitType { get; set; }
        public AppointmentStatus Status { get; set; }
        public string DoctorName { get; set; } = "";
        public string? SpecialtyName { get; set; }
        public bool IsPaid { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? PaymentMethod { get; set; }
       


    }

    public class PatientAppointmentsIndexViewModel
    {
        public string? Filter { get; set; }
        public bool FromCard { get; set; }
        public List<PatientAppointmentListItemViewModel> Appointments { get; set; } = new();
    }
}
