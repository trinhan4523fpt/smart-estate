using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEstate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listings_users_AssignedBrokerUserId",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users");

            migrationBuilder.DropTable(
                name: "broker_profiles");

            migrationBuilder.DropTable(
                name: "point_purchases");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "takeover_requests");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropIndex(
                name: "IX_users_RoleId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_payments_PayerUserId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_PointPurchaseId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_Provider_ProviderRef",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_TakeoverRequestId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_Type_Status",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_listings_addr_city",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_addr_district",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_IsDeleted",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_lat",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_lng",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_ModerationStatus_LifecycleStatus",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_price_amount",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ListingId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "PointPurchaseId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "addr_full",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "addr_street",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "addr_ward",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "price_amount",
                table: "listings");

            migrationBuilder.RenameColumn(
                name: "currency",
                table: "payments",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "payments",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "TakeoverRequestId",
                table: "payments",
                newName: "RefId");

            migrationBuilder.RenameColumn(
                name: "lng",
                table: "listings",
                newName: "Lng");

            migrationBuilder.RenameColumn(
                name: "lat",
                table: "listings",
                newName: "Lat");

            migrationBuilder.RenameColumn(
                name: "addr_district",
                table: "listings",
                newName: "District");

            migrationBuilder.RenameColumn(
                name: "addr_city",
                table: "listings",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "price_currency",
                table: "listings",
                newName: "TransactionType");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "payments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "payments",
                type: "numeric(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderRef",
                table: "payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PayUrl",
                table: "payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeeType",
                table: "payments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RefType",
                table: "payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Lng",
                table: "listings",
                type: "decimal(11,8)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Lat",
                table: "listings",
                type: "decimal(10,8)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VirtualTourUrl",
                table: "listings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "listings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "QualityScore",
                table: "listings",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PropertyType",
                table: "listings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ModerationStatus",
                table: "listings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ModerationReason",
                table: "listings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LifecycleStatus",
                table: "listings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "listings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "listings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "listings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerManaged",
                table: "listings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "listings",
                type: "numeric(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "listings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerPhone",
                table: "listings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "broker_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TakeoverFeeAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    FeePaidByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FeeStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broker_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_broker_requests_listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_broker_requests_users_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_broker_requests_users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "point_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PointPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_point_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_point_transactions_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_point_transactions_point_packages_PointPackageId",
                        column: x => x.PointPackageId,
                        principalTable: "point_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_point_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_broker_requests_BrokerId",
                table: "broker_requests",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_broker_requests_ListingId",
                table: "broker_requests",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_broker_requests_SellerId",
                table: "broker_requests",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_PaymentId",
                table: "point_transactions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_PointPackageId",
                table: "point_transactions",
                column: "PointPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_UserId",
                table: "point_transactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_listings_users_AssignedBrokerUserId",
                table: "listings",
                column: "AssignedBrokerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listings_users_AssignedBrokerUserId",
                table: "listings");

            migrationBuilder.DropTable(
                name: "broker_requests");

            migrationBuilder.DropTable(
                name: "point_transactions");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "FeeType",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "RefType",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "IsBrokerManaged",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "SellerPhone",
                table: "listings");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "payments",
                newName: "currency");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "payments",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "RefId",
                table: "payments",
                newName: "TakeoverRequestId");

            migrationBuilder.RenameColumn(
                name: "Lng",
                table: "listings",
                newName: "lng");

            migrationBuilder.RenameColumn(
                name: "Lat",
                table: "listings",
                newName: "lat");

            migrationBuilder.RenameColumn(
                name: "District",
                table: "listings",
                newName: "addr_district");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "listings",
                newName: "addr_city");

            migrationBuilder.RenameColumn(
                name: "TransactionType",
                table: "listings",
                newName: "price_currency");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<short>(
                name: "RoleId",
                table: "users",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderRef",
                table: "payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PayUrl",
                table: "payments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "currency",
                table: "payments",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,0)");

            migrationBuilder.AddColumn<Guid>(
                name: "ListingId",
                table: "payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PointPurchaseId",
                table: "payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "VirtualTourUrl",
                table: "listings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "listings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<decimal>(
                name: "QualityScore",
                table: "listings",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PropertyType",
                table: "listings",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "ModerationStatus",
                table: "listings",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ModerationReason",
                table: "listings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "lng",
                table: "listings",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LifecycleStatus",
                table: "listings",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<double>(
                name: "lat",
                table: "listings",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "addr_full",
                table: "listings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "addr_street",
                table: "listings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "addr_ward",
                table: "listings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_amount",
                table: "listings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "broker_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LicenseNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RatingAvg = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broker_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_broker_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "point_purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PointPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceCurrency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_point_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_point_purchases_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_point_purchases_point_packages_PointPackageId",
                        column: x => x.PointPackageId,
                        principalTable: "point_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_point_purchases_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "takeover_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrokerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsFeePaid = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Payer = table.Column<int>(type: "int", nullable: false),
                    RejectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_takeover_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_takeover_requests_listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_takeover_requests_users_BrokerUserId",
                        column: x => x.BrokerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_takeover_requests_users_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<short>(type: "smallint", nullable: false),
                    RoleId = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_RoleId",
                table: "users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PayerUserId",
                table: "payments",
                column: "PayerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PointPurchaseId",
                table: "payments",
                column: "PointPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Provider_ProviderRef",
                table: "payments",
                columns: new[] { "Provider", "ProviderRef" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_TakeoverRequestId",
                table: "payments",
                column: "TakeoverRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Type_Status",
                table: "payments",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_listings_addr_city",
                table: "listings",
                column: "addr_city");

            migrationBuilder.CreateIndex(
                name: "IX_listings_addr_district",
                table: "listings",
                column: "addr_district");

            migrationBuilder.CreateIndex(
                name: "IX_listings_IsDeleted",
                table: "listings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_listings_lat",
                table: "listings",
                column: "lat");

            migrationBuilder.CreateIndex(
                name: "IX_listings_lng",
                table: "listings",
                column: "lng");

            migrationBuilder.CreateIndex(
                name: "IX_listings_ModerationStatus_LifecycleStatus",
                table: "listings",
                columns: new[] { "ModerationStatus", "LifecycleStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_listings_price_amount",
                table: "listings",
                column: "price_amount");

            migrationBuilder.CreateIndex(
                name: "IX_broker_profiles_UserId",
                table: "broker_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Code",
                table: "permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_IsDeleted",
                table: "permissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_point_purchases_IsDeleted",
                table: "point_purchases",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_point_purchases_PaymentId",
                table: "point_purchases",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_point_purchases_PointPackageId",
                table: "point_purchases",
                column: "PointPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_point_purchases_Status",
                table: "point_purchases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_point_purchases_UserId",
                table: "point_purchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_IsDeleted",
                table: "role_permissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                table: "role_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_RoleId",
                table: "role_permissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_RoleId_PermissionId",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_IsDeleted",
                table: "roles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_takeover_requests_BrokerUserId_Status",
                table: "takeover_requests",
                columns: new[] { "BrokerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_takeover_requests_IsFeePaid",
                table: "takeover_requests",
                column: "IsFeePaid");

            migrationBuilder.CreateIndex(
                name: "IX_takeover_requests_ListingId_Status",
                table: "takeover_requests",
                columns: new[] { "ListingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_takeover_requests_SellerUserId",
                table: "takeover_requests",
                column: "SellerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_listings_users_AssignedBrokerUserId",
                table: "listings",
                column: "AssignedBrokerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
