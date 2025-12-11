using System;
using System.Collections.Generic;
using Doctor_AppointmentSystem.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class ReceptionistDashboardViewModel
    {
        // ========= TOP CARDS =========
        public int TotalAppointments { get; set; }
        public int TodaysAppointments { get; set; }
        public decimal TodaysCollections { get; set; }
        public decimal MonthlyCollections { get; set; }

        /// <summary>
        /// For displaying "Wednesday, Dec 10, 2025" in the top-right corner.
        /// </summary>
        public string CurrentDateDisplay { get; set; } = string.Empty;

        // ========= FILTERS (Find Your Doctor) =========
        public string DoctorNameFilter { get; set; } = string.Empty;
        public int? SpecialtyIdFilter { get; set; }
        /// <summary>
        /// Experience bucket: "", "0-3", "4-7", "8+"
        /// </summary>
        public string ExperienceFilter { get; set; } = string.Empty;

        public List<SelectListItem> SpecialtyOptions { get; set; } = new();
        public List<SelectListItem> ExperienceOptions { get; set; } = new();

        // ========= DOCTOR CARDS =========
        public List<DoctorCardItem> Doctors { get; set; } = new();

        // ========= PAGINATION =========
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalDoctors { get; set; }

        // ========= TODAY'S APPOINTMENTS TABLE =========
        public List<TodaysAppointmentRow> TodaysAppointmentsList { get; set; } = new();

        // ========= ALERTS & NOTIFICATIONS =========
        public List<NotificationItem> AlertsAndNotifications { get; set; } = new();

        // ======================================================
        // Nested DTOs
        // ======================================================

        public class DoctorCardItem
        {
            public int DoctorProfileId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string PrimarySpecialty { get; set; } = string.Empty;
            public string ExperienceText { get; set; } = string.Empty;
            public string ClinicInfo { get; set; } = string.Empty;
            public string Bio { get; set; } = string.Empty;
            public string ProfileImagePath { get; set; } = string.Empty;
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
        }

        public class TodaysAppointmentRow
        {
            public int AppointmentId { get; set; }
            public DateTime AppointmentDateTime { get; set; }

            public string PatientName { get; set; } = string.Empty;
            public string DoctorName { get; set; } = string.Empty;

            public AppointmentStatus Status { get; set; }

            public PaymentStatus PaymentStatus { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }

            /// <summary>
            /// e.g. "Paid (Card)", "Paid (bKash)", "Unpaid".
            /// </summary>
            public string PaymentDisplay { get; set; } = string.Empty;
        }

        public class NotificationItem
        {
            public int NotificationId { get; set; }
            public DateTime CreatedAt { get; set; }

            /// <summary>Short bold title.</summary>
            public string Title { get; set; } = string.Empty;

            /// <summary>Longer line describing the notification.</summary>
            public string? Message { get; set; }

            /// <summary>Optional meta line, e.g. channel or extra info.</summary>
            public string? Meta { get; set; }

            /// <summary>Text shown inside the colored pill.</summary>
            public string BadgeText { get; set; } = string.Empty;

            /// <summary>CSS class for the badge.</summary>
            public string BadgeCssClass { get; set; } = string.Empty;

            public bool IsUnread { get; set; }
        }
    }
}
