using System;
using System.Collections.Generic;
using System.Net;
using AsyncSocket;

namespace Trans
{
    /// <summary>
    /// Event arguments containing event data.
    /// </summary>
    public class DataTransmissionEventArgs : EventArgs
    {
        protected List<byte> data = null;

        public List<byte> EventData
        {
            get { return this.data; }
            set { this.data = value; }
        }

        protected AsyncSocketUserToken m_Token = null;

        public AsyncSocketUserToken Token
        {
            get { return m_Token; }
            set { m_Token = value; }
        }

        /// <summary>
        ///Copies the data starting at the specified index and paste them to the inner array
        /// </summary>
        /// <param name="result">Data raised in the event.</param>
        /// <param name="index">the index in the sourceArray at which copying begins.</param>
        /// <param name="length"> the number of elements to copy</param>
        public DataTransmissionEventArgs(byte[] result, int index, int length, AsyncSocketUserToken token=null)
        {
            if(data==null)
                data = new List<byte>();
            else
                data.Clear();
            for(int iLoop=index;iLoop<index+length;iLoop++)
            {
                data.Add(result[iLoop]);
            }
            m_Token = token;
        }

        /// <summary>
        /// Copies the data to the inner array 
        /// </summary>
        /// <param name="result">Data raised in the event.</param>
        public DataTransmissionEventArgs(byte[] result, AsyncSocketUserToken token=null)
        {
            if (data == null)
                data = new List<byte>();
            else
                data.Clear();
            data.AddRange(result);
            m_Token = token;
        }

        /// <summary>
        /// Copies the data to the inner array 
        /// </summary>
        /// <param name="result">Data raised in the event.</param>
        public DataTransmissionEventArgs(AsyncSocketUserToken token)
        {
            m_Token = token;
        }


    }

    public class ClientInfoArgs : EventArgs
    {
        protected List<IPEndPoint> m_ClientPoint;
        public List<IPEndPoint> ClientPoint
        {
            get { return m_ClientPoint; }
            set { m_ClientPoint = value; }
        }

        public ClientInfoArgs()
        {
            m_ClientPoint = new List<IPEndPoint>();
        }

        public ClientInfoArgs(List<IPEndPoint> clientPoint)
        {
            m_ClientPoint = clientPoint;
        }
    }
}
