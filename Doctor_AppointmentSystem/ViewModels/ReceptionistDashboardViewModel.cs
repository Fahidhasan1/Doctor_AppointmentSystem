using System;
using System.Collections.Generic;
using Doctor_AppointmentSystem.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class ReceptionistDashboardViewModel
    {
        // ==========================
        // DASHBOARD TOP CARDS
        // ==========================

        public int TotalAppointments { get; set; }
        public int TodaysAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int CancelledAppointments { get; set; }

        public decimal TodaysCollections { get; set; }
        public decimal MonthlyCollections { get; set; }
        public string CurrentDateDisplay { get; set; }

        // ==========================
        // FILTERS (Doctor search)
        // ==========================

        public string? DoctorNameFilter { get; set; }
        public int? SpecialtyIdFilter { get; set; }
        public string? ExperienceFilter { get; set; }

        public List<SelectListItem> SpecialtyOptions { get; set; } = new();
        public List<SelectListItem> ExperienceOptions { get; set; } = new();

        // ==========================
        // DOCTOR CARDS
        // ==========================

        public List<DoctorCardItem> Doctors { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalDoctors { get; set; }

        // ==========================
        // TODAY'S APPOINTMENTS TABLE
        // ==========================

        public List<TodaysAppointmentRow> TodaysAppointmentsList { get; set; } = new();

        // ==========================
        // ALERTS & NOTIFICATIONS
        // ==========================

        public List<NotificationItem> AlertsAndNotifications { get; set; } = new();

        // =====================================================
        // NESTED CLASSES
        // =====================================================

        public class DoctorCardItem
        {
            public int DoctorProfileId { get; set; }
            public string FullName { get; set; }
            public string PrimarySpecialty { get; set; }
            public string ExperienceText { get; set; }
            public string ClinicInfo { get; set; }
            public string Qualification { get; set; }
            public string Bio { get; set; }
            public string ProfileImagePath { get; set; }
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
        }

        public class TodaysAppointmentRow
        {
            public int AppointmentId { get; set; }
            public DateTime AppointmentDateTime { get; set; }

            public string PatientName { get; set; }
            public string DoctorName { get; set; }

            public AppointmentStatus Status { get; set; }

            public PaymentStatus PaymentStatus { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }

            public string PaymentDisplay { get; set; }
        }

        public class NotificationItem
        {
            public int NotificationId { get; set; }
            public DateTime CreatedAt { get; set; }

            public string Title { get; set; }
            public string Message { get; set; }

            public string Meta { get; set; }

            public string BadgeText { get; set; }
            public string BadgeCssClass { get; set; }

            public bool IsUnread { get; set; }
        }
    }
}
