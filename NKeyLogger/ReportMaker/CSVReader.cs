using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace ReportMaker;
internal class CSVReader : IDisposable
{
    private const short HEADER_PARAMS = 4;
    private FileStream stream { get; init; }
    private StreamReader reader { get; init; }
    private readonly string newPath = string.Empty;
    public CSVReader(string path)
    {
        FileStreamOptions options = new FileStreamOptions();
        options.Share = FileShare.None;
        options.Mode = FileMode.Open;
        options.Access = FileAccess.Read;
        newPath = AppDomain.CurrentDomain.BaseDirectory + "\\" + System.IO.Path.GetFileName(path) + ".copy";

        if (File.Exists(newPath))
            File.Delete(newPath);

        copyFile(path, newPath);
        stream = new FileStream(newPath, options);
        this.reader = new StreamReader(stream);
    }

    ~CSVReader()
    {
        this.Dispose();
    }

    private void copyFile(string sourcePath, string distPath)
    {
        short exceptionCount = 2;
    again:
        try
        {
            File.Copy(sourcePath, distPath);
            --exceptionCount;
        }
        catch (IOException)
        {
            if (exceptionCount <= 0)
                throw;
            File.Delete(newPath);
            goto again;
        }
    }
    private bool checkFormat(string? header)
    {
        if (header == null)
            return false;
        string[] param = header.Split(";");
        string[] idealParam = new string[]{ "key", "language", "processName", "timestamp"};
        if (param.Length != idealParam.Length)
            return false;
        for (int i = 0; i < param.Length; i++)
        {
            if (param[i] != idealParam[i])
                return false;
        }
        return true;
    }
    public void read(Action<KeyInfo?> action)
    {
        stream.Seek(0, SeekOrigin.Begin);
        string? header = reader.ReadLine();

        if (!checkFormat(header))
            throw new ApplicationException("Error parse file");

        KeyInfo? value = null; 
        Result<KeyInfo> key;
        while ( (key = readLine(reader)).isSuccess )
        {
            value = key.Value;
            if (value == null)
                continue;
            action.Invoke(value);
        }
        EndOfFileEvent?.Invoke(value);
    }

    private Result<KeyInfo> readLine(StreamReader reader)
    {
        string? line = reader.ReadLine();
        if (line == null)
            return Result<KeyInfo>.Failure("Read failed");
        return KeyInfo.fromCSVString(line);
    }

    public void Dispose()
    {
        reader.Close();
        stream.Close();
        File.Delete(newPath);
    }

    public delegate void EndOfFileDelegate(KeyInfo? info);
    public event EndOfFileDelegate? EndOfFileEvent;

}
