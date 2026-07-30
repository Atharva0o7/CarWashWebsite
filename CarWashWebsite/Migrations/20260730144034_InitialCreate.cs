using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarWashWebsite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "car_brands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_car_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "faqs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    question = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    answer = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faqs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "localities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_localities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    icon = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    starting_price = table.Column<int>(type: "integer", nullable: false),
                    list_price = table.Column<int>(type: "integer", nullable: false),
                    duration = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    warranty = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_new = table.Column<bool>(type: "boolean", nullable: false),
                    is_bestseller = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "testimonials",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quote = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    author = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    car = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    locality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_testimonials", x => x.id);
                    table.CheckConstraint("ck_testimonials_rating", "rating BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "wash_plans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    blurb = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    price_per_month = table.Column<int>(type: "integer", nullable: false),
                    list_price_per_month = table.Column<int>(type: "integer", nullable: false),
                    wash_count = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_popular = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "car_models",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    car_brand_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    body_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_car_models", x => x.id);
                    table.ForeignKey(
                        name: "fk_car_models_car_brands_car_brand_id",
                        column: x => x.car_brand_id,
                        principalTable: "car_brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
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
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    locality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    preferred_date = table.Column<DateOnly>(type: "date", nullable: false),
                    preferred_slot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    quoted_price = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_includes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_includes", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_includes_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_features",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wash_plan_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_features", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_features_wash_plans_wash_plan_id",
                        column: x => x.wash_plan_id,
                        principalTable: "wash_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_phone",
                table: "bookings",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_preferred_date_status",
                table: "bookings",
                columns: new[] { "preferred_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_reference",
                table: "bookings",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_service_id",
                table: "bookings",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_car_brands_name",
                table: "car_brands",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_car_models_car_brand_id_name",
                table: "car_models",
                columns: new[] { "car_brand_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_localities_city_name",
                table: "localities",
                columns: new[] { "city", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plan_features_wash_plan_id",
                table: "plan_features",
                column: "wash_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_includes_service_id",
                table: "service_includes",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_slug",
                table: "services",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wash_plans_name",
                table: "wash_plans",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "car_models");

            migrationBuilder.DropTable(
                name: "faqs");

            migrationBuilder.DropTable(
                name: "localities");

            migrationBuilder.DropTable(
                name: "plan_features");

            migrationBuilder.DropTable(
                name: "service_includes");

            migrationBuilder.DropTable(
                name: "testimonials");

            migrationBuilder.DropTable(
                name: "car_brands");

            migrationBuilder.DropTable(
                name: "wash_plans");

            migrationBuilder.DropTable(
                name: "services");
        }
    }
}
