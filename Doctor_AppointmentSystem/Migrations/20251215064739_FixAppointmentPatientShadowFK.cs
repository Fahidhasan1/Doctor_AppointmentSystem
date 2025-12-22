using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doctor_AppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixAppointmentPatientShadowFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty:
            // The real DB does not have PatientId column.
            // This migration exists to update EF's model snapshot after fixing the relationship mapping.
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty
        }

    }
}
