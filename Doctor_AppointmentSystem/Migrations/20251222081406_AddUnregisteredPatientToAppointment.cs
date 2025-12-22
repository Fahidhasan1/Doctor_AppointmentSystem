using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doctor_AppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUnregisteredPatientToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PatientProfileId",
                table: "Appointments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "UnregisteredPatientName",
                table: "Appointments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnregisteredPatientName",
                table: "Appointments");

            migrationBuilder.AlterColumn<int>(
                name: "PatientProfileId",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
