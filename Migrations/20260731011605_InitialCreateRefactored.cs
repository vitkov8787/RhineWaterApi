using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RhineWaterApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateRefactored : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RhineLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StationName = table.Column<string>(type: "text", nullable: false),
                    Kilometer = table.Column<double>(type: "double precision", nullable: false),
                    LevelCm = table.Column<int>(type: "integer", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RhineLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StationName = table.Column<string>(type: "text", nullable: false),
                    GlwCm = table.Column<int>(type: "integer", nullable: false),
                    GuaranteedDepthCm = table.Column<int>(type: "integer", nullable: false),
                    SafetyMarginCm = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RhineLevels");

            migrationBuilder.DropTable(
                name: "StationConfigs");
        }
    }
}
