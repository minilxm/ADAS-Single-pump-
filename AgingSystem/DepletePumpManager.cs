using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AsyncSocket;
using Analyse;

namespace AgingSystem
{
    /// <summary>
    /// 提供耗尽后续操作，包括创建线程去执行上电操作
    /// </summary>
    public class DepletePumpManager
    {
        private List<DepletePumpList> m_DepletePumpQueue = new List<DepletePumpList>();
        
        public List<DepletePumpList> DepletePumpQueue
        {
            get { return m_DepletePumpQueue; }
        }

        public DepletePumpManager()
        { }

         
        /// <summary>
        /// 有耗尽信息，更新到表中
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="channel"></param>
        public void UpdateDepleteInfo(long ip, byte channel)
        {
            lock (m_DepletePumpQueue)
            {
                DepletePumpList pumpInfo = m_DepletePumpQueue.Find((x => { return x.ip == ip; }));
                if (pumpInfo != null)
                    pumpInfo.Update(ip, channel);
                else
                {
                    DepletePumpList pumps = new DepletePumpList(ip);
                    pumps.Update(ip, channel);
                    m_DepletePumpQueue.Add(pumps);
                }
            }
        }

        /// <summary>
        /// 清除所有泵
        /// </summary>
        public void Clear()
        {
            lock (m_DepletePumpQueue)
            {
                m_DepletePumpQueue.Clear();
            }
        }

        /// <summary>
        /// 移除一层泵
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="channel"></param>
        public void Remove(long ip)
        {
            lock (m_DepletePumpQueue)
            {
                m_DepletePumpQueue.RemoveAll((x => { return x.ip == ip;}));
            }
        }

        public DepletePumpList GetDepletePumpsByIP(long ip)
        {
            return m_DepletePumpQueue.Find((x) => { return x.ip == ip; });
        }
    }

    public class DepletePumpList
    {
        public long ip;
        public List<byte> channels = new List<byte>();

        public DepletePumpList()
        {
            ip = 0;
        }

        public DepletePumpList(long ip)
        {
            this.ip = ip;
        }

        public void Update(long ip, byte channel)
        {
            if (channels.Count==0)
                channels.Add(channel);
            else
            {
                if(!channels.Contains(channel))
                    channels.Add(channel);
            }
        }

        /// <summary>
        /// 同一个IP下面有多个同时耗尽的泵，可以一起发送补电命令,此时需要生成通道编号
        /// </summary>
        /// <returns></returns>
        public byte GenChannel()
        {
            byte channel = 0;
            if (channels.Count==0)
            {
                return 0;
            }
            else
            {
                for(int iLoop = 0;iLoop<channels.Count;iLoop++)
                {
                    channel |= (byte)(1 << (channels[iLoop]-1));
                }
            }
            return channel;
        }

    }
}
