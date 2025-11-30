using Doctor_AppointmentSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Doctor_AppointmentSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Profiles
        public DbSet<AdminProfile> AdminProfiles { get; set; } = null!;
        public DbSet<DoctorProfile> DoctorProfiles { get; set; } = null!;
        public DbSet<ReceptionistProfile> ReceptionistProfiles { get; set; } = null!;
        public DbSet<PatientProfile> PatientProfiles { get; set; } = null!;

        // Doctor & Specialties
        public DbSet<Specialty> Specialties { get; set; } = null!;
        public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; } = null!;
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; } = null!;
        public DbSet<DoctorUnavailability> DoctorUnavailabilities { get; set; } = null!;

        // Core domain
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<DoctorReview> DoctorReviews { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Appointment -> DoctorProfile (NO CASCADE)
            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment -> PatientProfile (NO CASCADE)
            builder.Entity<Appointment>()
                .HasOne<PatientProfile>()
                .WithMany()
                .HasForeignKey(a => a.PatientProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorReview -> DoctorProfile (NO CASCADE)
            builder.Entity<DoctorReview>()
                .HasOne<DoctorProfile>()
                .WithMany(d => d.Reviews)
                .HasForeignKey(r => r.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorReview -> PatientProfile (NO CASCADE)
            builder.Entity<DoctorReview>()
                .HasOne<PatientProfile>()
                .WithMany()
                .HasForeignKey(r => r.PatientProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorReview -> Appointment (if appointment deleted, just null link)
            builder.Entity<DoctorReview>()
                .HasOne<Appointment>()
                .WithMany()
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Payment -> Appointment (NO CASCADE)
            builder.Entity<Payment>()
                .HasOne(p => p.Appointment)
                .WithMany()
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification -> Appointment (if appointment deleted, keep log, set null)
            builder.Entity<Notification>()
                .HasOne<Appointment>()
                .WithMany()
                .HasForeignKey(n => n.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // DoctorSchedule -> DoctorProfile (NO CASCADE)
            builder.Entity<DoctorSchedule>()
                .HasOne(s => s.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(s => s.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorUnavailability -> DoctorProfile (NO CASCADE)
            builder.Entity<DoctorUnavailability>()
                .HasOne(u => u.Doctor)
                .WithMany(d => d.Unavailabilities)
                .HasForeignKey(u => u.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorSpecialty -> Specialty (NO CASCADE delete of specialties)
            builder.Entity<DoctorSpecialty>()
                .HasOne(ds => ds.Specialty)
                .WithMany()
                .HasForeignKey(ds => ds.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
