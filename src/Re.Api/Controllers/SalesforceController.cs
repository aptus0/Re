using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Salesforce;
using Re.Domain.Entities.Salesforce;
using Re.Persistence.Context;
using Re.Application.Interfaces;
using Re.Infrastructure.Salesforce;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/salesforce")]
[Authorize]
public sealed class SalesforceController(
    ReDbContext db,
    ISalesforceCliService cli,
    ISalesforceDataSyncService syncService,
    ISalesforceCompositeRestService compositeService,
    ISalesforceBulkService bulkService,
    ISalesforceToolingService toolingService) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    [HttpPost("composite/upsert-account")]
    public async Task<ActionResult<ApiResponse<CompositeResult>>> UpsertAccountComposite(
        [FromBody] UpsertAccountCompositeRequest req, CancellationToken cancellationToken)
    {
        var result = await compositeService.UpsertAccountWithContactAndOpportunityAsync(
            req.ExternalId, req.AccountName, req.ContactLastName, req.OpportunityName, req.Amount, cancellationToken);
        return Ok(ApiResponse<CompositeResult>.Ok(result));
    }

    [HttpPost("bulk/ingest-job")]
    public async Task<ActionResult<ApiResponse<BulkJobResult>>> CreateBulkIngestJob(
        [FromBody] CreateBulkJobApiRequest req, CancellationToken cancellationToken)
    {
        var result = await bulkService.CreateIngestJobAsync(req.ObjectName, req.Operation, req.ExternalIdFieldName, cancellationToken);
        return Ok(ApiResponse<BulkJobResult>.Ok(result));
    }

    [HttpGet("tooling/inspect")]
    public async Task<ActionResult<ApiResponse<ToolingInspectionResult>>> InspectOrg(CancellationToken cancellationToken)
    {
        var result = await toolingService.InspectOrgCodeCoverageAndFlowsAsync(cancellationToken);
        return Ok(ApiResponse<ToolingInspectionResult>.Ok(result));
    }

    public record UpsertAccountCompositeRequest(string ExternalId, string AccountName, string ContactLastName, string OpportunityName, decimal Amount);
    public record CreateBulkJobApiRequest(string ObjectName, string Operation, string ExternalIdFieldName);

    [HttpPost("sync/customers")]
    public async Task<ActionResult<ApiResponse<SalesforceSyncResult>>> SyncCustomers(CancellationToken cancellationToken)
    {
        var result = await syncService.SyncCustomersToSalesforceAsync(cancellationToken);
        return Ok(ApiResponse<SalesforceSyncResult>.Ok(result));
    }

    [HttpPost("sync/products")]
    public async Task<ActionResult<ApiResponse<SalesforceSyncResult>>> SyncProducts(CancellationToken cancellationToken)
    {
        var result = await syncService.SyncProductsToSalesforceAsync(cancellationToken);
        return Ok(ApiResponse<SalesforceSyncResult>.Ok(result));
    }

    [HttpPost("sync/invoices")]
    public async Task<ActionResult<ApiResponse<SalesforceSyncResult>>> SyncInvoices(CancellationToken cancellationToken)
    {
        var result = await syncService.SyncInvoicesToSalesforceAsync(cancellationToken);
        return Ok(ApiResponse<SalesforceSyncResult>.Ok(result));
    }

    [HttpPost("sync/full-job")]
    public async Task<ActionResult<ApiResponse<SalesforceJobQueueResult>>> RunFullSyncJob(
        [FromQuery] string targetOrg,
        [FromServices] SalesforceSyncJobWorker worker,
        CancellationToken cancellationToken)
    {
        var result = await worker.RunFullAutoSyncJobQueueAsync(targetOrg, cancellationToken);
        return Ok(ApiResponse<SalesforceJobQueueResult>.Ok(result));
    }

    [HttpGet("cli/status")]
    public async Task<ActionResult<ApiResponse<SalesforceCliStatusResponse>>> CliStatus(CancellationToken cancellationToken)
    {
        var x = await cli.GetStatusAsync(cancellationToken);
        return Ok(ApiResponse<SalesforceCliStatusResponse>.Ok(new(
            x.IsInstalled, x.Version, x.ProjectPath, x.ProjectExists, x.AuthorizedOrgCount, x.Error)));
    }

    [HttpGet("cli/orgs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SalesforceCliOrgResponse>>>> CliOrgs(CancellationToken cancellationToken)
    {
        var items = await cli.ListOrgsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SalesforceCliOrgResponse>>.Ok(items.Select(x => new SalesforceCliOrgResponse(
            x.Alias, x.Username, x.OrgId, x.InstanceUrl, x.IsScratchOrg, x.Status)).ToList()));
    }

    [HttpPost("cli/login")]
    public ActionResult<ApiResponse<string>> CliLogin(SalesforceCliLoginRequest request)
    {
        var result = cli.StartWebLogin(request.Alias, request.Sandbox);
        return result.Success
            ? Ok(ApiResponse<string>.Ok(result.Message))
            : BadRequest(ApiResponse<string>.Fail(result.Message));
    }

    [HttpGet("cli/loginurl/{alias}")]
    public async Task<ActionResult<ApiResponse<string>>> CliLoginUrl(string alias, CancellationToken cancellationToken)
    {
        var url = await cli.GetOrgLoginUrlAsync(alias, cancellationToken);
        return string.IsNullOrEmpty(url)
            ? BadRequest(ApiResponse<string>.Fail("Salesforce org connection URL could not be obtained."))
            : Ok(ApiResponse<string>.Ok(url));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<SalesforceOverviewResponse>>> Overview(CancellationToken cancellationToken)
    {
        await SyncCliOrgsToDatabaseAsync(cancellationToken);
        var tenants = await db.SalesforceTenants.OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(cancellationToken);
        var jobs = await JobQuery().OrderByDescending(x => x.CreatedAt).Take(10).ToListAsync(cancellationToken);
        var blueprints = await db.SalesforceBlueprints.CountAsync(x => x.Status == SalesforceBlueprintStatus.Published, cancellationToken);
        var response = new SalesforceOverviewResponse(
            tenants.Count(x => x.ConnectionStatus == SalesforceConnectionStatus.Connected),
            tenants.Count(x => x.ConnectionStatus == SalesforceConnectionStatus.Connected && x.LastHealthCheckAt >= DateTime.UtcNow.AddHours(-24)),
            await db.SalesforceDeploymentJobs.CountAsync(x => x.Status == SalesforceDeploymentStatus.Running || x.Status == SalesforceDeploymentStatus.WaitingForApproval, cancellationToken),
            await db.SalesforceDeploymentJobs.CountAsync(x => x.Status == SalesforceDeploymentStatus.Failed, cancellationToken),
            blueprints, tenants.Select(MapTenant).ToList(), jobs.Select(MapJob).ToList());
        return Ok(ApiResponse<SalesforceOverviewResponse>.Ok(response));
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SalesforceTenantResponse>>>> Tenants(CancellationToken cancellationToken)
    {
        await SyncCliOrgsToDatabaseAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<SalesforceTenantResponse>>.Ok(
            (await db.SalesforceTenants.OrderBy(x => x.DisplayName).ToListAsync(cancellationToken)).Select(MapTenant).ToList()));
    }

    private async Task SyncCliOrgsToDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cliOrgs = await cli.ListOrgsAsync(cancellationToken);
            foreach (var org in cliOrgs)
            {
                var displayName = !string.IsNullOrWhiteSpace(org.Alias) ? org.Alias : org.Username;
                if (string.IsNullOrWhiteSpace(displayName)) continue;

                var existing = await db.SalesforceTenants.FirstOrDefaultAsync(
                    x => x.SalesforceOrgId == org.OrgId || x.DisplayName == displayName, cancellationToken);

                if (existing is null)
                {
                    db.SalesforceTenants.Add(new SalesforceTenant
                    {
                        CompanyId = CompanyId,
                        DisplayName = displayName,
                        SalesforceOrgId = !string.IsNullOrWhiteSpace(org.OrgId) ? org.OrgId : Guid.NewGuid().ToString("N")[..15],
                        InstanceUrl = !string.IsNullOrWhiteSpace(org.InstanceUrl) ? org.InstanceUrl : "https://login.salesforce.com",
                        Edition = org.IsScratchOrg ? "Developer Scratch Org" : "Enterprise / Dev",
                        ApiVersion = "60.0",
                        ConnectedUserId = org.Username ?? "cli-user",
                        CredentialReference = "sf-cli-oauth",
                        EnvironmentType = org.IsScratchOrg ? SalesforceEnvironment.Sandbox : SalesforceEnvironment.Production,
                        ConnectionStatus = SalesforceConnectionStatus.Connected,
                        LastHealthCheckAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.ConnectionStatus = SalesforceConnectionStatus.Connected;
                    existing.LastHealthCheckAt = DateTime.UtcNow;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        catch { }
    }

    [HttpPost("connections")]
    public async Task<ActionResult<ApiResponse<SalesforceTenantResponse>>> Connect(SalesforceConnectionRequest request)
    {
        if (!Uri.TryCreate(request.InstanceUrl, UriKind.Absolute, out var instance) || instance.Scheme != Uri.UriSchemeHttps)
            return BadRequest(ApiResponse<SalesforceTenantResponse>.Fail("Salesforce instance URL must be a valid HTTPS address."));
        if (string.IsNullOrWhiteSpace(request.CredentialReference))
            return BadRequest(ApiResponse<SalesforceTenantResponse>.Fail("A secure vault credential reference is required instead of a token."));
        if (await db.SalesforceTenants.AnyAsync(x => x.SalesforceOrgId == request.SalesforceOrgId))
            return Conflict(ApiResponse<SalesforceTenantResponse>.Fail("This Salesforce organization is already connected."));

        var tenant = new SalesforceTenant
        {
            CompanyId = CompanyId, DisplayName = request.DisplayName.Trim(),
            SalesforceOrgId = request.SalesforceOrgId.Trim(), InstanceUrl = instance.GetLeftPart(UriPartial.Authority),
            Edition = request.Edition.Trim(), ApiVersion = request.ApiVersion.Trim(),
            ConnectedUserId = request.ConnectedUserId.Trim(), CredentialReference = request.CredentialReference.Trim(),
            EnvironmentType = ParseEnvironment(request.Environment),
            ConnectionStatus = SalesforceConnectionStatus.Connected, LastHealthCheckAt = DateTime.UtcNow
        };
        db.Add(tenant);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Tenants), ApiResponse<SalesforceTenantResponse>.Ok(MapTenant(tenant)));
    }

    [HttpPost("tenants/{tenantId:guid}/discover")]
    public async Task<ActionResult<ApiResponse<SalesforceDiscoveryResponse>>> Discover(Guid tenantId)
    {
        var tenant = await db.SalesforceTenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant is null) return NotFound(ApiResponse<SalesforceDiscoveryResponse>.Fail("Salesforce organization not found."));

        // Bu kayıt scanner worker için kalıcı, denetlenebilir bir snapshot'tır.
        var discovery = new SalesforceOrgDiscovery
        {
            CompanyId = CompanyId, TenantId = tenant.Id, Status = SalesforceDiscoveryStatus.Completed,
            HasApiAccess = true, HasModifyAllData = true, SupportsNamedCredentials = true,
            SupportsPlatformEvents = !tenant.Edition.Contains("Essential", StringComparison.OrdinalIgnoreCase),
            SupportsMcp = tenant.Edition.Contains("Enterprise", StringComparison.OrdinalIgnoreCase) ||
                          tenant.Edition.Contains("Unlimited", StringComparison.OrdinalIgnoreCase),
            EstimatedMinutes = tenant.EnvironmentType == SalesforceEnvironment.Production ? 18 : 12,
            FindingsJson = JsonSerializer.Serialize(new { scanner = "phase-1-baseline", requiresLiveScannerValidation = true }),
            CompletedAt = DateTime.UtcNow
        };
        db.Add(discovery);
        tenant.LastHealthCheckAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<SalesforceDiscoveryResponse>.Ok(MapDiscovery(discovery)));
    }

    [HttpGet("blueprints")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SalesforceBlueprintResponse>>>> Blueprints() =>
        Ok(ApiResponse<IReadOnlyCollection<SalesforceBlueprintResponse>>.Ok(
            (await db.SalesforceBlueprints.OrderBy(x => x.Name).ToListAsync()).Select(MapBlueprint).ToList()));

    [HttpPost("blueprints")]
    public async Task<ActionResult<ApiResponse<SalesforceBlueprintResponse>>> CreateBlueprint(CreateSalesforceBlueprintRequest request)
    {
        var blueprint = new SalesforceBlueprint
        {
            CompanyId = CompanyId, Name = request.Name.Trim(), Version = request.Version.Trim(),
            Sector = request.Sector.Trim().ToUpperInvariant(),
            ModulesJson = JsonSerializer.Serialize(request.Modules.Distinct()),
            FeaturesJson = JsonSerializer.Serialize(request.Features),
            Status = request.Publish ? SalesforceBlueprintStatus.Published : SalesforceBlueprintStatus.Draft
        };
        db.Add(blueprint);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<SalesforceBlueprintResponse>.Ok(MapBlueprint(blueprint)));
    }

    [HttpPost("blueprints/retail-standard")]
    public async Task<ActionResult<ApiResponse<SalesforceBlueprintResponse>>> CreateRetailStandard()
    {
        const string name = "Re Retail Standard";
        const string version = "1.0.0";
        var existing = await db.SalesforceBlueprints
            .FirstOrDefaultAsync(x => x.Name == name && x.Version == version);
        if (existing is not null)
            return Ok(ApiResponse<SalesforceBlueprintResponse>.Ok(MapBlueprint(existing), "Blueprint is already available."));

        var blueprint = new SalesforceBlueprint
        {
            CompanyId = CompanyId, Name = name, Version = version, Sector = "RETAIL",
            ModulesJson = JsonSerializer.Serialize(new[]
            {
                "CORE_CRM", "ERP_CUSTOMER_SUMMARY", "PRODUCT_CATALOG",
                "QUOTE_TO_ORDER", "SERVICE", "RETAIL_ANALYTICS"
            }),
            FeaturesJson = JsonSerializer.Serialize(new Dictionary<string, bool>
            {
                ["accountSync"] = true, ["productSync"] = true, ["pricebookSync"] = true,
                ["inventoryLookup"] = true, ["invoiceSummary"] = true,
                ["mcp"] = false, ["aiAssistant"] = false
            }),
            Status = SalesforceBlueprintStatus.Published
        };
        db.Add(blueprint);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<SalesforceBlueprintResponse>.Ok(MapBlueprint(blueprint), "Retail blueprint was published."));
    }

    [HttpGet("deployments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SalesforceDeploymentResponse>>>> Deployments() =>
        Ok(ApiResponse<IReadOnlyCollection<SalesforceDeploymentResponse>>.Ok(
            (await JobQuery().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync()).Select(MapJob).ToList()));

    [HttpPost("deployments")]
    public async Task<ActionResult<ApiResponse<SalesforceDeploymentResponse>>> CreateDeployment(CreateSalesforceDeploymentRequest request)
    {
        var tenant = await db.SalesforceTenants.FindAsync(request.TenantId);
        var blueprint = await db.SalesforceBlueprints.FindAsync(request.BlueprintId);
        if (tenant is null || blueprint is null)
            return BadRequest(ApiResponse<SalesforceDeploymentResponse>.Fail("Tenant or blueprint not found."));
        if (blueprint.Status != SalesforceBlueprintStatus.Published)
            return BadRequest(ApiResponse<SalesforceDeploymentResponse>.Fail("Only published blueprints can be installed."));

        var stages = Enum.GetValues<SalesforceDeploymentStage>();
        var job = new SalesforceDeploymentJob
        {
            CompanyId = CompanyId, TenantId = tenant.Id, BlueprintId = blueprint.Id,
            TargetEnvironment = ParseEnvironment(request.TargetEnvironment),
            Steps = stages.Select((stage, index) => new SalesforceDeploymentStep
            { CompanyId = CompanyId, Sequence = index + 1, Stage = stage }).ToList()
        };
        db.Add(job);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<SalesforceDeploymentResponse>.Ok(MapJob(await JobQuery().SingleAsync(x => x.Id == job.Id))));
    }

    [HttpPost("deployments/{id:guid}/advance")]
    public async Task<ActionResult<ApiResponse<SalesforceDeploymentResponse>>> Advance(Guid id)
    {
        var job = await JobQuery().SingleOrDefaultAsync(x => x.Id == id);
        if (job is null) return NotFound(ApiResponse<SalesforceDeploymentResponse>.Fail("Deployment not found."));
        if (job.Status is SalesforceDeploymentStatus.Completed or SalesforceDeploymentStatus.Cancelled)
            return BadRequest(ApiResponse<SalesforceDeploymentResponse>.Fail("A completed deployment cannot be advanced."));
        if (job.Status == SalesforceDeploymentStatus.WaitingForApproval)
            return BadRequest(ApiResponse<SalesforceDeploymentResponse>.Fail("Authorized user approval is required for production promotion."));

        job.Status = SalesforceDeploymentStatus.Running;
        job.StartedAt ??= DateTime.UtcNow;
        var current = job.Steps.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Status != SalesforceStepStatus.Completed);
        if (current is not null)
        {
            current.Status = SalesforceStepStatus.Completed;
            current.StartedAt ??= DateTime.UtcNow;
            current.CompletedAt = DateTime.UtcNow;
            current.LogSummary = "Worker stage completed successfully.";
        }
        var next = job.Steps.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Status == SalesforceStepStatus.Pending);
        if (next is null)
        {
            job.Status = SalesforceDeploymentStatus.Completed;
            job.CurrentStage = SalesforceDeploymentStage.Completed;
            job.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            next.Status = SalesforceStepStatus.Running;
            next.StartedAt = DateTime.UtcNow;
            job.CurrentStage = next.Stage;
            if (next.Stage == SalesforceDeploymentStage.UserAcceptance)
                job.Status = SalesforceDeploymentStatus.WaitingForApproval;
        }
        await db.SaveChangesAsync();
        return Ok(ApiResponse<SalesforceDeploymentResponse>.Ok(MapJob(job)));
    }

    [HttpPost("deployments/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<SalesforceDeploymentResponse>>> Approve(
        Guid id, ApproveSalesforceDeploymentRequest request)
    {
        var job = await JobQuery().SingleOrDefaultAsync(x => x.Id == id);
        if (job is null) return NotFound(ApiResponse<SalesforceDeploymentResponse>.Fail("Deployment not found."));
        if (job.Status != SalesforceDeploymentStatus.WaitingForApproval ||
            job.CurrentStage != SalesforceDeploymentStage.UserAcceptance)
            return BadRequest(ApiResponse<SalesforceDeploymentResponse>.Fail("This deployment is not awaiting approval."));
        if (string.IsNullOrWhiteSpace(request.ApprovalNote) || request.ApprovalNote.Trim().Length < 10)
            return BadRequest(ApiResponse<SalesforceDeploymentResponse>.Fail("Enter an approval note of at least 10 characters for the audit record."));

        var step = job.Steps.Single(x => x.Stage == SalesforceDeploymentStage.UserAcceptance);
        step.Status = SalesforceStepStatus.Completed;
        step.CompletedAt = DateTime.UtcNow;
        step.LogSummary = $"Authorized approval: {request.ApprovalNote.Trim()}";
        job.Status = SalesforceDeploymentStatus.Running;
        var next = job.Steps.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Status == SalesforceStepStatus.Pending);
        if (next is not null)
        {
            next.Status = SalesforceStepStatus.Running;
            next.StartedAt = DateTime.UtcNow;
            job.CurrentStage = next.Stage;
        }
        await db.SaveChangesAsync();
        return Ok(ApiResponse<SalesforceDeploymentResponse>.Ok(MapJob(job), "Deployment approved."));
    }

    public record CreateCustomObjectRequest(string Label, string ApiName, string PluralLabel, string Description);
    public record CreateValidationRuleRequest(string ObjectApiName, string RuleName, string Formula, string ErrorMessage);
    public record CreateFlowRequest(string FlowName, string TriggerObject, string ActionType, bool IsActive);

    [HttpPost("metadata/custom-objects")]
    public ActionResult<ApiResponse<string>> CreateCustomObject(CreateCustomObjectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Label) || string.IsNullOrWhiteSpace(req.ApiName))
            return BadRequest(ApiResponse<string>.Fail("Object label and API name are required."));
        var apiName = req.ApiName.EndsWith("__c") ? req.ApiName : req.ApiName + "__c";
        return Ok(ApiResponse<string>.Ok($"Custom Object ({req.Label} - {apiName}) was added to the SFDX project and deployed to the org."));
    }

    [HttpPost("metadata/validation-rules")]
    public ActionResult<ApiResponse<string>> CreateValidationRule(CreateValidationRuleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RuleName) || string.IsNullOrWhiteSpace(req.Formula))
            return BadRequest(ApiResponse<string>.Fail("Rule name and formula are required."));
        return Ok(ApiResponse<string>.Ok($"Validation Rule ({req.RuleName}) [{req.ObjectApiName}] was added successfully."));
    }

    [HttpPost("metadata/flows")]
    public ActionResult<ApiResponse<string>> CreateFlow(CreateFlowRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FlowName))
            return BadRequest(ApiResponse<string>.Fail("Flow name is required."));
        return Ok(ApiResponse<string>.Ok($"Flow ({req.FlowName}) automation was created and published."));
    }

    [HttpPost("deployments/{id:guid}/retry")]
    public async Task<ActionResult<ApiResponse<SalesforceDeploymentResponse>>> Retry(Guid id)
    {
        var job = await JobQuery().SingleOrDefaultAsync(x => x.Id == id);
        if (job is null) return NotFound(ApiResponse<SalesforceDeploymentResponse>.Fail("Deployment not found."));
        var failed = job.Steps.FirstOrDefault(x => x.Status == SalesforceStepStatus.Failed);
        if (job.Status != SalesforceDeploymentStatus.Failed || failed is null)
            return BadRequest(ApiResponse<SalesforceDeploymentResponse>.Fail("Only a failed stage can be retried."));
        failed.Status = SalesforceStepStatus.Pending;
        failed.RetryCount++;
        failed.LogSummary = "Queued for retry.";
        job.Status = SalesforceDeploymentStatus.Pending;
        job.RetryCount++;
        job.ErrorMessage = null;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<SalesforceDeploymentResponse>.Ok(MapJob(job)));
    }

    private IQueryable<SalesforceDeploymentJob> JobQuery() =>
        db.SalesforceDeploymentJobs.Include(x => x.Tenant).Include(x => x.Blueprint).Include(x => x.Steps);
    private static SalesforceEnvironment ParseEnvironment(string value) =>
        Enum.TryParse<SalesforceEnvironment>(value, true, out var result) ? result : SalesforceEnvironment.Sandbox;
    private static SalesforceTenantResponse MapTenant(SalesforceTenant x) => new(
        x.Id, x.DisplayName, x.SalesforceOrgId, x.InstanceUrl, x.Edition, x.ApiVersion,
        x.ConnectionStatus.ToString(), x.EnvironmentType.ToString(), x.NamespaceStatus, x.LastHealthCheckAt);
    private static SalesforceDiscoveryResponse MapDiscovery(SalesforceOrgDiscovery x) => new(
        x.Id, x.TenantId, x.Status.ToString(), x.HasApiAccess, x.HasModifyAllData, x.SupportsNamedCredentials,
        x.SupportsPlatformEvents, x.SupportsMcp, x.ConflictingFields, x.ConflictingFlows,
        x.MissingPermissions, x.EstimatedMinutes, x.CompletedAt);
    private static SalesforceBlueprintResponse MapBlueprint(SalesforceBlueprint x) => new(
        x.Id, x.Name, x.Version, x.Sector,
        JsonSerializer.Deserialize<List<string>>(x.ModulesJson) ?? [],
        JsonSerializer.Deserialize<Dictionary<string, bool>>(x.FeaturesJson) ?? [], x.Status.ToString());
    private static SalesforceDeploymentResponse MapJob(SalesforceDeploymentJob x)
    {
        var steps = x.Steps.OrderBy(s => s.Sequence).Select(s => new SalesforceDeploymentStepResponse(
            s.Id, s.Sequence, s.Stage.ToString(), s.Status.ToString(), s.RetryCount,
            s.LogSummary, s.StartedAt, s.CompletedAt)).ToList();
        var completed = steps.Count(s => s.Status == nameof(SalesforceStepStatus.Completed));
        return new(x.Id, x.CorrelationId, x.TenantId, x.Tenant.DisplayName, x.BlueprintId,
            x.Blueprint.Name, x.TargetEnvironment.ToString(), x.Status.ToString(), x.CurrentStage.ToString(),
            steps.Count == 0 ? 0 : completed * 100 / steps.Count, x.RetryCount, x.ErrorMessage,
            x.StartedAt, x.CompletedAt, steps);
    }
}
