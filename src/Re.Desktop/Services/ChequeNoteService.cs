using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Re.Desktop.Services;

public enum ChequeNoteType
{
    CustomerCheque,   // Müşteri Çeki (Alacak)
    CustomerNote,     // Müşteri Senedi (Alacak)
    SupplierCheque,   // Kendi Çekimiz (Borç)
    SupplierNote      // Borç Senedimiz (Borç)
}

public enum ChequeNoteStatus
{
    Portfolio,        // Portföyde
    Endorsed,         // Ciro Edildi
    Collected,        // Tahsil Edildi
    Paid,             // Ödendi
    Bounced,          // Karşılıksız / Ödenmemiş
    Cancelled         // İptal Edildi
}

public class ChequeNoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public ChequeNoteType Type { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime DueDate { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.Now;
    public ChequeNoteStatus Status { get; set; } = ChequeNoteStatus.Portfolio;
    public string BankName { get; set; } = string.Empty;
    public string Drawer { get; set; } = string.Empty; // Keşideci
    public string Description { get; set; } = string.Empty;
    
    // UI Helpers
    public string TypeDisplay => Type switch
    {
        ChequeNoteType.CustomerCheque => "Customer Cheque",
        ChequeNoteType.CustomerNote => "Customer Promissory Note",
        ChequeNoteType.SupplierCheque => "Our Cheque",
        ChequeNoteType.SupplierNote => "Our Promissory Note",
        _ => Type.ToString()
    };

    public string StatusDisplay => Status switch
    {
        ChequeNoteStatus.Portfolio => "In Portfolio",
        ChequeNoteStatus.Endorsed => "Endorsed (Ciro)",
        ChequeNoteStatus.Collected => "Collected",
        ChequeNoteStatus.Paid => "Paid",
        ChequeNoteStatus.Bounced => "Bounced / Unpaid",
        ChequeNoteStatus.Cancelled => "Cancelled",
        _ => Status.ToString()
    };

    public bool IsReceivable => Type == ChequeNoteType.CustomerCheque || Type == ChequeNoteType.CustomerNote;
    public bool IsPayable => Type == ChequeNoteType.SupplierCheque || Type == ChequeNoteType.SupplierNote;
}

public interface IChequeNoteService
{
    Task<List<ChequeNoteItem>> GetAllAsync();
    Task SaveAsync(ChequeNoteItem item);
    Task DeleteAsync(Guid id);
    Task<ChequeNoteItem?> GetByIdAsync(Guid id);
    Task SeedDefaultDataAsync();
}

public class ChequeNoteService : IChequeNoteService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReERP", "cheques-notes.json");

    public ChequeNoteService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        if (!File.Exists(FilePath))
        {
            _ = SeedDefaultDataAsync();
        }
    }

    public async Task<List<ChequeNoteItem>> GetAllAsync()
    {
        if (!File.Exists(FilePath)) return new List<ChequeNoteItem>();
        try
        {
            var content = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<ChequeNoteItem>>(content) ?? new List<ChequeNoteItem>();
        }
        catch
        {
            return new List<ChequeNoteItem>();
        }
    }

    public async Task SaveAsync(ChequeNoteItem item)
    {
        var list = await GetAllAsync();
        var index = list.FindIndex(x => x.Id == item.Id);
        if (index >= 0)
        {
            list[index] = item;
        }
        else
        {
            list.Add(item);
        }
        await WriteAllAsync(list);
    }

    public async Task DeleteAsync(Guid id)
    {
        var list = await GetAllAsync();
        list.RemoveAll(x => x.Id == id);
        await WriteAllAsync(list);
    }

    public async Task<ChequeNoteItem?> GetByIdAsync(Guid id)
    {
        var list = await GetAllAsync();
        return list.Find(x => x.Id == id);
    }

    public async Task SeedDefaultDataAsync()
    {
        var defaultData = new List<ChequeNoteItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Number = "CK-10023",
                Type = ChequeNoteType.CustomerCheque,
                AccountId = Guid.Parse("d7f8d000-001a-2b3c-4d5e-6f7a8b9c0d1e"),
                AccountName = "Global Ticaret A.Ş.",
                Amount = 120000.00m,
                Currency = "TRY",
                DueDate = DateTime.Today.AddDays(30),
                IssueDate = DateTime.Today.AddDays(-5),
                Status = ChequeNoteStatus.Portfolio,
                BankName = "Garanti BBVA - Levent",
                Drawer = "Ahmet Yılmaz",
                Description = "Sales agreement cheque installment #1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Number = "NT-40092",
                Type = ChequeNoteType.CustomerNote,
                AccountId = Guid.Parse("d7f8d000-001a-2b3c-4d5e-6f7a8b9c0d2f"),
                AccountName = "Koç Holding Enerji Grubu",
                Amount = 250000.00m,
                Currency = "TRY",
                DueDate = DateTime.Today.AddDays(45),
                IssueDate = DateTime.Today.AddDays(-10),
                Status = ChequeNoteStatus.Portfolio,
                Drawer = "Mustafa Kemal",
                Description = "Service contract promissory note"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Number = "CK-50011",
                Type = ChequeNoteType.SupplierCheque,
                AccountId = Guid.Parse("d7f8d000-001a-2b3c-4d5e-6f7a8b9c0d3a"),
                AccountName = "Anadolu Metal Çelik A.Ş.",
                Amount = 85000.00m,
                Currency = "TRY",
                DueDate = DateTime.Today.AddDays(15),
                IssueDate = DateTime.Today.AddDays(-12),
                Status = ChequeNoteStatus.Portfolio,
                BankName = "Akbank - Merkez",
                Drawer = "Re ERP Business",
                Description = "Raw material purchase payment"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Number = "NT-80024",
                Type = ChequeNoteType.SupplierNote,
                AccountId = Guid.Parse("d7f8d000-001a-2b3c-4d5e-6f7a8b9c0d4b"),
                AccountName = "Makina Sanayi Fabrikaları",
                Amount = 300000.00m,
                Currency = "TRY",
                DueDate = DateTime.Today.AddDays(-5),
                IssueDate = DateTime.Today.AddDays(-60),
                Status = ChequeNoteStatus.Bounced,
                Drawer = "Re ERP Business",
                Description = "CNC machine installment note"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Number = "CK-10024",
                Type = ChequeNoteType.CustomerCheque,
                AccountId = Guid.Parse("d7f8d000-001a-2b3c-4d5e-6f7a8b9c0d1e"),
                AccountName = "Global Ticaret A.Ş.",
                Amount = 90000.00m,
                Currency = "TRY",
                DueDate = DateTime.Today.AddDays(-2),
                IssueDate = DateTime.Today.AddDays(-32),
                Status = ChequeNoteStatus.Collected,
                BankName = "QNB Finansbank - Maslak",
                Drawer = "Ahmet Yılmaz",
                Description = "Sales agreement cheque installment #2 (Collected)"
            }
        };
        await WriteAllAsync(defaultData);
    }

    private async Task WriteAllAsync(List<ChequeNoteItem> list)
    {
        try
        {
            var content = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, content);
        }
        catch
        {
            // Ignore errors
        }
    }
}
