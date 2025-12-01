using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doctor_AppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorRatingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "DoctorProfiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "DoctorProfiles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "DoctorProfiles");
        }
    }
}
