using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elementum.Infrastructure.Outbound.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConstrainMetalsSymbol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "metals",
                newName: "symbol");

            migrationBuilder.AlterColumn<string>(
                name: "symbol",
                table: "metals",
                type: "varchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_metals_symbol",
                table: "metals",
                column: "symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_metals_symbol",
                table: "metals");

            migrationBuilder.RenameColumn(
                name: "symbol",
                table: "metals",
                newName: "Symbol");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "metals",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldMaxLength: 8)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
