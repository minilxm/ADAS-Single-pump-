using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Analyse
{
    public enum CommandPriority : byte
    {
        High = 0,
        AboveNormal = 1,
        Normal = 2,
    }

    public enum DeviceType : byte
    {
        Unknown = 0,
        SerialPort,
        UDP,
        TCP,
    }


}
