using Re.Domain.Entities.Common;
using Re.Application.Interfaces;
using Re.Domain.Entities.Accounting;
using Re.Domain.Entities.Company;
using Re.Domain.Entities.Identity;
using Re.Domain.Entities.Inventory;
using Re.Domain.Entities.Sales;
using Re.Domain.Entities.Salesforce;
using Re.Domain.Entities.Purchasing;
using Re.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Re.Persistence.Context;

/// <summary>
/// Ana veritabanı bağlamı.
/// Soft delete global query filter, audit interceptor ve concurrency token burada yapılandırılır.
/// </summary>
public class ReDbContext : DbContext
{
    private readonly ICurrentTenantService? _currentTenantService;

    public ReDbContext(
        DbContextOptions<ReDbContext> options,
        ICurrentTenantService currentTenantService = null!) : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    // Company
    public DbSet<Company>    Companies  => Set<Company>();
    public DbSet<Branch>     Branches   => Set<Branch>();
    public DbSet<Warehouse>  Warehouses => Set<Warehouse>();

    // Identity
    public DbSet<User>           Users          => Set<User>();
    public DbSet<Role>           Roles          => Set<Role>();
    public DbSet<Permission>     Permissions    => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole>       UserRoles      => Set<UserRole>();
    public DbSet<RefreshToken>   RefreshTokens  => Set<RefreshToken>();

    // Inventory
    public DbSet<Product>        Products        => Set<Product>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Category>       Categories      => Set<Category>();
    public DbSet<Brand>          Brands          => Set<Brand>();
    public DbSet<Unit>           Units           => Set<Unit>();
    public DbSet<ProductCollection> ProductCollections => Set<ProductCollection>();
    public DbSet<StockMovement>  StockMovements  => Set<StockMovement>();

    // Sales
    public DbSet<Invoice>     Invoices     => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    // Accounting
    public DbSet<Account>            Accounts            => Set<Account>();
    public DbSet<AccountMovement>    AccountMovements    => Set<AccountMovement>();
    public DbSet<CashRegister>       CashRegisters       => Set<CashRegister>();
    public DbSet<CashRegisterMovement> CashRegisterMovements => Set<CashRegisterMovement>();
    public DbSet<BankAccount>        BankAccounts        => Set<BankAccount>();
    public DbSet<BankAccountMovement> BankAccountMovements => Set<BankAccountMovement>();

    // Salesforce control plane
    public DbSet<SalesforceTenant> SalesforceTenants => Set<SalesforceTenant>();
    public DbSet<SalesforceOrgDiscovery> SalesforceOrgDiscoveries => Set<SalesforceOrgDiscovery>();
    public DbSet<SalesforceBlueprint> SalesforceBlueprints => Set<SalesforceBlueprint>();
    public DbSet<SalesforceDeploymentJob> SalesforceDeploymentJobs => Set<SalesforceDeploymentJob>();
    public DbSet<SalesforceDeploymentStep> SalesforceDeploymentSteps => Set<SalesforceDeploymentStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Soft Delete Global Filter ───────────────────────────────────
        // Yalnızca kendi IsDeleted sütunu olan tablolara filtre uygula.
        // Junction tabloları (UserRole, RolePermission) ve tarih tabloları hariç.
        var excludedTypes = new[]
        {
            typeof(UserRole),
            typeof(RolePermission),
            typeof(RefreshToken),
            typeof(ProductBarcode),
            typeof(InvoiceLine),
            typeof(PurchaseInvoiceLine),
            typeof(OrderLine),
            typeof(AccountMovement)
        };

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (excludedTypes.Contains(entityType.ClrType)) continue;

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");

            // Soft Delete Filter
            var isDeletedProperty = entityType.FindProperty("IsDeleted");
            System.Linq.Expressions.LambdaExpression? softDeleteFilter = null;
            if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
            {
                softDeleteFilter = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(
                        System.Linq.Expressions.Expression.Property(parameter, "IsDeleted"),
                        System.Linq.Expressions.Expression.Constant(false)),
                    parameter);
            }

            // Multi-Tenant Filter (IMustHaveCompany)
            System.Linq.Expressions.LambdaExpression? tenantFilter = null;
            if (typeof(IMustHaveCompany).IsAssignableFrom(entityType.ClrType))
            {
                var property = System.Linq.Expressions.Expression.Property(parameter, "CompanyId");
                var tenantId = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(this),
                    nameof(CurrentCompanyId));
                
                // EF Core Expression.Equal types must match. Convert e.CompanyId (Guid) to Guid?
                var nullableProperty = System.Linq.Expressions.Expression.Convert(property, typeof(Guid?));

                tenantFilter = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(nullableProperty, tenantId),
                    parameter);
            }

            // Filtreleri Birleştir
            if (softDeleteFilter != null && tenantFilter != null)
            {
                var combined = System.Linq.Expressions.Expression.AndAlso(
                    softDeleteFilter.Body,
                    tenantFilter.Body
                );
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    System.Linq.Expressions.Expression.Lambda(combined, parameter));
            }
            else if (softDeleteFilter != null)
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(softDeleteFilter);
            }
            else if (tenantFilter != null)
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(tenantFilter);
            }
        }

        ConfigureIdentity(modelBuilder);
        ConfigureCompany(modelBuilder);
        var supportsTemporalTables = Database.IsSqlServer();
        ConfigureInventory(modelBuilder, supportsTemporalTables);
        ConfigureSales(modelBuilder, supportsTemporalTables);
          ConfigureAccounting(modelBuilder, supportsTemporalTables);
          ConfigureSalesforce(modelBuilder, supportsTemporalTables);
      }

    private static void ConfigureSalesforce(ModelBuilder m, bool isSqlServer)
    {
        m.Entity<SalesforceTenant>(e =>
        {
            e.ToTable("SalesforceTenants");
            e.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            e.Property(x => x.SalesforceOrgId).HasMaxLength(40).IsRequired();
            e.Property(x => x.InstanceUrl).HasMaxLength(300).IsRequired();
            e.Property(x => x.Edition).HasMaxLength(80);
            e.Property(x => x.ApiVersion).HasMaxLength(20);
            e.Property(x => x.ConnectedUserId).HasMaxLength(40);
            e.Property(x => x.CredentialReference).HasMaxLength(300);
            e.Property(x => x.NamespaceStatus).HasMaxLength(80);
            e.Property(x => x.ConnectionStatus).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.EnvironmentType).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.CompanyId, x.SalesforceOrgId }).IsUnique();
        });
        m.Entity<SalesforceOrgDiscovery>(e =>
        {
            e.ToTable("SalesforceOrgDiscoveries");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            var findings = e.Property(x => x.FindingsJson);
            if (isSqlServer) findings.HasColumnType("nvarchar(max)");
            e.HasOne(x => x.Tenant).WithMany(x => x.Discoveries).HasForeignKey(x => x.TenantId);
        });
        m.Entity<SalesforceBlueprint>(e =>
        {
            e.ToTable("SalesforceBlueprints");
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Version).HasMaxLength(30).IsRequired();
            e.Property(x => x.Sector).HasMaxLength(60).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => new { x.CompanyId, x.Name, x.Version }).IsUnique();
        });
        m.Entity<SalesforceDeploymentJob>(e =>
        {
            e.ToTable("SalesforceDeploymentJobs");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.CurrentStage).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.TargetEnvironment).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasIndex(x => x.CorrelationId).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(x => x.Deployments).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Blueprint).WithMany(x => x.Deployments).HasForeignKey(x => x.BlueprintId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<SalesforceDeploymentStep>(e =>
        {
            e.ToTable("SalesforceDeploymentSteps");
            e.Property(x => x.Stage).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.LogSummary).HasMaxLength(2000);
            e.HasIndex(x => new { x.DeploymentJobId, x.Sequence }).IsUnique();
            e.HasOne(x => x.DeploymentJob).WithMany(x => x.Steps).HasForeignKey(x => x.DeploymentJobId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ── Identity ──────────────────────────────────────────────────────────
    private static void ConfigureIdentity(ModelBuilder m)
    {
        m.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(250).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Username }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Email }).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        m.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        m.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // Junction tablo – navigation'lar isteğe bağlı (IsRequired=false)
        // böylece soft-delete filter çakışması önlenir
        m.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions");
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.HasOne(x => x.Role)
             .WithMany(r => r.RolePermissions)
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);  // Filter çakışmasını önler
            e.HasOne(x => x.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
        });

        m.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User)
             .WithMany(u => u.UserRoles)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
            e.HasOne(x => x.Role)
             .WithMany(r => r.UserRoles)
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);
        });

        m.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(500).IsRequired();
            e.HasOne(x => x.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ── Company ───────────────────────────────────────────────────────────
    private static void ConfigureCompany(ModelBuilder m)
    {
        m.Entity<Company>(e =>
        {
            e.ToTable("Companies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(250).IsRequired();
            e.Property(x => x.TaxNumber).HasMaxLength(11);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        m.Entity<Branch>(e =>
        {
            e.ToTable("Branches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Company).WithMany(c => c.Branches).HasForeignKey(x => x.CompanyId);
        });

        m.Entity<Warehouse>(e =>
        {
            e.ToTable("Warehouses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            e.HasOne(x => x.Branch).WithMany(b => b.Warehouses).HasForeignKey(x => x.BranchId);
        });
    }

    // ── Inventory ─────────────────────────────────────────────────────────
    private static void ConfigureInventory(ModelBuilder m, bool supportsTemporalTables)
    {
        m.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.ImagePath).HasMaxLength(500);
            e.Property(x => x.ShortName).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.SalePrice).HasPrecision(18, 4);
            e.Property(x => x.PurchasePrice).HasPrecision(18, 4);
            e.Property(x => x.DealerPrice).HasPrecision(18, 4);
            e.Property(x => x.VatRate).HasPrecision(5, 2);
            e.Property(x => x.MinStockLevel).HasPrecision(18, 4);
            e.Property(x => x.MaxStockLevel).HasPrecision(18, 4);
            e.Property(x => x.Weight).HasPrecision(10, 4);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        m.Entity<ProductBarcode>(e =>
        {
            e.ToTable("ProductBarcodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(100).IsRequired();
            e.Property(x => x.BarcodeType).HasMaxLength(20);
            e.Property(x => x.UnitQuantity).HasPrecision(18, 4);
            e.HasIndex(x => new { x.ProductId, x.Value }).IsUnique();
            e.HasOne(x => x.Product)
             .WithMany(p => p.Barcodes)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<ProductVariant>(e =>
        {
            e.ToTable("ProductVariants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.SalePrice).HasPrecision(18, 4);
            e.HasIndex(x => new { x.ProductId, x.Code }).IsUnique();
            e.HasOne(x => x.Product)
             .WithMany(p => p.Variants)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<StockMovement>(e =>
        {
            if (supportsTemporalTables) e.ToTable("StockMovements", t => t.IsTemporal());
            else e.ToTable("StockMovements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.StockAfterMovement).HasPrecision(18, 4);
        });

        m.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.ParentCategory)
             .WithMany(c => c.SubCategories)
             .HasForeignKey(x => x.ParentCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Brand>(e =>
        {
            e.ToTable("Brands");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        m.Entity<Unit>(e =>
        {
            e.ToTable("Units");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.Abbreviation).HasMaxLength(10);
        });
        m.Entity<ProductCollection>(e =>
        {
            e.ToTable("ProductCollections");
            e.Property(x => x.Code).HasMaxLength(40).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Season).HasMaxLength(80);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });
    }

    // ── Sales ─────────────────────────────────────────────────────────────
    private static void ConfigureSales(ModelBuilder m, bool supportsTemporalTables)
    {
        m.Entity<Invoice>(e =>
        {
            if (supportsTemporalTables) e.ToTable("Invoices", t => t.IsTemporal());
            else e.ToTable("Invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 4);
            e.Property(x => x.TotalAmount).HasPrecision(18, 4);
            e.Property(x => x.PaidAmount).HasPrecision(18, 4);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.CompanyId, x.DocumentNumber }).IsUnique();
        });

        m.Entity<InvoiceLine>(e =>
        {
            e.ToTable("InvoiceLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.VatRate).HasPrecision(5, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.HasOne(x => x.Invoice)
             .WithMany(i => i.Lines)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Order>(e =>
        {
            if (supportsTemporalTables) e.ToTable("Orders", t => t.IsTemporal());
            else e.ToTable("Orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomerReference).HasMaxLength(100);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            e.Property(x => x.SubTotal).HasPrecision(18, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 4);
            e.Property(x => x.TotalAmount).HasPrecision(18, 4);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
        });

        m.Entity<OrderLine>(e =>
        {
            e.ToTable("OrderLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(50);
            e.Property(x => x.ProductName).HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.FulfilledQuantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.VatRate).HasPrecision(5, 2);
            e.HasOne(x => x.Order).WithMany(x => x.Lines).HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ── Accounting ────────────────────────────────────────────────────────
    private static void ConfigureAccounting(ModelBuilder m, bool supportsTemporalTables)
    {
        m.Entity<Account>(e =>
        {
            e.ToTable("Accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.TcKimlik).HasMaxLength(11);
            e.Property(x => x.CurrentBalance).HasPrecision(18, 4);
            e.Property(x => x.CreditLimit).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        m.Entity<AccountMovement>(e =>
        {
            if (supportsTemporalTables) e.ToTable("AccountMovements", t => t.IsTemporal());
            else e.ToTable("AccountMovements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.Property(x => x.RunningBalance).HasPrecision(18, 4);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            e.HasOne(x => x.Account)
             .WithMany(a => a.Movements)
             .HasForeignKey(x => x.AccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<CashRegister>(e =>
        {
            e.ToTable("CashRegisters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CurrentBalance).HasPrecision(18, 4);
        });

        m.Entity<CashRegisterMovement>(e =>
        {
            if (supportsTemporalTables) e.ToTable("CashRegisterMovements", t => t.IsTemporal());
            else e.ToTable("CashRegisterMovements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.Property(x => x.RunningBalance).HasPrecision(18, 4);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            e.HasOne(x => x.CashRegister)
             .WithMany()
             .HasForeignKey(x => x.CashRegisterId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<BankAccount>(e =>
        {
            e.ToTable("BankAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.BankName).HasMaxLength(200).IsRequired();
            e.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
            e.Property(x => x.AccountNumber).HasMaxLength(30);
            e.Property(x => x.Iban).HasMaxLength(34);
            e.Property(x => x.CurrentBalance).HasPrecision(18, 4);
        });

        m.Entity<BankAccountMovement>(e =>
        {
            if (supportsTemporalTables) e.ToTable("BankAccountMovements", t => t.IsTemporal());
            else e.ToTable("BankAccountMovements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.Property(x => x.RunningBalance).HasPrecision(18, 4);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            e.HasOne(x => x.BankAccount)
             .WithMany()
             .HasForeignKey(x => x.BankAccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public Guid? CurrentCompanyId => _currentTenantService?.CompanyId;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantIdAutomatically();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetTenantIdAutomatically();
        return base.SaveChanges();
    }

    private void SetTenantIdAutomatically()
    {
        var tenantId = _currentTenantService?.CompanyId;
        if (tenantId == null || tenantId == Guid.Empty)
            return;

        foreach (var entry in ChangeTracker.Entries<IMustHaveCompany>())
        {
            if (entry.State == EntityState.Added)
            {
                // Mevcut değer Guid.Empty veya set edilmemişse otomatik ata
                if (entry.Entity.CompanyId == Guid.Empty)
                {
                    entry.Entity.CompanyId = tenantId.Value;
                }
            }
        }
    }
}

