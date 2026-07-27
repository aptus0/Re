using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Re.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesforceProvisioningPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesforceBlueprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ModulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeaturesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesforceBlueprints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesforceTenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SalesforceOrgId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    InstanceUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Edition = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ApiVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConnectedUserId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CredentialReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ConnectionStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EnvironmentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NamespaceStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesforceTenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesforceDeploymentJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlueprintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetEnvironment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesforceDeploymentJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesforceDeploymentJobs_SalesforceBlueprints_BlueprintId",
                        column: x => x.BlueprintId,
                        principalTable: "SalesforceBlueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesforceDeploymentJobs_SalesforceTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SalesforceTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesforceOrgDiscoveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HasApiAccess = table.Column<bool>(type: "bit", nullable: false),
                    HasModifyAllData = table.Column<bool>(type: "bit", nullable: false),
                    SupportsNamedCredentials = table.Column<bool>(type: "bit", nullable: false),
                    SupportsPlatformEvents = table.Column<bool>(type: "bit", nullable: false),
                    SupportsMcp = table.Column<bool>(type: "bit", nullable: false),
                    ConflictingFields = table.Column<int>(type: "int", nullable: false),
                    ConflictingFlows = table.Column<int>(type: "int", nullable: false),
                    MissingPermissions = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    FindingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesforceOrgDiscoveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesforceOrgDiscoveries_SalesforceTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SalesforceTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesforceDeploymentSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LogSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesforceDeploymentSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesforceDeploymentSteps_SalesforceDeploymentJobs_DeploymentJobId",
                        column: x => x.DeploymentJobId,
                        principalTable: "SalesforceDeploymentJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesforceBlueprints_CompanyId_Name_Version",
                table: "SalesforceBlueprints",
                columns: new[] { "CompanyId", "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesforceDeploymentJobs_BlueprintId",
                table: "SalesforceDeploymentJobs",
                column: "BlueprintId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesforceDeploymentJobs_CorrelationId",
                table: "SalesforceDeploymentJobs",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesforceDeploymentJobs_TenantId",
                table: "SalesforceDeploymentJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesforceDeploymentSteps_DeploymentJobId_Sequence",
                table: "SalesforceDeploymentSteps",
                columns: new[] { "DeploymentJobId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesforceOrgDiscoveries_TenantId",
                table: "SalesforceOrgDiscoveries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesforceTenants_CompanyId_SalesforceOrgId",
                table: "SalesforceTenants",
                columns: new[] { "CompanyId", "SalesforceOrgId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesforceDeploymentSteps");

            migrationBuilder.DropTable(
                name: "SalesforceOrgDiscoveries");

            migrationBuilder.DropTable(
                name: "SalesforceDeploymentJobs");

            migrationBuilder.DropTable(
                name: "SalesforceBlueprints");

            migrationBuilder.DropTable(
                name: "SalesforceTenants");
        }
    }
}
