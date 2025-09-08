using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public class Setting : AbstractSetting
{
    public Setting(in string path, bool isCheckFileChange = false) : base(path, isCheckFileChange)
    {

    }

    public void insert(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            return;
        base.properties[key] = value;
    }
}
