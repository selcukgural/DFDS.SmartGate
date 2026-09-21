using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFDS.SmartGate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "visits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    terminal_id = table.Column<string>(type: "character(5)", fixedLength: true, maxLength: 5, nullable: false),
                    current_status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    driver_license_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    driver_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    driver_phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    truck_carrier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    truck_license_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    truck_unit_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visits", x => x.id);
                },
                comment: "Truck visits. Rows are retained for 7 years (regulatory); archive/partition by created_at, never delete in-app.");

            migrationBuilder.CreateTable(
                name: "visit_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    unit_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    from_location = table.Column<string>(type: "character(5)", fixedLength: true, maxLength: 5, nullable: false),
                    to_location = table.Column<string>(type: "character(5)", fixedLength: true, maxLength: 5, nullable: false),
                    reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    from_country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false, computedColumnSql: "left(from_location, 2)", stored: true),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    to_country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false, computedColumnSql: "left(to_location, 2)", stored: true),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visit_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_visit_movements_visit",
                        column: x => x.visit_id,
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visit_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visit_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_visit_status_history_visit",
                        column: x => x.visit_id,
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Append-only audit trail of status changes; UPDATE/DELETE are rejected by trigger.");

            migrationBuilder.CreateIndex(
                name: "ix_visit_movements_from_country",
                table: "visit_movements",
                column: "from_country");

            migrationBuilder.CreateIndex(
                name: "ix_visit_movements_from_location",
                table: "visit_movements",
                column: "from_location");

            migrationBuilder.CreateIndex(
                name: "ix_visit_movements_to_country",
                table: "visit_movements",
                column: "to_country");

            migrationBuilder.CreateIndex(
                name: "ix_visit_movements_to_location",
                table: "visit_movements",
                column: "to_location");

            migrationBuilder.CreateIndex(
                name: "ix_visit_movements_visit_sequence",
                table: "visit_movements",
                columns: new[] { "visit_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_visit_status_history_visit_changed_at",
                table: "visit_status_history",
                columns: new[] { "visit_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_visits_terminal_created",
                table: "visits",
                columns: new[] { "terminal_id", "created_at", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_visits_terminal_created_by_created",
                table: "visits",
                columns: new[] { "terminal_id", "created_by", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_visits_terminal_status_created",
                table: "visits",
                columns: new[] { "terminal_id", "current_status", "created_at" },
                descending: new[] { false, false, true });

            // The audit trail is append-only by contract: reject any rewrite at the database, regardless of the client.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION visit_status_history_reject_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'visit_status_history is append-only: % is not allowed', TG_OP
                        USING ERRCODE = 'integrity_constraint_violation';
                END;
                $$;

                CREATE TRIGGER trg_visit_status_history_append_only
                    BEFORE UPDATE OR DELETE ON visit_status_history
                    FOR EACH ROW EXECUTE FUNCTION visit_status_history_reject_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_visit_status_history_append_only ON visit_status_history;
                DROP FUNCTION IF EXISTS visit_status_history_reject_change();
                """);

            migrationBuilder.DropTable(
                name: "visit_movements");

            migrationBuilder.DropTable(
                name: "visit_status_history");

            migrationBuilder.DropTable(
                name: "visits");
        }
    }
}
