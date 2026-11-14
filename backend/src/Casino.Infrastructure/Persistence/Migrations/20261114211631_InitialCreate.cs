using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Game = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TableId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeftAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalDrunk = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalGiven = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TableId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DealerCards = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DealerTotal = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rounds_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundHands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cards = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Total = table.Column<int>(type: "INTEGER", nullable: false),
                    Bet = table.Column<int>(type: "INTEGER", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundHands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundHands_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundHands_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundSipTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromPlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToPlayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sips = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "IX_Players_TableId",
                table: "Players",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundHands_PlayerId",
                table: "RoundHands",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundHands_RoundId",
                table: "RoundHands",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_TableId_Number",
                table: "Rounds",
                columns: new[] { "TableId", "Number" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Tables_ClosedAtUtc",
                table: "Tables",
                column: "ClosedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_Code",
                table: "Tables",
                column: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoundHands");

            migrationBuilder.DropTable(
                name: "RoundSipTransfers");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "Tables");
        }
    }
}
