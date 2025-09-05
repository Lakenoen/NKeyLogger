using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public class KeyInfo(string key, string lang, string proc, string timestamp) : AbstractKeyInfo
{
    protected override Dictionary<string, string> values { get; set; } = new Dictionary<string, string>()
    {
        {"key",  key},
        {"language", lang },
        {"processName", proc },
        {"timestamp", timestamp }
    };

    public string Key {
        get => values["key"];
        set
        {
            values["key"] = value;
            base.OnUpdate("key", value);
        }
    }
    public string Language
    {
        get => values["language"];
        set
        {
            values["language"] = value;
            base.OnUpdate("language", value);
        }
    }
    public string ProcessName
    {
        get => values["processName"];
        set
        {
            values["processName"] = value;
            base.OnUpdate("processName", value);
        }
    }

    public KeyInfo() : this("", "", "", "") { }
    public KeyInfo(string key) : this(key, "", "", "") { }
    public KeyInfo(string key, string lang) : this(key, lang, "", "") { }

}

