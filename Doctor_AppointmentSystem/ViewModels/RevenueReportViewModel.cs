using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // ✅ Doctor filter
        public string? DoctorId { get; set; }              // Identity user id of doctor
        public List<SelectListItem> DoctorOptions { get; set; } = new(); // dropdown options

        // calculated range actually used by query
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; } // inclusive display (controller can set exclusive internally)

        public List<RevenueReportRowViewModel> Rows { get; set; } = new();
        public decimal Total { get; set; }
        public int? DoctorProfileId { get; set; }   // selected doctor filter
        public List<DoctorDropdownItemViewModel> Doctors { get; set; } = new(); // dropdown list

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

    public class DoctorDropdownItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "—";
    }

}
