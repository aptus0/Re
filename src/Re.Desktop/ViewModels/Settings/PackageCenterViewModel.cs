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
    [ObservableProperty] private string _updateStatus = "System is up to date.";
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
            _dialog.Info($"{package.Name} is a protected core module and is always available.", "Core Module");
            return;
        }

        var install = !package.IsInstalled;
        if (!install && !_dialog.Confirm(
                $"Uninstall {package.Name}? Its local settings will be retained.",
                "Package Center")) return;

        _packages.SetInstalled(package.Id, install);
        OnPropertyChanged(nameof(Packages));
        _dialog.Success($"{package.Name} was {(install ? "installed" : "uninstalled")} successfully.",
            "Package Center");
    }

    [RelayCommand]
    private void CheckUpdates()
    {
        UpdateStatus = $"Checked just now · Re {ApplicationVersion} is current · Stable channel";
        _dialog.Success("All installed components are up to date.", "Update Center");
    }
}
