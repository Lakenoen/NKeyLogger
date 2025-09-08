using System;
using System.Collections.Generic;
using System.Data;
using System.IO.MemoryMappedFiles;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public class ConstSetting : AbstractSetting
{
    public ConstSetting(in string path, bool isCheckFileChange = false) : base(path, isCheckFileChange)
    {

    }
}
