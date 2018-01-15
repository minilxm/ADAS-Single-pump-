using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Net.Sockets;
using Cmd;

namespace Analyse
{
    public class CommandQueueEx
    {
        private readonly string m_lock = string.Empty;
        private Hashtable m_SocketQueueHash = new Hashtable(); //每个SOCKET一个队列，这样就不用怕干扰了。<(long)ip, Queue<BaseCommand>>
        public int Count
        {
            get
            {
                int iCount = 0;
                Queue<BaseCommand> queue = null;
                lock(m_SocketQueueHash)
                {
                    foreach (DictionaryEntry entry in m_SocketQueueHash)
                    {
                        queue = entry.Value as Queue<BaseCommand>;
                        iCount += queue.Count;
                    }
                }
                return iCount;
            }
        }

        public CommandQueueEx()
        {
            if (m_SocketQueueHash == null)
                m_SocketQueueHash = new Hashtable();
        }

     
        /// <summary>
        /// 每个SOCKET一个队列，所以在进队列的时候要分清楚
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="item"></param>
        public void Enqueue(long ip, BaseCommand item)
        {
            lock (m_SocketQueueHash)
            {
                if (!m_SocketQueueHash.ContainsKey(ip))
                {
                    m_SocketQueueHash.Add(ip, new Queue<BaseCommand>());
                    Logger.Instance().DebugFormat("创建了一个新的发送队列，ip={0}", ip);
                }
                Queue<BaseCommand> queue = m_SocketQueueHash[ip] as Queue<BaseCommand>;
                queue.Enqueue(item);
            }
        }

        /// <summary>
        /// 每次依然是只取一个命令进行发送，因为发送速度很快不用担心延迟
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public BaseCommand Peek(ref long ip)
        {
            BaseCommand item = null;
            Queue<BaseCommand> queue = null;
            lock (m_SocketQueueHash)
            {
                foreach (DictionaryEntry entry in m_SocketQueueHash)
                {
                    queue = entry.Value as Queue<BaseCommand>;
                    if (queue!=null && queue.Count > 0)
                    {
                        ip = (long)entry.Key;
                        break;
                    }
                }
                if (queue != null && queue.Count>0)
                {
                    item = queue.Peek();
                }
                return item;
            }
        }

        /// <summary>
        /// 出栈还是一个个来，但是，要氢所在的SOCKET句柄传入
        /// </summary>
        /// <param name="item"></param>
        /// <param name="handle"></param>
        public void Dequeue(long ip, BaseCommand item)
        {
            lock (m_SocketQueueHash)
            {
                Queue<BaseCommand> queue = m_SocketQueueHash[ip] as Queue<BaseCommand>;
                if (queue != null && queue.Contains(item))
                {
                    queue.Dequeue();
                }
            }
        }

        /// <summary>
        /// 清理栈中的命令，但不清除栈本身
        /// </summary>
        public void ClearQueue()
        {
            lock (m_SocketQueueHash)
            {
                foreach (DictionaryEntry entry in m_SocketQueueHash)
                {
                    Queue<BaseCommand> queue = entry.Value as Queue<BaseCommand>;
                    if (queue != null)
                    {
                        queue.Clear();
                        Logger.Instance().DebugFormat("ClearQueue执行，清除了ip={0}的发送队列中的命令", (long)entry.Key);
                    }
                }
            }
        }

        /// <summary>
        /// 清除某个队列，当连接不存在时
        /// </summary>
        /// <param name="handle"></param>
        public void ClearQueue(long ip)
        {
            lock (m_SocketQueueHash)
            {
                if (m_SocketQueueHash.ContainsKey(ip))
                {
                    m_SocketQueueHash.Remove(ip);
                    Logger.Instance().DebugFormat("清除了一个发送队列，ip={0}", ip);
                }
            }
        }
        
        /// <summary>
        /// 清除所有栈内容，包括SOCKET
        /// </summary>
        public void ClearAll()
        {
            lock (m_SocketQueueHash)
            {
                m_SocketQueueHash.Clear();
                Logger.Instance().Debug("清除了所有发送队列");
            }
        }

        /// <summary>
        /// 如果某个SOCKET对应的队列不存在，就新建一个。否则忽略
        /// </summary>
        /// <param name="handle"></param>
        public void CreateQueue(long ip)
        {
            if (!m_SocketQueueHash.ContainsKey(ip))
            {
                m_SocketQueueHash.Add(ip, new Queue<BaseCommand>());
                Logger.Instance().DebugFormat("创建了一个新的发送队列，ip={0}", ip);
            }
        }


         
    }
}
