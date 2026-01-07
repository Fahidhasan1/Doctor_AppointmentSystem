using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class DoctorRevenueReportViewModel
    {
        // Filters (same style as admin)
        public string RangeType { get; set; } = "Month"; // Month / Last7 / Last15 / Custom

        [Range(2000, 2100)]
        public int Year { get; set; } = DateTime.Today.Year;

        // 1-12
        [Range(1, 12)]
        public int Month { get; set; } = DateTime.Today.Month;

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // Display range label like: "Range: 01 Jan 2026 – 31 Jan 2026"
        public string RangeLabel { get; set; } = "";

        // Rows
        public List<DoctorRevenueRowViewModel> Rows { get; set; } = new();

        // Summary
        public decimal TotalRevenue { get; set; }
    }

    public class DoctorRevenueRowViewModel
    {
        public DateTime Date { get; set; }

        public string PatientName { get; set; } = "-";

        public string PatientPhone { get; set; } = "-";

        public string PaymentMethod { get; set; } = "-";

        public decimal Amount { get; set; }
    }
}
