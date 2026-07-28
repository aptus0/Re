using Re.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Re.Persistence;

/// <summary>
/// Persistence katmanı DI kayıt extension metodu.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<ReDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("'DefaultConnection' connection string is missing.");
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
                    sql.CommandTimeout(120);
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                });
            }
            else
            {
                var sqlitePath = configuration["Database:SqlitePath"];
                if (string.IsNullOrWhiteSpace(sqlitePath))
                {
                    sqlitePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ReSoft", "Re", "Data", "Re.db");
                }

                sqlitePath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(sqlitePath));
                Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath)!);
                options.UseSqlite($"Data Source={sqlitePath};Cache=Shared;Foreign Keys=True");
            }

#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        return services;
    }

    /// <summary>
    /// Uygulama başlangıcında migration'ları otomatik uygular.
    /// </summary>
    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Re.Application.Common.Interfaces.IPasswordHasher>();

        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.EnsureCreatedAsync();
            await EnsureSqliteModuleTablesAsync(dbContext);
        }
        else
            await dbContext.Database.MigrateAsync();
        await SeedDefaultDataAsync(dbContext, hasher);
    }

    private static async Task EnsureSqliteModuleTablesAsync(ReDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS PurchaseInvoices (
                Id TEXT NOT NULL PRIMARY KEY,
                CompanyId TEXT NOT NULL,
                BranchId TEXT NOT NULL,
                SupplierId TEXT NOT NULL,
                WarehouseId TEXT NOT NULL,
                DocumentNumber TEXT NOT NULL,
                SupplierDocumentNumber TEXT NULL,
                DocumentDate TEXT NOT NULL,
                DueDate TEXT NULL,
                Status INTEGER NOT NULL,
                SubTotal TEXT NOT NULL,
                TaxAmount TEXT NOT NULL,
                TotalAmount TEXT NOT NULL,
                Currency TEXT NOT NULL,
                ExchangeRate TEXT NOT NULL,
                Notes TEXT NULL,
                ApprovedBy TEXT NULL,
                ApprovedAt TEXT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT NULL,
                UpdatedAt TEXT NULL,
                UpdatedBy TEXT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT NULL,
                DeletedBy TEXT NULL,
                RowVersion BLOB NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_PurchaseInvoices_CompanyId_DocumentNumber
                ON PurchaseInvoices (CompanyId, DocumentNumber);
            CREATE TABLE IF NOT EXISTS PurchaseInvoiceLines (
                Id TEXT NOT NULL PRIMARY KEY,
                PurchaseInvoiceId TEXT NOT NULL,
                ProductId TEXT NOT NULL,
                ProductVariantId TEXT NULL,
                ProductCode TEXT NOT NULL,
                ProductName TEXT NOT NULL,
                Quantity TEXT NOT NULL,
                UnitPrice TEXT NOT NULL,
                DiscountPercent TEXT NOT NULL,
                VatRate TEXT NOT NULL,
                LotNumber TEXT NULL,
                SerialNumber TEXT NULL,
                ExpiryDate TEXT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT NULL,
                UpdatedAt TEXT NULL,
                UpdatedBy TEXT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT NULL,
                DeletedBy TEXT NULL,
                RowVersion BLOB NULL,
                FOREIGN KEY (PurchaseInvoiceId) REFERENCES PurchaseInvoices (Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_PurchaseInvoiceLines_PurchaseInvoiceId
                ON PurchaseInvoiceLines (PurchaseInvoiceId);
            CREATE TABLE IF NOT EXISTS Orders (
                Id TEXT NOT NULL PRIMARY KEY,
                CompanyId TEXT NOT NULL, BranchId TEXT NOT NULL, AccountId TEXT NOT NULL,
                WarehouseId TEXT NOT NULL, OrderNumber TEXT NOT NULL,
                CustomerReference TEXT NULL, Type INTEGER NOT NULL, Status INTEGER NOT NULL,
                OrderDate TEXT NOT NULL, RequestedDeliveryDate TEXT NULL,
                Currency TEXT NOT NULL, ExchangeRate TEXT NOT NULL,
                SubTotal TEXT NOT NULL, TaxAmount TEXT NOT NULL, TotalAmount TEXT NOT NULL,
                Notes TEXT NULL, InvoiceId TEXT NULL, ConfirmedBy TEXT NULL, ConfirmedAt TEXT NULL,
                CreatedAt TEXT NOT NULL, CreatedBy TEXT NULL, UpdatedAt TEXT NULL, UpdatedBy TEXT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0, DeletedAt TEXT NULL, DeletedBy TEXT NULL,
                RowVersion BLOB NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Orders_CompanyId_OrderNumber
                ON Orders (CompanyId, OrderNumber);
            CREATE TABLE IF NOT EXISTS OrderLines (
                Id TEXT NOT NULL PRIMARY KEY, OrderId TEXT NOT NULL, ProductId TEXT NOT NULL,
                ProductVariantId TEXT NULL, ProductCode TEXT NOT NULL, ProductName TEXT NOT NULL,
                Quantity TEXT NOT NULL, FulfilledQuantity TEXT NOT NULL,
                UnitPrice TEXT NOT NULL, DiscountPercent TEXT NOT NULL, VatRate TEXT NOT NULL,
                Notes TEXT NULL, CreatedAt TEXT NOT NULL, CreatedBy TEXT NULL,
                UpdatedAt TEXT NULL, UpdatedBy TEXT NULL, IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT NULL, DeletedBy TEXT NULL, RowVersion BLOB NULL,
                FOREIGN KEY (OrderId) REFERENCES Orders (Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_OrderLines_OrderId ON OrderLines (OrderId);
            """);
    }

    private static async Task SeedDefaultDataAsync(ReDbContext db, Re.Application.Common.Interfaces.IPasswordHasher hasher)
    {
        // Permission seed'i – yoksa ekle
        if (!await db.Permissions.AnyAsync())
        {
            var permissions = new[]
            {
                // Genel
                ("System.Admin", "Sistem", "Sistem Yönetimi"),
                ("Company.View", "Firma", "Firma Görüntüle"),
                ("Company.Edit", "Firma", "Firma Düzenle"),
                // Ürün
                ("Product.View", "Stok", "Ürün Listesi"),
                ("Product.Create", "Stok", "Ürün Oluştur"),
                ("Product.Edit", "Stok", "Ürün Düzenle"),
                ("Product.Delete", "Stok", "Ürün Sil"),
                // Fatura
                ("Invoice.View", "Satış", "Fatura Görüntüle"),
                ("Invoice.Create", "Satış", "Fatura Oluştur"),
                ("Invoice.Approve", "Satış", "Fatura Onayla"),
                ("Invoice.Cancel", "Satış", "Fatura İptal"),
                // Stok
                ("Stock.View", "Stok", "Stok Görüntüle"),
                ("Stock.Move", "Stok", "Stok Hareketi"),
                // Cari
                ("Account.View", "Cari", "Cari Görüntüle"),
                ("Account.Create", "Cari", "Cari Oluştur"),
                ("Account.Edit", "Cari", "Cari Düzenle"),
                // Rapor
                ("Report.View", "Rapor", "Rapor Görüntüle"),
            };

            foreach (var (code, category, name) in permissions)
            {
                db.Permissions.Add(new Domain.Entities.Identity.Permission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Category = category,
                    Name = name,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        // Firma seed'i
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company == null)
        {
            company = Domain.Entities.Company.Company.Create("Şirketim", null);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
        }

        // Eski sürümlerde oluşturulan tanımlı demo firma adını gerçek kurulum adına dönüştür.
        if (company.Name == "Re Demo A.Ş.")
        {
            company.Update("Şirketim", company.TradeName, company.TaxNumber, company.TaxOffice,
                company.Phone, company.Email, company.Website, company.AddressLine1,
                company.AddressLine2, company.City, company.District, company.PostalCode,
                company.Country);
            await db.SaveChangesAsync();
        }

        // A fresh portable installation must be immediately usable by Invoice,
        // POS, Stock and Cash modules without manual database preparation.
        var branch = await db.Branches.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.CompanyId == company.Id);
        if (branch == null)
        {
            branch = Domain.Entities.Company.Branch.Create(company.Id, "MRK", "Merkez Şube", true);
            db.Branches.Add(branch);
            await db.SaveChangesAsync();
        }

        if (!await db.Warehouses.AnyAsync(x => x.BranchId == branch.Id))
        {
            db.Warehouses.Add(Domain.Entities.Company.Warehouse.Create(branch.Id, "MRK-D", "Merkez Depo", true));
        }

        if (!await db.CashRegisters.AnyAsync(x => x.BranchId == branch.Id))
        {
            db.CashRegisters.Add(new Domain.Entities.Accounting.CashRegister
            {
                Id = Guid.NewGuid(), BranchId = branch.Id, Code = "KASA-01",
                Name = "Merkez Kasa", Currency = "TRY", CreatedAt = DateTime.UtcNow
            });
        }

        if (!await db.Accounts.IgnoreQueryFilters().AnyAsync(x => x.CompanyId == company.Id && x.Code == "PESIN"))
        {
            db.Accounts.Add(Domain.Entities.Accounting.Account.Create(
                company.Id, "PESIN", "Peşin Müşteri", Domain.Enums.AccountType.Customer));
        }

        await db.SaveChangesAsync();

        // Admin Rolü seed'i
        var adminRole = await db.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Name == "Sistem Yöneticisi");
        if (adminRole == null)
        {
            adminRole = Domain.Entities.Identity.Role.Create(company.Id, "Sistem Yöneticisi", "Tam yetkili sistem yöneticisi", true);
            db.Roles.Add(adminRole);
            
            // Tüm yetkileri admine ata
            var allPermissions = await db.Permissions.ToListAsync();
            foreach (var perm in allPermissions)
            {
                db.RolePermissions.Add(new Domain.Entities.Identity.RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = perm.Id
                });
            }
            await db.SaveChangesAsync();
        }

        // Admin Kullanıcısı seed'i
        if (!await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            var adminUser = Domain.Entities.Identity.User.Create(
                company.Id,
                "admin",
                "admin@Re.com",
                "Sistem",
                "Yöneticisi",
                hasher.Hash("123456"),
                null
            );
            db.Users.Add(adminUser);

            db.UserRoles.Add(new Domain.Entities.Identity.UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });

            await db.SaveChangesAsync();
        }
    }
}

