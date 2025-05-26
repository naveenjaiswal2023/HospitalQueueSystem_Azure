using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalQueueSystem.Migrations
{
    /// <inheritdoc />
    public partial class updateDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QueueEntry",
                table: "QueueEntry");

            migrationBuilder.RenameTable(
                name: "QueueEntry",
                newName: "QueueEntries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QueueEntries",
                table: "QueueEntries",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QueueEntries",
                table: "QueueEntries");

            migrationBuilder.RenameTable(
                name: "QueueEntries",
                newName: "QueueEntry");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QueueEntry",
                table: "QueueEntry",
                column: "Id");
        }
    }
}
