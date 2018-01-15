using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trans
{
    public abstract class ConnectionDevice
    {
        public abstract void Open();
        public abstract void Close();
        public abstract bool IsOpen();
        public abstract void SendData(byte[] data);
    }
}
