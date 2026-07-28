using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace Re.Desktop.ViewModels.Settings;

public partial class ApiSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _machineIp = "Loading...";

    [ObservableProperty]
    private string _apiPort = "5188";

    [ObservableProperty]
    private string _serviceStatus = "Bilinmiyor";

    private const string ServiceName = "Re.Api";

    public ApiSettingsViewModel()
    {
        MachineIp = GetLocalIpAddress();
        RefreshStatus();
    }

    private string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "IP Not Found";
        }
        catch
        {
            return "Error";
        }
    }

    [RelayCommand]
    private void RefreshStatus()
    {
        try
        {
            var p = new Process();
            p.StartInfo.FileName = "sc.exe";
            p.StartInfo.Arguments = $"query {ServiceName}";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            if (output.Contains("RUNNING"))
                ServiceStatus = "Running";
            else if (output.Contains("STOPPED"))
                ServiceStatus = "Durduruldu";
            else
                ServiceStatus = "Not Installed";
        }
        catch
        {
            ServiceStatus = "Kontrol Edilemedi";
        }
    }

    [RelayCommand]
    private void InstallService()
    {
        var exePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Re.Api", "bin", "Debug", "net10.0", "Re.Api.exe"));
        if (!File.Exists(exePath))
        {
            exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Re.Api.exe");
        }

        if (!File.Exists(exePath))
        {
            MessageBox.Show("Re.Api.exe was not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RunAdminCommand("sc.exe", $"create {ServiceName} binPath= \"{exePath}\" start= auto displayname= \"Re Business Solutions API\"");
        RefreshStatus();
    }

    [RelayCommand]
    private void StartService()
    {
        RunAdminCommand("net", $"start {ServiceName}");
        RefreshStatus();
    }

    [RelayCommand]
    private void StopService()
    {
        RunAdminCommand("net", $"stop {ServiceName}");
        RefreshStatus();
    }

    [RelayCommand]
    private void StartLocalApi()
    {
        // Re.Desktop projesindeki ApiRunnerService kullanılıyor
        Re.Desktop.Services.ApiRunnerService.StartApi();
        MessageBox.Show("The local API start signal was sent. Checking port 5188...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RunAdminCommand(string fileName, string arguments)
    {
        try
        {
            var p = new Process();
            p.StartInfo.FileName = fileName;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.Verb = "runas"; // Directionetici izni
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            p.WaitForExit();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The operation was denied or failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
