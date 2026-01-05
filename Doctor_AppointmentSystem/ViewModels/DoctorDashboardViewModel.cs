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

        // ===== TODAY STATUS DONUT (UPDATED) =====
        // We will show: Completed, No Show, Remaining Today
        public int TodayRemainingCount { get; set; }    // Confirmed & time > now
        public int TodayCompletedCount { get; set; }    // Completed
        public int TodayNoShowCount { get; set; }       // NoShow

        // (keep if your old code still references it anywhere; safe)
        public int TodayAcceptedCount { get; set; }     // Confirmed
        public int TodayCancelledCount { get; set; }    // Cancelled (optional legacy)

        // ===== TODAY'S APPOINTMENTS TABLE (UPDATED) =====
        public List<DoctorDashboardAppointmentRow> TodaysAppointmentsList { get; set; } = new();

        // ===== AVAILABILITY & OFF DAYS =====
        public List<DoctorDashboardSlotSummary> TodaySlots { get; set; } = new();
        public List<DoctorDashboardOffDaySummary> UpcomingOffDays { get; set; } = new();
    }

    public class DoctorDashboardAppointmentRow
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDateTime { get; set; }

        public string PatientName { get; set; } = "—";
        public string PatientPhone { get; set; } = "—";

        public AppointmentStatus Status { get; set; }

        // Payment display on dashboard table
        public string PaymentDisplay { get; set; } = "Paid";
        public decimal? PaidAmount { get; set; }
        public string Currency { get; set; } = "BDT";

        // Optional: keep VisitType if you still use it elsewhere
        public string? VisitType { get; set; }
    }

    public class DoctorDashboardSlotSummary
    {
        public string Label { get; set; } = string.Empty; // e.g. "Morning 09:00 – 11:30"
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Total slots in the whole schedule block
        public int TotalSlots { get; set; }

        // Booked slots (you can use future-booked if you want dynamic)
        public int SlotsBooked { get; set; }

        // ✅ Make it dynamic: controller will calculate exact remaining slots
        public int SlotsRemaining { get; set; }
    }

    public class DoctorDashboardOffDaySummary
    {
        public DateTime Date { get; set; }
        public string? Reason { get; set; }
    }
}
