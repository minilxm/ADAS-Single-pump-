using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using Cmd;

namespace Analyse
{
    /// <summary>
    /// 等待响应队列，超时报警
    /// </summary>
    public class WaitResponseCommandQueueEx
    {
        private readonly string m_lock = string.Empty;
        private Hashtable m_WaitSocketQueueHash = new Hashtable(); //每个SOCKET一个队列，这样就不用怕干扰了。<Handle, Queue<BaseCommand>


        public WaitResponseCommandQueueEx()
        {
            if (m_WaitSocketQueueHash == null)
                m_WaitSocketQueueHash = new Hashtable();
        }

        /// <summary>
        /// 5 priority 5 Queue
        /// </summary>
        /// <param name="item"></param>
        /// <param name="priority"></param>
        public void Enqueue(long ip, BaseCommand item)
        {
            item.TimeStamp = DateTime.Now.Ticks;    //时间戳（100毫微秒）精度不是很高，但能满足需要
            lock (m_WaitSocketQueueHash)
            {
                Queue<BaseCommand> queue = m_WaitSocketQueueHash[ip] as Queue<BaseCommand>;
                if (queue!=null)
                    queue.Enqueue(item);
            }
        }

        public BaseCommand Peek(long ip)
        {
            BaseCommand item = null;
            lock (m_WaitSocketQueueHash)
            {
                Queue<BaseCommand> queue = m_WaitSocketQueueHash[ip] as Queue<BaseCommand>;
                if (queue != null && queue.Count>0)
                    item = queue.Peek();
                return item;
            }
        }

        /// <summary>
        /// 每个等待队列都要Peek出来一个对象
        /// </summary>
        /// <returns></returns>
        public List<Tuple<long,BaseCommand>> Peek()
        {
            List<Tuple<long, BaseCommand>> items = new List<Tuple<long, BaseCommand>>();
            lock (m_WaitSocketQueueHash)
            {
                foreach (DictionaryEntry entry in m_WaitSocketQueueHash)
                {
                    Queue<BaseCommand> queue = entry.Value as Queue<BaseCommand>;
                    if (queue != null && queue.Count>0)
                    {
                        items.Add(new Tuple<long, BaseCommand>((long)entry.Key, queue.Peek()));
                    }
                }
            }
            return items;
        }

        public void Dequeue(long ip, BaseCommand item)
        {
            lock (m_WaitSocketQueueHash)
            {
                Queue<BaseCommand> queue = m_WaitSocketQueueHash[ip] as Queue<BaseCommand>;
                if (queue != null && queue.Contains(item))
                    queue.Dequeue();
            }
        }

        /// <summary>
        /// 清理栈中的命令，但不清除栈本身
        /// </summary>
        public void ClearQueue()
        {
            lock (m_WaitSocketQueueHash)
            {
                foreach (DictionaryEntry entry in m_WaitSocketQueueHash)
                {
                    Queue<BaseCommand> queue = entry.Value as Queue<BaseCommand>;
                    if (queue != null)
                    {
                        queue.Clear();
                        Logger.Instance().DebugFormat("ClearQueue执行，清除了ip={0}的等待队列中的命令", (long)entry.Key);
                    }
                }
            }
        }

        public void ClearQueue(long ip)
        {
            lock (m_WaitSocketQueueHash)
            {
                if (m_WaitSocketQueueHash.ContainsKey(ip))
                {
                    m_WaitSocketQueueHash.Remove(ip);
                    Logger.Instance().DebugFormat("清除了一个等待队列，ip={0}", ip);
                }
            }
        }

        public void ClearAll()
        {
            lock (m_WaitSocketQueueHash)
            {
                m_WaitSocketQueueHash.Clear();
                Logger.Instance().Debug("清除了所有等待队列");
            }
        }

        /// <summary>
        /// 如果某个SOCKET对应的队列不存在，就新建一个。否则忽略
        /// </summary>
        /// <param name="handle"></param>
        public void CreateQueue(long ip)
        {
            lock (m_WaitSocketQueueHash)
            {
                if (!m_WaitSocketQueueHash.ContainsKey(ip))
                {
                    m_WaitSocketQueueHash.Add(ip, new Queue<BaseCommand>());
                    Logger.Instance().DebugFormat("创建了一个新的等待队列，ip={0}", ip);
                }
             }
        }

    }
}
