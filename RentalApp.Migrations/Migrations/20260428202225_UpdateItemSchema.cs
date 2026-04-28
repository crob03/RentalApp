using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RentalApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateItemSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ImageUrl", table: "items");

            migrationBuilder.DropColumn(name: "Latitude", table: "items");

            migrationBuilder.DropColumn(name: "Longitude", table: "items");

            migrationBuilder.AlterDatabase().Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "items",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "items",
                type: "geography(Point, 4326)",
                nullable: false
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "items",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedAt", table: "items");

            migrationBuilder.DropColumn(name: "IsAvailable", table: "items");

            migrationBuilder.DropColumn(name: "Location", table: "items");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "items");

            migrationBuilder.DropColumn(name: "CreatedAt", table: "categories");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "categories");

            migrationBuilder
                .AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "items",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "items",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0
            );

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "items",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0
            );
        }
    }
}
