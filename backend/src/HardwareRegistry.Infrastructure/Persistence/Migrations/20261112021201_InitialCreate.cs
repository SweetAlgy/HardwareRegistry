using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareRegistry.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:natural", "und-u-kn-true,und-u-kn-true,icu,True");

            migrationBuilder.CreateTable(
                name: "device_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, collation: "natural")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, collation: "natural"),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, collation: "natural"),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.CheckConstraint("ck_devices_inventory_number_format", "inventory_number ~ '^[A-Za-z0-9]+$'");
                    table.CheckConstraint("ck_devices_room_range", "room BETWEEN 1 AND 1000");
                    table.CheckConstraint("ck_entity_created_at_not_later_than_updated_at", "created_at <= updated_at");
                    table.ForeignKey(
                        name: "fk_devices_device_types_type_id",
                        column: x => x.type_id,
                        principalTable: "device_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_device_types_name",
                table: "device_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_inventory_number",
                table: "devices",
                column: "inventory_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_type_id",
                table: "devices",
                column: "type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "device_types");
        }
    }
}
