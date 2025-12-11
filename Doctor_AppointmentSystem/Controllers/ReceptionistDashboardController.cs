using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doctor_AppointmentSystem.Data;
using Doctor_AppointmentSystem.Enums;
using Doctor_AppointmentSystem.Models;
using Doctor_AppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 8;

        public ReceptionistDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ReceptionistDashboard
        public async Task<IActionResult> Index(
            string? doctorName,
            int? specialtyId,
            string? experience,
            int page = 1)
        {
            // ==========================
            // 1. TOP-CARD STATS
            // ==========================

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var totalAppointments = await _context.Appointments
                .Where(a => a.IsActive)
                .CountAsync();

            var todaysAppointmentsCount = await _context.Appointments
                .Where(a => a.IsActive &&
                            a.AppointmentDateTime >= today &&
                            a.AppointmentDateTime < tomorrow)
                .CountAsync();

            var todayStartUtc = DateTime.SpecifyKind(today, DateTimeKind.Local).ToUniversalTime();
            var tomorrowStartUtc = DateTime.SpecifyKind(tomorrow, DateTimeKind.Local).ToUniversalTime();
            var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Local).ToUniversalTime();
            var nextMonthStartUtc = DateTime.SpecifyKind(nextMonthStart, DateTimeKind.Local).ToUniversalTime();

            var todaysCollections = await _context.Payments
                .Where(p => p.IsActive &&
                            p.Status == PaymentStatus.Paid &&
                            p.PaidAtUtc >= todayStartUtc &&
                            p.PaidAtUtc < tomorrowStartUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var monthlyCollections = await _context.Payments
                .Where(p => p.IsActive &&
                            p.Status == PaymentStatus.Paid &&
                            p.PaidAtUtc >= monthStartUtc &&
                            p.PaidAtUtc < nextMonthStartUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // ==========================
            // 2. FILTER OPTIONS
            // ==========================

            var specialtyOptions = await _context.Specialties
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name,
                    Selected = specialtyId.HasValue && specialtyId.Value == s.Id
                })
                .ToListAsync();

            specialtyOptions.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "All Specialties",
                Selected = !specialtyId.HasValue
            });

            var experienceOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "",    Text = "Any Experience", Selected = string.IsNullOrWhiteSpace(experience) },
                new SelectListItem { Value = "0-3", Text = "0–3 years",      Selected = experience == "0-3" },
                new SelectListItem { Value = "4-7", Text = "4–7 years",      Selected = experience == "4-7" },
                new SelectListItem { Value = "8+",  Text = "8+ years",       Selected = experience == "8+" }
            };

            // ==========================
            // 3. DOCTOR QUERY + CARDS
            // ==========================

            var doctorsQuery = _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.DoctorSpecialties)
                    .ThenInclude(ds => ds.Specialty)
                .Where(d => d.IsActive && d.User.IsActive);

            if (!string.IsNullOrWhiteSpace(doctorName))
            {
                var term = doctorName.Trim().ToLower();
                doctorsQuery = doctorsQuery.Where(d =>
                    (d.User.FirstName + " " + d.User.LastName)
                        .ToLower()
                        .Contains(term));
            }

            if (specialtyId.HasValue && specialtyId.Value > 0)
            {
                doctorsQuery = doctorsQuery.Where(d =>
                    d.DoctorSpecialties.Any(ds => ds.SpecialtyId == specialtyId.Value));
            }

            if (!string.IsNullOrWhiteSpace(experience))
            {
                switch (experience)
                {
                    case "0-3":
                        doctorsQuery = doctorsQuery.Where(d => d.Experience <= 3);
                        break;
                    case "4-7":
                        doctorsQuery = doctorsQuery.Where(d => d.Experience >= 4 && d.Experience <= 7);
                        break;
                    case "8+":
                        doctorsQuery = doctorsQuery.Where(d => d.Experience >= 8);
                        break;
                }
            }

            var totalDoctors = await doctorsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalDoctors / (double)PageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var doctors = await doctorsQuery
                .OrderBy(d => d.User.FirstName)
                .ThenBy(d => d.User.LastName)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(d => new ReceptionistDashboardViewModel.DoctorCardItem
                {
                    DoctorProfileId = d.Id,
                    FullName = d.User.FirstName + " " + d.User.LastName,
                    PrimarySpecialty =
                        d.DoctorSpecialties
                            .Where(ds => ds.IsPrimary)
                            .Select(ds => ds.Specialty.Name)
                            .FirstOrDefault()
                        ?? d.DoctorSpecialties
                            .Select(ds => ds.Specialty.Name)
                            .FirstOrDefault()
                        ?? "General Physician",
                    ExperienceText = d.Experience > 0
                        ? $"{d.Experience}+ years experience"
                        : "Experience not set",
                    ClinicInfo = string.IsNullOrWhiteSpace(d.RoomNo)
                        ? "Room not set"
                        : $"Room {d.RoomNo}",
                    Bio = d.Description ?? string.Empty,
                    ProfileImagePath = d.User.ProfileImagePath ?? string.Empty,
                    AverageRating = d.Rating ?? 0.0,
                    ReviewCount = d.TotalReviews ?? 0
                })
                .ToListAsync();

            // ==========================
            // 4. TODAY'S APPOINTMENTS
            // ==========================

            var todaysAppointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Where(a => a.IsActive &&
                            a.AppointmentDateTime >= today &&
                            a.AppointmentDateTime < tomorrow)
                .OrderBy(a => a.AppointmentDateTime);

            var todaysAppointments = await todaysAppointmentsQuery.ToListAsync();
            var todaysAppointmentIds = todaysAppointments.Select(a => a.Id).ToList();

            var latestPayments = await _context.Payments
                .Where(p => p.IsActive && todaysAppointmentIds.Contains(p.AppointmentId))
                .GroupBy(p => p.AppointmentId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                .ToListAsync();

            var paymentsByAppointmentId = latestPayments
                .ToDictionary(p => p.AppointmentId, p => p);

            var todaysAppointmentRows = new List<ReceptionistDashboardViewModel.TodaysAppointmentRow>();

            foreach (var a in todaysAppointments)
            {
                paymentsByAppointmentId.TryGetValue(a.Id, out var payment);

                var paymentStatus = payment?.Status ?? PaymentStatus.Pending;
                PaymentMethod? paymentMethod = payment?.Method;

                var paymentDisplay = "Unpaid";
                if (payment != null)
                {
                    if (payment.Status == PaymentStatus.Paid)
                    {
                        var methodText = payment.Method switch
                        {
                            PaymentMethod.Cash => "Cash",
                            PaymentMethod.Bkash => "bKash",
                            PaymentMethod.Nagad => "Nagad",
                            PaymentMethod.Rocket => "Rocket",
                            _ => payment.Method.ToString()
                        };

                        paymentDisplay = $"Paid ({methodText})";
                    }
                    else
                    {
                        paymentDisplay = payment.Status.ToString();
                    }
                }

                todaysAppointmentRows.Add(new ReceptionistDashboardViewModel.TodaysAppointmentRow
                {
                    AppointmentId = a.Id,
                    AppointmentDateTime = a.AppointmentDateTime,
                    PatientName = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                    DoctorName = "Dr. " + a.Doctor.User.FirstName + " " + a.Doctor.User.LastName,
                    Status = a.Status,
                    PaymentStatus = paymentStatus,
                    PaymentMethod = paymentMethod,
                    PaymentDisplay = paymentDisplay
                });
            }

            // ==========================
            // 5. ALERTS & NOTIFICATIONS
            // ==========================

            var latestNotifications = await _context.Notifications
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var alerts = latestNotifications
                .Select(n =>
                {
                    var item = new ReceptionistDashboardViewModel.NotificationItem
                    {
                        NotificationId = n.Id,
                        CreatedAt = n.CreatedAt,
                        Title = n.Subject ?? (n.Channel == NotificationChannel.Email
                            ? "Email notification"
                            : "SMS notification"),
                        Message = n.MessageBody,
                        Meta = n.Channel.ToString()
                    };

                    if (n.Status == NotificationStatus.Failed)
                    {
                        item.BadgeText = "Important";
                        item.BadgeCssClass = "badge-important";
                        item.IsUnread = true;
                    }
                    else if (n.Status == NotificationStatus.Pending)
                    {
                        item.BadgeText = "Pending";
                        item.BadgeCssClass = "badge-pending";
                        item.IsUnread = true;
                    }
                    else // Sent
                    {
                        if (n.CreatedAt.Date == today)
                        {
                            item.BadgeText = "Today";
                            item.BadgeCssClass = "badge-today";
                        }
                        else
                        {
                            item.BadgeText = "Viewed";
                            item.BadgeCssClass = "badge-viewed";
                        }

                        item.IsUnread = false;
                    }

                    return item;
                })
                .ToList();

            // ==========================
            // 6. BUILD VIEWMODEL
            // ==========================

            var vm = new ReceptionistDashboardViewModel
            {
                TotalAppointments = totalAppointments,
                TodaysAppointments = todaysAppointmentsCount,
                TodaysCollections = todaysCollections,
                MonthlyCollections = monthlyCollections,
                CurrentDateDisplay = today.ToString("dddd, MMM dd, yyyy"),

                DoctorNameFilter = doctorName,
                SpecialtyIdFilter = specialtyId,
                ExperienceFilter = experience,
                SpecialtyOptions = specialtyOptions,
                ExperienceOptions = experienceOptions,
                Doctors = doctors,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalDoctors = totalDoctors,

                TodaysAppointmentsList = todaysAppointmentRows,
                AlertsAndNotifications = alerts
            };

            return View(vm);
        }
    }
}
