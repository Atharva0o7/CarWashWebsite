using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarWashWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "washes_included",
                table: "wash_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reference = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    car_brand = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    car_model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    body_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    wash_plan_id = table.Column<int>(type: "integer", nullable: false),
                    locality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    monthly_price = table.Column<int>(type: "integer", nullable: false),
                    washes_included = table.Column<int>(type: "integer", nullable: false),
                    washes_used_this_period = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    current_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    current_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_wash_on = table.Column<DateOnly>(type: "date", nullable: true),
                    whats_app_opt_in = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_wash_plans_wash_plan_id",
                        column: x => x.wash_plan_id,
                        principalTable: "wash_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reminder_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    dedupe_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    subscription_id = table.Column<int>(type: "integer", nullable: true),
                    booking_id = table.Column<int>(type: "integer", nullable: true),
                    template_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    template_params_json = table.Column<string>(type: "jsonb", nullable: false),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminder_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_reminder_logs_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reminder_logs_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reminder_logs_booking_id",
                table: "reminder_logs",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_reminder_logs_dedupe_key",
                table: "reminder_logs",
                column: "dedupe_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reminder_logs_status_created_at",
                table: "reminder_logs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reminder_logs_subscription_id",
                table: "reminder_logs",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_phone",
                table: "subscriptions",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_reference",
                table: "subscriptions",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_status_current_period_end",
                table: "subscriptions",
                columns: new[] { "status", "current_period_end" });

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_wash_plan_id",
                table: "subscriptions",
                column: "wash_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reminder_logs");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropColumn(
                name: "washes_included",
                table: "wash_plans");
        }
    }
}
