using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace NKeyLoggerServer;
internal class CSVFile
{
    public FileInfo fileInfo { get; init; }
    private object locker = new();
    private long? maxFileLen = null;
    public long? MaxFileLen
    {
        get => maxFileLen;
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
        if(!File.Exists(path))
            File.Create(path).Close();
        this.fileInfo = new FileInfo(path);
    }

    public CSVFile(string path,long maxFileLen) : this(path)
    {
        this.maxFileLen = maxFileLen;
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
        using var stream = fileInfo.AppendText();
        lock (stream)
        {
            for (int i = 0; i < arr.Count() - 1; ++i)
            {
                stream.Write($"{arr[i]};");
            }
            stream.Write($"{arr[arr.Length - 1]}\n");
        }
    }
    private void resize()
    {
        string tempFilePath = fileInfo.FullName + "_copy";
        using (FileStream file = fileInfo.OpenRead())
        {
            using FileStream tempFile = File.Create(tempFilePath);
            using StreamWriter writer = new StreamWriter(tempFile);
            using StreamReader reader = new StreamReader(file);

            //Copy header to temp file
            writer.WriteLine(reader.ReadLine());
            writer.Flush();

            //Calc new size and move to end line
            long newSize = this.fileInfo.Length / 2;
            file.Seek(newSize, SeekOrigin.Begin);
            byte[] buff = new byte [2];
            while (file.ReadByte() != 0xA) { }

            //Copy to temp file
            byte[] buffer = new byte[BLOCK_SIZE];
            int readed = 0;
            while ( (readed = file.Read(buffer, 0, buffer.Length)) > 0)
            {
                var p = tempFile.Position;
                tempFile.Write(buffer, 0, readed);
            }
            tempFile.Flush();
        }
        delete();
        File.Copy(tempFilePath, fileInfo.FullName, true);
        delete(tempFilePath);
    }

    public void delete() {
        this.delete(fileInfo.FullName);
    }

    private void delete(string path)
    {
        lock (locker)
        {
            File.Delete(path);
        }
    }
}
