using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Re.Desktop.ViewModels.Funding;

public partial class FundingIntelligenceViewModel : ObservableObject
{
    public ObservableCollection<FundingApplicationItem> Applications { get; } = new();
    public ObservableCollection<PolicyFindingItem> PolicyFindings { get; } = new();
    public ObservableCollection<DocumentCheckItem> Documents { get; } = new();

    [ObservableProperty] private FundingApplicationItem? _selectedApplication;
    [ObservableProperty] private string _activeFilter = "All";
    [ObservableProperty] private string _decisionNote = string.Empty;

    public int OpenApplicationCount => Applications.Count(x => x.Status != "Approved" && x.Status != "Declined");
    public int ReviewCount => Applications.Count(x => x.Status == "Analyst Review");
    public decimal RequestedTotal => Applications.Sum(x => x.RequestedAmount);
    public decimal OfferTotal => Applications.Sum(x => x.ProposedOffer);

    public FundingIntelligenceViewModel()
    {
        Applications.Add(new("RF-2026-1048", "Atlas Retail Inc.", "Retail", 2_400_000, 1_850_000, 82, "Offer Ready", "12 min", "Emma Stone"));
        Applications.Add(new("RF-2026-1047", "Nova Foods Ltd.", "Food & Beverage", 1_200_000, 850_000, 68, "Analyst Review", "38 min", "Mark Miller"));
        Applications.Add(new("RF-2026-1046", "Mavi Rota Lojistik", "Logistics", 3_000_000, 0, 41, "Policy Exception", "1 h 12 min", "Selin Searchs"));
        Applications.Add(new("RF-2026-1045", "Urban Fashion Retail", "Fashion Retail", 950_000, 720_000, 76, "Document Review", "24 min", "Emma Stone"));
        Applications.Add(new("RF-2026-1044", "Orion Teknoloji", "Technology", 1_750_000, 1_400_000, 88, "Approved", "Completed", "Murat Demir"));

        SelectedApplication = Applications[0];
    }

    partial void OnSelectedApplicationChanged(FundingApplicationItem? value)
    {
        PolicyFindings.Clear();
        Documents.Clear();
        if (value is null) return;

        PolicyFindings.Add(new("Verified revenue trend", "+18% over 6 months", "Positive"));
        PolicyFindings.Add(new("Collection discipline", "96.4% collected on time", "Positive"));
        PolicyFindings.Add(new("Existing exposure", "31% of policy limit", "Neutral"));
        PolicyFindings.Add(new("Cash-flow volatility", "Two high-variance weeks detected", "Review"));

        Documents.Add(new("Bank statements", "6 / 6 months", "Verified"));
        Documents.Add(new("Tax certificate", "Valid until 31.12.2026", "Verified"));
        Documents.Add(new("Signature circular", "Manual identity check required", "Review"));
        Documents.Add(new("Trade registry", "Current", "Verified"));

        OnPropertyChanged(nameof(SelectedScoreLabel));
        OnPropertyChanged(nameof(SelectedRiskLabel));
    }

    public string SelectedScoreLabel => SelectedApplication is null ? "—" : $"{SelectedApplication.Score}/100";
    public string SelectedRiskLabel => SelectedApplication?.Score switch
    {
        >= 80 => "Low risk",
        >= 65 => "Moderate risk",
        _ => "Elevated risk"
    };

    [RelayCommand]
    private void SetFilter(string filter) => ActiveFilter = filter;

    [RelayCommand]
    private void StartReview()
    {
        if (SelectedApplication is null) return;
        SelectedApplication.Status = "Analyst Review";
        OnPropertyChanged(nameof(SelectedApplication));
    }

    [RelayCommand]
    private void PrepareOffer()
    {
        if (SelectedApplication is null) return;
        SelectedApplication.Status = "Offer Ready";
        if (SelectedApplication.ProposedOffer == 0)
            SelectedApplication.ProposedOffer = Math.Round(SelectedApplication.RequestedAmount * 0.65m, 0);
        OnPropertyChanged(nameof(SelectedApplication));
        OnPropertyChanged(nameof(OfferTotal));
    }
}

public partial class FundingApplicationItem : ObservableObject
{
    public FundingApplicationItem(string number, string merchant, string sector, decimal requestedAmount,
        decimal proposedOffer, int score, string status, string sla, string owner)
    {
        Number = number;
        Merchant = merchant;
        Sector = sector;
        RequestedAmount = requestedAmount;
        ProposedOffer = proposedOffer;
        Score = score;
        Status = status;
        Sla = sla;
        Owner = owner;
    }

    public string Number { get; }
    public string Merchant { get; }
    public string Sector { get; }
    public decimal RequestedAmount { get; }
    [ObservableProperty] private decimal _proposedOffer;
    public int Score { get; }
    [ObservableProperty] private string _status;
    public string Sla { get; }
    public string Owner { get; }
}

public record PolicyFindingItem(string Title, string Detail, string Severity);
public record DocumentCheckItem(string Name, string Detail, string Status);
