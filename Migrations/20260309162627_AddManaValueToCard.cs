using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGDeckBuilder.Migrations
{
    /// <inheritdoc />
    public partial class AddManaValueToCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ColorIdentity",
                table: "Decks",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ManaValue",
                table: "Cards",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManaValue",
                table: "Cards");

            migrationBuilder.UpdateData(
                table: "Decks",
                keyColumn: "ColorIdentity",
                keyValue: null,
                column: "ColorIdentity",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ColorIdentity",
                table: "Decks",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
