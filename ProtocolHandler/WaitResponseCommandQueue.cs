using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cmd;

namespace Analyse
{
    /// <summary>
    /// 等待响应队列，超时报警
    /// </summary>
    public class WaitResponseCommandQueue
    {
        private Queue<BaseCommand> m_Queue = new Queue<BaseCommand>();
        private readonly string m_lock = string.Empty;

        public int Count
        {
            get
            {
                return m_Queue.Count;
            }
        }

        public WaitResponseCommandQueue()
        {
        }

        /// <summary>
        /// 5 priority 5 Queue
        /// </summary>
        /// <param name="item"></param>
        /// <param name="priority"></param>
        public void Enqueue(BaseCommand item)
        {
            lock (m_lock)
            {
                item.TimeStamp = DateTime.Now.Ticks;    //时间戳（100毫微秒）精度不是很高，但能满足需要
                m_Queue.Enqueue(item);
            }
        }

        public BaseCommand Peek()
        {
            lock (m_lock)
            {
                BaseCommand item = null;
                if (m_Queue.Count > 0)
                {
                    item = m_Queue.Peek();
                }
                return item;
            }
        }

        public void Dequeue(BaseCommand item)
        {
            lock (m_lock)
            {
                if (m_Queue.Contains(item))
                    m_Queue.Dequeue();
            }
        }

        public void Clear()
        {
            lock (m_lock)
            {
                m_Queue.Clear();
            }
        }
    }
}
