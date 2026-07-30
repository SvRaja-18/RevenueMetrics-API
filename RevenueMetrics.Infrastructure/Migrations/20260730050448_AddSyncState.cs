using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RevenueMetrics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawPayload",
                table: "transactions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "sync_states",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastCursor = table.Column<string>(type: "text", nullable: true),
                    LastSyncTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sync_states_SourceName",
                table: "sync_states",
                column: "SourceName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_states");

            migrationBuilder.DropColumn(
                name: "RawPayload",
                table: "transactions");
        }
    }
}
