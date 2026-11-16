using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BetLimitsAndCulSec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Modifiée à la main : les tables et manches déjà enregistrées reçoivent les valeurs
            // d'avant cette évolution (mises de 1 à 10 gorgées), et les transferts existants sont
            // recopiés dans la nouvelle table avant la suppression de l'ancienne (voir plus bas).
            migrationBuilder.AddColumn<bool>(
                name: "AllowCulSec",
                table: "Tables",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxBet",
                table: "Tables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "MinBet",
                table: "Tables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "RoundHands",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "Sip");

            migrationBuilder.AddColumn<int>(
                name: "TotalCulsSecsDrunk",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCulsSecsGiven",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoundDrinkTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromPlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToPlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundDrinkTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundDrinkTransfers_Players_FromPlayerId",
                        column: x => x.FromPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundDrinkTransfers_Players_ToPlayerId",
                        column: x => x.ToPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundDrinkTransfers_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoundDrinkTransfers_FromPlayerId",
                table: "RoundDrinkTransfers",
                column: "FromPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundDrinkTransfers_RoundId",
                table: "RoundDrinkTransfers",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundDrinkTransfers_ToPlayerId",
                table: "RoundDrinkTransfers",
                column: "ToPlayerId");

            migrationBuilder.Sql(
                """
                INSERT INTO "RoundDrinkTransfers" ("Id", "RoundId", "FromPlayerId", "ToPlayerId", "Amount", "Unit")
                SELECT "Id", "RoundId", "FromPlayerId", "ToPlayerId", "Sips", 'Sip' FROM "RoundSipTransfers";
                """);

            migrationBuilder.DropTable(
                name: "RoundSipTransfers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowCulSec",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "MaxBet",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "MinBet",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "RoundHands");

            migrationBuilder.DropColumn(
                name: "TotalCulsSecsDrunk",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TotalCulsSecsGiven",
                table: "Players");

            migrationBuilder.CreateTable(
                name: "RoundSipTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromPlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sips = table.Column<int>(type: "INTEGER", nullable: false),
                    ToPlayerId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundSipTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundSipTransfers_Players_FromPlayerId",
                        column: x => x.FromPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundSipTransfers_Players_ToPlayerId",
                        column: x => x.ToPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundSipTransfers_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoundSipTransfers_FromPlayerId",
                table: "RoundSipTransfers",
                column: "FromPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundSipTransfers_RoundId",
                table: "RoundSipTransfers",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundSipTransfers_ToPlayerId",
                table: "RoundSipTransfers",
                column: "ToPlayerId");

            // Les culs secs n'ont pas d'équivalent dans l'ancien schéma : seules les gorgées reviennent.
            migrationBuilder.Sql(
                """
                INSERT INTO "RoundSipTransfers" ("Id", "RoundId", "FromPlayerId", "ToPlayerId", "Sips")
                SELECT "Id", "RoundId", "FromPlayerId", "ToPlayerId", "Amount" FROM "RoundDrinkTransfers" WHERE "Unit" = 'Sip';
                """);

            migrationBuilder.DropTable(
                name: "RoundDrinkTransfers");
        }
    }
}
