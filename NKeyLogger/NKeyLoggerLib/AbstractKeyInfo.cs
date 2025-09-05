using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public abstract class AbstractKeyInfo : IComparable<AbstractKeyInfo>
{
    protected const short unicodeCharSize = 0x2;
    protected abstract Dictionary<string,string> values { get; set; }
    public IEnumerable<string> getValues()
    {
        return values.Values;
    }

    public IEnumerable<string> getKeys()
    {
        return values.Keys;
    }

    public delegate void updateDelegate(AbstractKeyInfo sender, string key, string value);
    public event updateDelegate? updateEvent;
    protected virtual void OnUpdate(string key, string value)
    {
        updateEvent?.Invoke(this, key, value);
    }

    public byte[] GetBytes()
    {
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(BitConverter.GetBytes((short)values.Count));

        foreach (KeyValuePair<string,string> kvp in values)
        {
            writer.Write(BitConverter.GetBytes(kvp.Value.Length * unicodeCharSize));
        }
        foreach (KeyValuePair<string, string> kvp in values)
        {
            writer.Write(Encoding.Unicode.GetBytes(kvp.Value));
        }
        writer.Flush();
        return stream.ToArray();
    }

    public void FromBytes(in byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(stream);
        short count = BitConverter.ToInt16( reader.ReadBytes(sizeof(short)) );
        int[] lengths = new int[count];
        for(int i = 0; i < count; i++)
        {
            lengths[i] = BitConverter.ToInt32( reader.ReadBytes(sizeof(int)) );
        }
        for (int i = 0; i < count; i++)
        {
            byte[] bytes = reader.ReadBytes(lengths[i]);
            string val = Encoding.Unicode.GetString(bytes);
            values[values.ElementAt(i).Key] = val;
        }
    }

    public int CompareTo(AbstractKeyInfo? obj)
    {
        if (obj == null) return 1;
        if (this.values.Count != obj.values.Count)
            throw new ArgumentException("The number of values ​​should be equal");

        int res = 0;
        foreach( KeyValuePair<string,string> kvp in this.values)
        {
            if ((res = kvp.Value.CompareTo(obj.values[kvp.Key])) != 0)
                return res;
        }
        return res;
    }

}
