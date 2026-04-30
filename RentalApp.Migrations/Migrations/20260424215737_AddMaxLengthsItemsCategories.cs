using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxLengthsItemsCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "items",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "items",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100
            );

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048
            );

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "items",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100
            );

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100
            );
        }
    }
}
