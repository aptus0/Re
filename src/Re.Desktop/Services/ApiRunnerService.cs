using System.Diagnostics;
using System.IO;

namespace Re.Desktop.Services;

public static class ApiRunnerService
{
    private static Process? _apiProcess;

    public static void StartApi()
    {
        // Önce Windows Servis kontrolü yap
        if (IsServiceRunning("Re.Api"))
        {
            return; // Servis zaten çalışıyor
        }

        // Eğer servis kurulu değilse veya çalışmıyorsa yerel process olarak başlatmayı dene
        StartLocalProcess();
    }

    private static bool IsServiceRunning(string serviceName)
    {
        try
        {
            var p = new Process();
            p.StartInfo.FileName = "sc.exe";
            p.StartInfo.Arguments = $"query {serviceName}";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output.Contains("RUNNING");
        }
        catch
        {
            return false;
        }
    }

    private static void StartLocalProcess()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var apiExeName = "Re.Api.exe";
        var apiPath = Path.Combine(baseDir, apiExeName);

        // Fallback for development environment
        if (!File.Exists(apiPath))
        {
            var devPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Re.Api", "bin", "Debug", "net10.0", apiExeName));
            if (File.Exists(devPath))
            {
                apiPath = devPath;
            }
        }

        if (File.Exists(apiPath))
        {
            // Visual Studio (veya başka bir şekilde) tarafından zaten başlatılmışsa, öldürme!
            var existingProcesses = Process.GetProcessesByName("Re.Api");
            if (existingProcesses.Length > 0)
            {
                return; // Zaten çalışıyor, yeni bir tane başlatmaya gerek yok.
            }

            var currentProcessId = Environment.ProcessId;
            var startInfo = new ProcessStartInfo
            {
                FileName = apiPath,
                WorkingDirectory = Path.GetDirectoryName(apiPath) ?? baseDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"--parent-pid {currentProcessId} --urls \"http://localhost:5188\""
            };

            try
            {
                _apiProcess = Process.Start(startInfo);
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    public static void StopApi()
    {
        if (_apiProcess != null && !_apiProcess.HasExited)
        {
            try
            {
                _apiProcess.Kill();
                _apiProcess.Dispose();
            }
            catch { }
        }
    }
}
