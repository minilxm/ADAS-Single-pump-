using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cmd;

namespace Analyse
{
    public class CommandQueue
    {
        private const int QUEUESIZE = 3;
        private Queue<BaseCommand>[] m_QueueArray = new Queue<BaseCommand>[QUEUESIZE];
        private readonly string m_lock = string.Empty;

        public int Count
        {
            get
            {
                int iCount = 0;
                for (int i = 0; i < QUEUESIZE; i++)
                {
                    iCount += m_QueueArray[i].Count;
                }
                return iCount;
            }
        }

        public CommandQueue()
        {
            for (int i = 0; i < QUEUESIZE; i++)
            {
                m_QueueArray[i] = new Queue<BaseCommand>();
            }
        }

        /// <summary>
        /// 3 priority 3 Queue
        /// </summary>
        /// <param name="item"></param>
        /// <param name="priority"></param>
        public void Enqueue(BaseCommand item, CommandPriority priority = CommandPriority.Normal)
        {
            lock (m_lock)
            {
                switch (priority)
                {
                    case CommandPriority.High:
                        m_QueueArray[0].Enqueue(item);
                        break;
                    case CommandPriority.AboveNormal:
                        m_QueueArray[1].Enqueue(item);
                        break;
                    case CommandPriority.Normal:
                        m_QueueArray[2].Enqueue(item);
                        break;
                    default:
                        m_QueueArray[2].Enqueue(item);
                        break;
                }
            }
        }

        public BaseCommand Peek(ref int QueueArrayIndex)
        {
            lock (m_lock)
            {
                BaseCommand item = null;
                for (int i = 0; i < QUEUESIZE; i++)
                {
                    if (m_QueueArray[i].Count > 0)
                    {
                        item = m_QueueArray[i].Peek();
                        QueueArrayIndex = i;
                        break;
                    }
                }
                return item;
            }
        }

        public void Dequeue(BaseCommand item, int QueueArrayIndex)
        {
            lock (m_lock)
            {
                if (QueueArrayIndex < 0 || QueueArrayIndex >= QUEUESIZE)
                    return;
                if (m_QueueArray[QueueArrayIndex].Contains(item))
                    m_QueueArray[QueueArrayIndex].Dequeue();
            }
        }

        public void Clear()
        {
            lock (m_lock)
            {
                for (int i = 0; i < QUEUESIZE; i++)
                {
                    m_QueueArray[i].Clear();
                }
            }
        }
    }
}
