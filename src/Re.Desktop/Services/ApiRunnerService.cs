using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace Re.Desktop.Services;

public static class ApiRunnerService
{
    private static readonly object Sync = new();
    private static Process? apiProcess;
    private static readonly Uri HealthUri = new("http://localhost:5188/health");

    public static bool StartApi()
    {
        lock (Sync)
        {
            if (IsApiReady()) return true;

            // SC.exe Windows Service check
            if (IsServiceRunning("Re.Api") && WaitUntilReady(TimeSpan.FromSeconds(8))) return true;
            if (IsServiceInstalled("Re.Api"))
            {
                TryStartService("Re.Api");
                if (WaitUntilReady(TimeSpan.FromSeconds(10))) return true;

                if (Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Api")))
                {
                    WriteLog("Installed Re.Api Windows Service status pending.", null);
                    return false;
                }
            }

            var apiPath = ResolveApiPath();
            if (apiPath is null || !File.Exists(apiPath))
            {
                WriteLog("Re.Api executable not present. Running desktop in independent mode.", null);
                return false;
            }

            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ReSoft", "Re", "Logs");
                Directory.CreateDirectory(logDirectory);
                var outputPath = Path.Combine(logDirectory, "api-process.log");
                var startInfo = new ProcessStartInfo
                {
                    FileName = apiPath,
                    WorkingDirectory = Path.GetDirectoryName(apiPath)!,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Arguments = $"--parent-pid {Environment.ProcessId} --urls \"http://localhost:5188\""
                };
                startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
                apiProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                apiProcess.OutputDataReceived += (_, e) => AppendProcessOutput(outputPath, e.Data);
                apiProcess.ErrorDataReceived += (_, e) => AppendProcessOutput(outputPath, e.Data);
                apiProcess.Exited += (_, _) =>
                {
                    if (!IsApiReady())
                    {
                        WriteLog($"Local API process finished with code {apiProcess?.ExitCode}.", null);
                    }
                };

                if (!apiProcess.Start()) return false;
                apiProcess.BeginOutputReadLine();
                apiProcess.BeginErrorReadLine();
                return WaitUntilReady(TimeSpan.FromSeconds(12));
            }
            catch (Exception ex)
            {
                WriteLog("Local API process start deferred.", ex);
                return false;
            }
        }
    }

    private static string? ResolveApiPath()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var directCandidates = new[]
        {
            Path.Combine(baseDirectory.FullName, "Api", "Re.Api.exe"),
            Path.Combine(baseDirectory.FullName, "Re.Api.exe")
        };
        foreach (var candidate in directCandidates)
            if (File.Exists(candidate)) return candidate;

        for (var directory = baseDirectory; directory is not null; directory = directory.Parent)
        {
            var apiProject = Path.Combine(directory.FullName, "Re.Api");
            if (!Directory.Exists(apiProject)) continue;
            var configurations = baseDirectory.FullName.Contains(
                $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase)
                ? new[] { "Release", "Debug" }
                : new[] { "Debug", "Release" };
            foreach (var configuration in configurations)
            {
                var candidate = Path.Combine(apiProject, "bin", configuration, "net10.0", "Re.Api.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private static bool WaitUntilReady(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (IsApiReady()) return true;
            if (apiProcess is { HasExited: true }) return false;
            Thread.Sleep(200);
        }
        WriteLog("Local API health check timed out.", null);
        return false;
    }

    private static bool IsApiReady()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(700) };
            return client.GetAsync(HealthUri).GetAwaiter().GetResult().IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static bool IsServiceRunning(string serviceName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe", Arguments = $"query {serviceName}", UseShellExecute = false,
                RedirectStandardOutput = true, CreateNoWindow = true
            });
            if (process is null) return false;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static bool IsServiceInstalled(string serviceName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe", Arguments = $"query {serviceName}", UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true,
                CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
            });
            if (process is null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    private static void TryStartService(string serviceName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe", Arguments = $"start {serviceName}", UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true,
                CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
            });
            process?.WaitForExit(5000);
        }
        catch (Exception ex) { WriteLog("Windows Service start request failed.", ex); }
    }

    public static void StopApi()
    {
        lock (Sync)
        {
            try
            {
                if (apiProcess is { HasExited: false }) apiProcess.Kill(true);
                apiProcess?.Dispose();
            }
            catch { }
            finally { apiProcess = null; }
        }
    }

    private static void AppendProcessOutput(string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try { File.AppendAllText(path, $"[{DateTimeOffset.Now:O}] {value}{Environment.NewLine}"); }
        catch { }
    }

    private static void WriteLog(string message, Exception? exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReSoft", "Re", "Logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "api-runner.log"),
                $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}{exception}{Environment.NewLine}");
        }
        catch { }
    }
}
