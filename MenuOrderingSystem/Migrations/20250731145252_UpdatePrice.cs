using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceMedium",
                table: "Drinks");

            migrationBuilder.RenameColumn(
                name: "PriceSmall",
                table: "Drinks",
                newName: "PriceRegular");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceRegular",
                table: "Drinks",
                newName: "PriceSmall");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceMedium",
                table: "Drinks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
