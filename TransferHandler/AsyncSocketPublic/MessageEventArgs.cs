using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSocket
{
    public class MessageEventArgs : EventArgs
    {
        private byte[] buffer;
        public byte[]Buffer
        {
            set { buffer = value; }
            get { return buffer; }
        }

        public void FillBuffer(byte[] sourceBuf, int index, int count)
        {
            if (count > 0 && count > index)
            {
                buffer = new byte[count];
                Array.Copy(sourceBuf, index, buffer, 0, count);
            }
        }

        public MessageEventArgs(byte[] sourceBuf, int index, int count)
        {
            if (count > 0 && count > index)
            {
                buffer = new byte[count];
                Array.Copy(sourceBuf, index, buffer, 0, count);
            }
        }


    }

    public class MessageEventArgsEx : EventArgs
    {
        private AsyncClient m_Client = null;
        private byte[] buffer;
        public byte[] Buffer
        {
            set { buffer = value; }
            get { return buffer; }
        }

        public AsyncClient Client
        {
            set { m_Client = value; }
            get { return m_Client; }

        }
        public void FillBuffer(byte[] sourceBuf, int index, int count)
        {
            if (count > 0 && count > index)
            {
                buffer = new byte[count];
                Array.Copy(sourceBuf, index, buffer, 0, count);
            }
        }

        public MessageEventArgsEx(byte[] sourceBuf, int index, int count, AsyncClient client = null)
        {
            m_Client = client;
            if (count > 0 && count > index)
            {
                buffer = new byte[count];
                Array.Copy(sourceBuf, index, buffer, 0, count);
            }
        }


    }
}
