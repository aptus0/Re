using CommunityToolkit.Mvvm.ComponentModel;

namespace Re.Desktop.ViewModels.Shell;

public partial class WorkspaceTab : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _route = string.Empty;
    [ObservableProperty] private object? _content;
    [ObservableProperty] private bool _isCloseable = true;
}
