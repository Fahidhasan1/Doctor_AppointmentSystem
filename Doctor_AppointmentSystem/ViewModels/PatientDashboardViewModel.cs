using System;
using System.Collections.Generic;
using Doctor_AppointmentSystem.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class PatientDashboardViewModel
    {
        // ====== PATIENT STATS (TOP CARDS) ======
        public int UpcomingAppointments { get; set; }          // within next 7 days
        public int CompletedVisits { get; set; }               // all-time completed
        public int CancelledOrMissed { get; set; }             // cancelled + no-show
        public decimal DigitalPaymentsTotal { get; set; }      // non-cash paid amount

        // Extra stats (useful later if needed)
        public int TotalAppointments { get; set; }
        public int PendingPaymentsCount { get; set; }
        public DateTime? NextAppointmentDate { get; set; }
        public DateTime? LastAppointmentDate { get; set; }

        // Sidebar badges
        public int MyAppointmentsBadge { get; set; }
        public int NotificationBadge { get; set; }

        // ====== DOCTOR SEARCH / FILTER BAR ======
        public string? DoctorNameFilter { get; set; }
        public int? SpecialtyIdFilter { get; set; }
        public string? ExperienceFilter { get; set; }          // "0-3", "4-7", "8+"

        public IEnumerable<SelectListItem> SpecialtyOptions { get; set; }
            = new List<SelectListItem>();

        public IEnumerable<SelectListItem> ExperienceOptions { get; set; }
            = new List<SelectListItem>();

        // ====== DOCTOR CARDS GRID ("Find Your Doctor") ======
        public IList<DoctorCardItem> Doctors { get; set; }
            = new List<DoctorCardItem>();

        // Pagination for doctor cards
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalDoctors { get; set; }
        public int PageSize { get; set; }

        // ====== MY APPOINTMENTS PREVIEW (TABLE WITH TABS) ======
        public AppointmentFilterType AppointmentFilter { get; set; }
            = AppointmentFilterType.All;

        public IList<AppointmentListItem> Appointments { get; set; }
            = new List<AppointmentListItem>();

        // ====== NOTIFICATIONS & REMINDERS PREVIEW ======
        public IList<NotificationListItem> Notifications { get; set; }
            = new List<NotificationListItem>();

        // ================== NESTED VIEW-MODEL CLASSES ==================

        // Doctor card in "Find Your Doctor" grid
        public class DoctorCardItem
        {
            public int DoctorProfileId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string PrimarySpecialty { get; set; } = string.Empty;
            public string ExperienceText { get; set; } = string.Empty;      // "7+ years experience"
            public string? ClinicInfo { get; set; }                         // e.g. hospital / room
            public string? Bio { get; set; }
            public string? ProfileImagePath { get; set; }                   // null => use initial avatar
            public double AverageRating { get; set; }                       // 0.0–5.0
            public int ReviewCount { get; set; }                            // "(0 reviews)"
        }

        // Row in "My Appointments" table
        public class AppointmentListItem
        {
            public int AppointmentId { get; set; }
            public string DoctorName { get; set; } = string.Empty;
            public string SpecialtyName { get; set; } = string.Empty;
            public DateTime AppointmentDateTime { get; set; }

            public AppointmentStatus Status { get; set; }

            public PaymentStatus PaymentStatus { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }
            public decimal Amount { get; set; }
            public bool IsRefunded { get; set; }

            // Convenience flags for actions column
            public bool CanPayNow { get; set; }
            public bool CanViewReceipt { get; set; }
            public bool CanDownloadReceipt { get; set; }
            public bool CanViewDetails { get; set; }
        }

        // Item in "Notifications & Reminders"
        public class NotificationListItem
        {
            public int NotificationId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }

            public NotificationStatus Status { get; set; }      // New / Viewed etc.
            public bool IsImportant { get; set; }               // for red "Important" badge
        }
    }
}
