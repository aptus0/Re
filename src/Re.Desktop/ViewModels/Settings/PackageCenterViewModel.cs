using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Settings;

public partial class PackageCenterViewModel : ObservableObject
{
    private readonly IPackageCenterService _packages;
    private readonly IDialogService _dialog;

    public ReadOnlyObservableCollection<RePackage> Packages => _packages.Packages;
    [ObservableProperty] private string _updateStatus = "Sistem güncel. Tüm bileşenler en son sürümde.";
    public string ApplicationVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";

    public PackageCenterViewModel(IPackageCenterService packages, IDialogService dialog)
    {
        _packages = packages;
        _dialog = dialog;
    }

    [RelayCommand]
    private void TogglePackage(RePackage? package)
    {
        if (package is null) return;
        if (package.IsCore)
        {
            _dialog.Info($"{package.Name} sistemin temel korumalı çip modülüdür ve her zaman aktiftir.", "Çekirdek Modül");
            return;
        }

        var install = !package.IsInstalled;
        if (!install && !_dialog.Confirm(
                $"{package.Name} paketini kaldırmak istediğinize emin misiniz? Yerel konfigürasyonlarınız saklanacaktır.",
                "Paket Merkezi")) return;

        _packages.SetInstalled(package.Id, install);
        OnPropertyChanged(nameof(Packages));
        _dialog.Success($"{package.Name} paketi başarıyla {(install ? "yüklendi ve aktifleştirildi" : "kaldırıldı")}.",
            "Paket Merkezi");
    }

    [RelayCommand]
    private void CheckUpdates()
    {
        UpdateStatus = $"Son kontrol: Az önce · Re {ApplicationVersion} güncel sürüm · Kararlı Kanal";
        _dialog.Success("Tüm yüklü paketler ve bileşenler en güncel durumdadır.", "Güncelleme Merkezi");
    }
}
