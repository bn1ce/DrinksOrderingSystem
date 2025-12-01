using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class ordertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserEmail",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserEmail",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "MemberEmail",
                table: "Orders",
                type: "nvarchar(100)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MemberEmail",
                table: "Orders",
                column: "MemberEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_MemberEmail",
                table: "Orders",
                column: "MemberEmail",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_MemberEmail",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_MemberEmail",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "MemberEmail",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "Orders",
                type: "nvarchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserEmail",
                table: "Orders",
                column: "UserEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserEmail",
                table: "Orders",
                column: "UserEmail",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
