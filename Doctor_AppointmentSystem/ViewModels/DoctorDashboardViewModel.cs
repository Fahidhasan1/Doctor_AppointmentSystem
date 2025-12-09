using System;
using System.Collections.Generic;
using Doctor_AppointmentSystem.Enums;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class DoctorDashboardViewModel
    {
        // ===== TOP CARDS =====
        public int TodaysAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int TotalPatientsTreated { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // (we still compute these but don't show card now)
        public int CompletedThisMonth { get; set; }
        public int CancelledThisMonth { get; set; }
        public int NoShowThisMonth { get; set; }

        // ===== MONTHLY REVENUE TREND CHART =====
        public List<string> RevenueMonthLabels { get; set; } = new();
        public List<decimal> RevenueMonthValues { get; set; } = new();

        // ===== TODAY STATUS DONUT =====
        public int TodayAcceptedCount { get; set; }     // Confirmed
        public int TodayRemainingCount { get; set; }    // Confirmed & time > now
        public int TodayCompletedCount { get; set; }    // Completed
        public int TodayCancelledCount { get; set; }    // Cancelled

        // ===== TODAY'S APPOINTMENTS TABLE =====
        public List<DoctorDashboardAppointmentRow> TodaysAppointmentsList { get; set; } = new();

        // ===== AVAILABILITY & OFF DAYS =====
        public List<DoctorDashboardSlotSummary> TodaySlots { get; set; } = new();
        public List<DoctorDashboardOffDaySummary> UpcomingOffDays { get; set; } = new();
    }

    public class DoctorDashboardAppointmentRow
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? VisitType { get; set; }
        public AppointmentStatus Status { get; set; }
    }

    public class DoctorDashboardSlotSummary
    {
        public string Label { get; set; } = string.Empty; // e.g. "Morning 09:00 – 11:30"
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int TotalSlots { get; set; }
        public int SlotsBooked { get; set; }
        public int SlotsRemaining => Math.Max(TotalSlots - SlotsBooked, 0);
    }

    public class DoctorDashboardOffDaySummary
    {
        public DateTime Date { get; set; }
        public string? Reason { get; set; }
    }
}
