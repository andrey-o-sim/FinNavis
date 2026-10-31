using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNavis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCsvImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_row_count = table.Column<int>(type: "integer", nullable: false),
                    already_present_row_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_runs_account",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "mapping_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shape_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mapping_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_created_account_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_created_account_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_created_account_links_account",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_import_created_account_links_import_run",
                        column: x => x.import_run_id,
                        principalTable: "import_runs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "import_created_category_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_created_category_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_created_category_links_category",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_import_created_category_links_import_run",
                        column: x => x.import_run_id,
                        principalTable: "import_runs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "import_set_aside_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    raw_line = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_set_aside_rows", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_set_aside_rows_import_run",
                        column: x => x.import_run_id,
                        principalTable: "import_runs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "import_transaction_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_transaction_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_transaction_links_import_run",
                        column: x => x.import_run_id,
                        principalTable: "import_runs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_import_transaction_links_transaction",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "import_transfer_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_transfer_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_transfer_links_import_run",
                        column: x => x.import_run_id,
                        principalTable: "import_runs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_import_transfer_links_transfer",
                        column: x => x.transfer_id,
                        principalTable: "transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mapping_profile_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapping_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mapping_profile_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_mapping_profile_rules_category",
                        column: x => x.target_category_id,
                        principalTable: "categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_mapping_profile_rules_mapping_profile",
                        column: x => x.mapping_profile_id,
                        principalTable: "mapping_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_import_created_account_links_import_run_id",
                table: "import_created_account_links",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ux_import_created_account_links_account_id",
                table: "import_created_account_links",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_import_created_category_links_import_run_id",
                table: "import_created_category_links",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ux_import_created_category_links_category_id",
                table: "import_created_category_links",
                column: "category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_import_runs_account_id",
                table: "import_runs",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "idx_import_set_aside_rows_import_run_id",
                table: "import_set_aside_rows",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "idx_import_transaction_links_import_run_id",
                table: "import_transaction_links",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ux_import_transaction_links_transaction_id",
                table: "import_transaction_links",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_import_transfer_links_import_run_id",
                table: "import_transfer_links",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ux_import_transfer_links_transfer_id",
                table: "import_transfer_links",
                column: "transfer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_mapping_profile_rules_target_category_id",
                table: "mapping_profile_rules",
                column: "target_category_id");

            migrationBuilder.CreateIndex(
                name: "ux_mapping_profile_rules_profile_source_category",
                table: "mapping_profile_rules",
                columns: new[] { "mapping_profile_id", "source_category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mapping_profiles_shape_key",
                table: "mapping_profiles",
                column: "shape_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_created_account_links");

            migrationBuilder.DropTable(
                name: "import_created_category_links");

            migrationBuilder.DropTable(
                name: "import_set_aside_rows");

            migrationBuilder.DropTable(
                name: "import_transaction_links");

            migrationBuilder.DropTable(
                name: "import_transfer_links");

            migrationBuilder.DropTable(
                name: "mapping_profile_rules");

            migrationBuilder.DropTable(
                name: "import_runs");

            migrationBuilder.DropTable(
                name: "mapping_profiles");
        }
    }
}
