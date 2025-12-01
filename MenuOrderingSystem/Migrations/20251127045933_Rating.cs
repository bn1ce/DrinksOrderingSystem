using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class Rating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_MemberEmail",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Drinks_DrinkID",
                table: "Ratings");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_MemberEmail",
                table: "Orders",
                column: "MemberEmail",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Drinks_DrinkID",
                table: "Ratings",
                column: "DrinkID",
                principalTable: "Drinks",
                principalColumn: "DrinkID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_MemberEmail",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Drinks_DrinkID",
                table: "Ratings");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_MemberEmail",
                table: "Orders",
                column: "MemberEmail",
                principalTable: "Users",
                principalColumn: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Drinks_DrinkID",
                table: "Ratings",
                column: "DrinkID",
                principalTable: "Drinks",
                principalColumn: "DrinkID");
        }
    }
}
