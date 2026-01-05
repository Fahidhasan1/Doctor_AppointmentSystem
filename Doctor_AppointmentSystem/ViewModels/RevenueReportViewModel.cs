using System;
using System.Collections.Generic;

namespace Doctor_AppointmentSystem.ViewModels
{
    public class RevenueReportViewModel
    {
        // filter mode: "month", "last7", "last15", "custom"
        public string Mode { get; set; } = "month";

        // month filter (UTC based)
        public int Year { get; set; }
        public int Month { get; set; }

        // custom filter (dates come from the UI as yyyy-MM-dd)
        public string? Start { get; set; }
        public string? End { get; set; }

        // calculated range actually used by query
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; } // inclusive display (controller can set exclusive internally)

        public List<RevenueReportRowViewModel> Rows { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class RevenueReportRowViewModel
    {
        public DateTime PaidAtUtc { get; set; }

        public int AppointmentId { get; set; }

        public string DoctorName { get; set; } = "—";
        public string PatientName { get; set; } = "—";

        public string Method { get; set; } = "—";
        public string Currency { get; set; } = "BDT";
        public decimal Amount { get; set; }
    }
}
