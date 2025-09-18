using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace NKeyLoggerServer;
internal class CSVFile : IDisposable
{
    public FileInfo fileInfo { get; private set; }
    private FileStream fileStream { get; set; }
    private StreamWriter fileWriter { get; set; }
    private StreamReader fileReader { get; set; }
    private object locker = new();
    private long? maxFileLen = null;
    public long? MaxFileLen
    {
        get {
            lock (locker)
            {
                return maxFileLen;
            }
        }
        set
        {
            lock (locker)
            {
                this.maxFileLen = value;
            }
        }
    }

    private const short BLOCK_SIZE = 0x200;
    public CSVFile(string path)
    {
        init(path);
    }

    private void init(string path)
    {
        this.fileInfo = new FileInfo(path);
        fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        fileStream.Seek(fileStream.Length, SeekOrigin.Begin);
        fileWriter = new StreamWriter(fileStream);
        fileReader = new StreamReader(fileStream);
    }

    public CSVFile(string path,long maxFileLen) : this(path)
    {
        this.maxFileLen = maxFileLen;
    }
    ~CSVFile()
    {
        this.Dispose();
    }
    public void append(AbstractKeyInfo key)
    {
        lock (locker)
        {
           fileInfo.Refresh();

            if (fileInfo.Length == 0)
                writeArray( key.getKeys().ToArray() );

            if (maxFileLen != null && maxFileLen != 0 && this.fileInfo.Length >= maxFileLen)
                resize();

            var values = key.getValues().ToArray();

            writeArray(values);
        }
    }

    private void writeArray(string[]? arr)
    {
        if (arr is null)
            return;

        lock (locker) 
        {
            for (int i = 0; i < arr.Count() - 1; ++i)
            {
                fileWriter.Write($"{arr[i]};");
            }
            fileWriter.Write($"{arr[arr.Length - 1]}\n");
            fileWriter.Flush();
            fileStream.Flush();
        }
    }
    private void resize()
    {
        string tempFilePath = fileInfo.FullName + "_copy";

        {
            using FileStream tempFile = File.Create(tempFilePath);
            using StreamWriter writer = new StreamWriter(tempFile);

            //Copy header to temp file
            long currentPos = fileStream.Position;
            fileStream.Seek(0, SeekOrigin.Begin);
            writer.WriteLine(fileReader.ReadLine());
            writer.Flush();
            fileStream.Seek(currentPos, SeekOrigin.Begin);

            //Calc new size and move to end line
            long newSize = this.fileInfo.Length / 2;
            fileStream.Seek(newSize, SeekOrigin.Begin);
            byte[] buff = new byte[2];
            while (fileStream.ReadByte() != 0xA) { }

            //Copy to temp file
            byte[] buffer = new byte[BLOCK_SIZE];
            int readed = 0;
            while ((readed = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var p = tempFile.Position;
                tempFile.Write(buffer, 0, readed);
            }
            tempFile.Flush();
        }

        delete();
        File.Copy(tempFilePath, fileInfo.FullName, true);
        init(fileInfo.FullName);
        delete(tempFilePath);
    }

    public void delete() {
        fileWriter.Flush();
        fileStream.Flush();
        this.Dispose();
        this.delete(fileInfo.FullName);
    }

    private void delete(string path)
    {
        lock (locker)
        {
            File.Delete(path);
        }
    }
    public void Dispose() {
        lock (locker)
        {
           fileWriter.Close();
           fileReader.Close();
           fileStream.Close();
        }
    }

}
