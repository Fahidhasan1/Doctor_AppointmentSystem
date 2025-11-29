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

        public DbSet<AdminProfile> AdminProfiles { get; set; } = null!;
        public DbSet<DoctorProfile> DoctorProfiles { get; set; } = null!;
        public DbSet<ReceptionistProfile> ReceptionistProfiles { get; set; } = null!;
        public DbSet<PatientProfile> PatientProfiles { get; set; } = null!;

        public DbSet<Specialty> Specialties { get; set; } = null!;
        public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; } = null!;
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; } = null!;
        public DbSet<DoctorUnavailability> DoctorUnavailabilities { get; set; } = null!;

        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<NotificationLog> NotificationLogs { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ******************************************
            //  FIX MULTIPLE CASCADE PATHS FOR APPOINTMENT
            // ******************************************

            // Appointment -> DoctorProfile  (NO CASCADE)
            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment -> PatientProfile (NO CASCADE)
            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany() // we don't have a collection on PatientProfile
                .HasForeignKey(a => a.PatientProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review -> DoctorProfile (NO CASCADE)
            builder.Entity<Review>()
                .HasOne(r => r.Doctor)
                .WithMany(d => d.Reviews)
                .HasForeignKey(r => r.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review -> PatientProfile (NO CASCADE)
            builder.Entity<Review>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review -> Appointment (if appointment deleted, just null the link)
            builder.Entity<Review>()
                .HasOne(r => r.Appointment)
                .WithMany()
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // PaymentTransaction -> Appointment (NO CASCADE)
            builder.Entity<PaymentTransaction>()
                .HasOne(p => p.Appointment)
                .WithMany()
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // NotificationLog -> Appointment (if appointment deleted, keep log)
            builder.Entity<NotificationLog>()
                .HasOne(n => n.Appointment)
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
