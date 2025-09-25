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
    private ulong dateThresholdMinute = 5;
    private static string TEMP_PATH = AppDomain.CurrentDomain.BaseDirectory + "temp";
    private readonly Dictionary<string,Context> contextMap = new Dictionary<string,Context>();
    private readonly Dictionary<DateTime, ulong> dateMap = new Dictionary<DateTime, ulong>();
    private readonly Dictionary<string, int> langMap = new Dictionary<string, int>();
    private readonly List<ulong> times = new List<ulong>();
    private string sourcePath { get; init; }
    public ReportGenerator(string source)
    {
        this.sourcePath = source;
    }

    public void generateReport(string targetPath)
    {

        if (File.Exists(targetPath))
            File.Delete(targetPath);

        foreach (var item in contextMap.Values)
            item.isFirstToken = true;

        Directory.CreateDirectory(TEMP_PATH);
        File.AppendAllText(targetPath, "<Report>\r\n");
        File.AppendAllText(targetPath, $"Source: \"{sourcePath}\"\r\n\r\n");


        using CSVReader reader = new CSVReader(this.sourcePath);

        reader.read(takeTime);
        dateThresholdMinute = (ulong) getMedian(getIntevalTable(times));
        dateThresholdMinute = (dateThresholdMinute == 0) ? 1 : dateThresholdMinute;

        reader.read(append);
        foreach (var pair in contextMap)
        {
            if (!File.Exists(pair.Value.TempFilePath))
                continue;
            File.AppendAllText(targetPath, "Application: \"" + pair.Key + "\"\r\n");
            joinFile(targetPath, pair.Value.TempFilePath);
            File.Delete(pair.Value.TempFilePath);
            File.AppendAllText(targetPath, "\r\n");
        }

        KeyValuePair<DateTime, ulong> maxPair = new KeyValuePair<DateTime,ulong>();
        foreach (var pair in dateMap)
        {
            if (pair.Value > maxPair.Value)
                maxPair = pair;
        }
        File.AppendAllText(targetPath, "Additional information:\r\n");
        File.AppendAllText(targetPath, $"The peak of activity: {maxPair.Key} - {maxPair.Key.AddMinutes(dateThresholdMinute)}\r\n");
        
        int allSumbolsCount = langMap.Sum(x => x.Value);
        foreach ( var p in langMap)
        {
            double percent = Math.Round ( (double) p.Value / (double) allSumbolsCount * 100.0 );
            File.AppendAllText(targetPath, $"{p.Key}: {percent}%\r\n");
        }
        this.clear();
    }

    private void clear()
    {
        contextMap.Clear();
        dateMap.Clear();
        times.Clear();
        langMap.Clear();
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

    DateTime lastDateTime = DateTime.MinValue;
    private void takeTime(KeyInfo? info)
    {
        if (info == null)
            return;

        DateTime currentTapKeyTime = DateTime.Parse(info.Timestamp);

        if (lastDateTime == DateTime.MinValue)
        {
            lastDateTime = DateTime.Parse(info.Timestamp);
            return;
        }

        ulong diff = Convert.ToUInt64((currentTapKeyTime - lastDateTime).TotalMinutes);
        lastDateTime = currentTapKeyTime;

        times.Add(diff);
    }

    private Dictionary<Interval, int> getIntevalTable(List<ulong> list)
    {
        var res = new Dictionary<Interval, int>();
        ulong sum = 0;
        foreach (ulong el in list)
            sum += el;
        ulong min = list.Min();
        ulong max = list.Max();
        ulong diff = max - min;
        ulong h = diff / (ulong)(1.0 + 3.3221 * Math.Log10(sum));

        List<Interval> ranges = new List<Interval>();

        for( ulong i = min; i < max; i += h)
            ranges.Add(new Interval( i, i + h ));

        foreach (Interval el in ranges)
        {
            res[el] = list.FindAll(i =>
            {
                if(i >= (ulong)el.Start && i < (ulong)el.End)
                    return true;
                return false;
            }).Count;
        }

        return res;
    }

    private int getMedian(Dictionary<Interval, int> data)
    {
        if (data.Count == 0)
            throw new ArgumentException("Frequency table is empty");

        var values = data.Values.ToList();
        values.Sort();
        var sum = values.Sum();
        int acc = 0;
        int index = 0;
        while( acc < sum / 2)
        {
            if(acc == 0)
                acc = values[index++];
            else
                acc += values[index++];
        }

        --index;

        Interval? midInt = null;

        foreach (var item in data)
        {
            if (item.Value == values[index])
                midInt = item.Key;
        }

        if (midInt is null)
            throw new ArgumentException("Median interva lwas be null");

        return ( (sum / 2) - values[index-1] ) * (int)midInt.Value.Start / values[index];
    }
    private void append(KeyInfo? info)
    {
        if (info == null)
            return;

        var ctx = getContext(info);

        string newLine = (!ctx.isFirstToken) ? "\r\n" : "";
        if (isDateDiffMoreThreshold(info.Timestamp, ctx.CurrentKey?.Timestamp) || ctx.isFirstToken) { 
            File.AppendAllText(ctx.TempFilePath, newLine + info.Timestamp + ": ");
            ctx.isFirstToken = false;
            dateMap[DateTime.Parse(info.Timestamp)] = 0;
            ctx.lastTimestamp = info.Timestamp;
        }

        if (info.Key.Length > 1)
            File.AppendAllText(ctx.TempFilePath, $"[{info.Key}]");
        else
            File.AppendAllText(ctx.TempFilePath, info.Key);

        langMap[info.Language] = (langMap.ContainsKey(info.Language)) ? langMap[info.Language] + 1 : 1;
        ++dateMap[DateTime.Parse(ctx.lastTimestamp)];

        ctx.CurrentKey = info;
    }

    private bool isDateDiffMoreThreshold(string timestampFirst, string? timestampSecond)
    {
        if (timestampSecond == null)
            return true;
        DateTime t1 = DateTime.Parse(timestampFirst);
        DateTime t2 = DateTime.Parse(timestampSecond);
        return dateThresholdMinute < (t1 - t2).TotalMinutes;
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

    private struct Interval(ulong start, ulong end)
    {
        public ulong Start { get; set; } = start;
        public ulong End { get; set; } = end;

        public override int GetHashCode()
        {
            return HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
        }

        public ulong getMid()
        {
            return (Start + End) / 2;
        }
    }

}