using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewRentalIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_reviews_RentalId", table: "reviews");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_RentalId",
                table: "reviews",
                column: "RentalId",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_reviews_RentalId", table: "reviews");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_RentalId",
                table: "reviews",
                column: "RentalId"
            );
        }
    }
}
