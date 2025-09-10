using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public class AbstractSetting : IDisposable, IComparable<AbstractSetting>
{
    protected string path = String.Empty;
    protected MemoryMappedFile mapping { get; set; }
    protected MemoryMappedViewStream stream { get; set; }
    protected FileStream file { get; set; }
    protected Dictionary<string, string> properties { get; } = new Dictionary<string, string>();
    protected const string sep = "=";
    public bool isCheckChangeFile { get; } = false;

    private FileSystemWatcher? fsw { get; set; } = null;
    public ImmutableDictionary<string, string> Properties
    {
        get => ImmutableDictionary.ToImmutableDictionary(properties);
    }
    public string Path
    {
        get { return path; }
        set
        {
            path = value;
            update();
        }
    }
    public AbstractSetting(in string path, bool isCheckChangeFile = false)
    {
        this.isCheckChangeFile = isCheckChangeFile;
        this.path = path;
        file = new FileStream(
            path,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);
        mapping = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);

        if (mapping == null)
            throw new ApplicationException("Settings not opened");

        stream = mapping.CreateViewStream();

        if (stream == null)
            throw new ApplicationException("Settings not opened");

        load();
        if (this.isCheckChangeFile)
        {
            fsw = new FileSystemWatcher(Directory.GetCurrentDirectory());
            fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess;
            fsw.Filter = this.Path;
            fsw.Changed += onFileChanged;
            fsw.EnableRaisingEvents = true;
        }
    }
    ~AbstractSetting()
    {
        Dispose();
    }

    private void onFileChanged(object sender, FileSystemEventArgs e)
    {
        update();
    }
    public virtual void load()
    {
        using StreamReader reader = new StreamReader(stream);
        string? line = null;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            string[]? propLine = line?.Split(sep);
            if (propLine == null)
                return;
            if (propLine.Length != 2)
                throw new ApplicationException($"{Path} read error");
            this.properties[propLine[0].Trim().ToLower()] = propLine[1].Trim();
        }
    }

    protected virtual void update()
    {
        mapping.Dispose();
        file = new FileStream(
            this.path,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);
        mapping = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
        stream = mapping.CreateViewStream();
        load();
        onChangeFile?.Invoke(this);
    }
    public void Dispose()
    {
        stream.Dispose();
        mapping.Dispose();
        file.Dispose();
    }

    public int CompareTo(AbstractSetting? other)
    {
        return this.path.CompareTo(other?.path);
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var prop in Properties)
        {
            sb.Append(prop.Key).Append(sep).Append(prop.Value).Append("\n");
        }
        return sb.ToString();
    }

    public delegate void ChangeSettingFilePath(AbstractSetting newSetting);
    public event ChangeSettingFilePath? onChangeFile;
}
