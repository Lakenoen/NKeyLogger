using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace ReportMaker;
internal class ReportGenerator
{
    private const short DATE_THRESHOLD = 5;
    private static string TEMP_PATH = AppDomain.CurrentDomain.BaseDirectory + "temp";
    private readonly Dictionary<string,Context> contextMap = new Dictionary<string,Context>();
    private DirectoryInfo? dirInfo;
    private readonly Dictionary<string, ulong> dateMap = new Dictionary<string, ulong>();
    private string sourcePath { get; init; }
    public ReportGenerator(string source)
    {
        this.sourcePath = source;
    }

    public void generateRawReport(string targetPath)
    {

        if (File.Exists(targetPath))
            File.Delete(targetPath);

        foreach (var item in contextMap.Values)
            item.isFirstToken = true;

        dirInfo = Directory.CreateDirectory(TEMP_PATH);
        using CSVReader reader = new CSVReader(this.sourcePath);

        reader.read(appendToRawReport);
        foreach (var pair in contextMap)
        {
            if (!File.Exists(pair.Value.TempFilePath))
                continue;
            File.AppendAllText(targetPath, pair.Key + ":\r\n");
            joinFile(targetPath, pair.Value.TempFilePath);
            File.Delete(pair.Value.TempFilePath);
            File.AppendAllText(targetPath, "\r\n");
        }

        this.clear();
    }

    private void clear()
    {
        contextMap.Clear();
        dateMap.Clear();
        dirInfo?.Delete();
    }

    private void joinFile(string mainFilePath, string childFilePath)
    {
        using (FileStream fileStream = File.Open(mainFilePath,FileMode.OpenOrCreate))
        {
            using StreamWriter writer = new StreamWriter(fileStream);
            fileStream.Seek(fileStream.Length, SeekOrigin.Begin);
            using StreamReader reader = new StreamReader(childFilePath);
            string? line = null;
            while ((line = reader.ReadLine()) != null)
            {
                writer.WriteLine(line);
            }
        }
    }
    private void appendToRawReport(KeyInfo? info)
    {
        if (info == null)
            return;

        var ctx = getContext(info);

        string newLine = (!ctx.isFirstToken) ? "\r\n" : "";
        if (isDateDiffMoreThreshold(info.Timestamp, ctx.CurrentKey?.Timestamp) || ctx.isFirstToken) { 
            File.AppendAllText(ctx.TempFilePath, newLine + info.Timestamp + ": ");
            ctx.isFirstToken = false;
            dateMap[info.Timestamp] = 0;
            ctx.lastTimestamp = info.Timestamp;
        }

        if (info.Key.Length > 1)
            File.AppendAllText(ctx.TempFilePath, $"[{info.Key}]");
        else
            File.AppendAllText(ctx.TempFilePath, info.Key);

        ++dateMap[ctx.lastTimestamp];

        ctx.CurrentKey = info;
    }

    private bool isDateDiffMoreThreshold(string timestampFirst, string? timestampSecond)
    {
        if (timestampSecond == null)
            return true;
        DateTime t1 = DateTime.Parse(timestampFirst);
        DateTime t2 = DateTime.Parse(timestampSecond);
        return DATE_THRESHOLD < (t1 - t2).TotalMinutes;
    }

    private Context getContext(KeyInfo info)
    {
        Context? ctx;
        contextMap.TryGetValue(info.ProcessName, out ctx);
        if (ctx == null)
            ctx = (contextMap[info.ProcessName] = new Context(TEMP_PATH));
        ctx.TempFilePath = info.ProcessName;
        return ctx;
    }

    class Context(string TempDirectoryPath)
    {
        private readonly string TempDirectoryPath = TempDirectoryPath;
        private string tempFilePath = "";
        public  KeyInfo? CurrentKey { get; set; } = null;
        public bool isFirstToken { get; set; } = true;
        public string lastTimestamp { get; set; } = string.Empty;

        public string TempFilePath
        {
            get { return tempFilePath; }
            set
            {
                tempFilePath = TempDirectoryPath + "\\" + System.IO.Path.GetFileName(value) + ".temp";
            }
        }

    }
}