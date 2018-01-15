using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;

namespace AsyncSocket
{
    public class AsyncSocketUserTokenPool
    {
        private Stack<AsyncSocketUserToken> m_pool;

        public AsyncSocketUserTokenPool(int capacity)
        {
            m_pool = new Stack<AsyncSocketUserToken>(capacity);
        }

        public void Push(AsyncSocketUserToken item)
        {
            if (item == null)
            {
                throw new ArgumentException("Items added to a AsyncSocketUserToken cannot be null");
            }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        public void Clear()
        {
            if(m_pool!=null)
                m_pool.Clear();
        }

        public AsyncSocketUserToken Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        public int Count
        {
            get { return m_pool.Count; }
        }
    }

    public class AsyncSocketUserTokenList : Object
    {
        private List<AsyncSocketUserToken> m_list;

        public int Count
        {
            get 
            { 
                if(m_list!=null)
                    return m_list.Count;
                else
                    return 0;
            }
        }

        public AsyncSocketUserTokenList()
        {
            m_list = new List<AsyncSocketUserToken>();
        } 

        public void Add(AsyncSocketUserToken userToken)
        {
            lock(m_list)
            {
                m_list.Add(userToken);
            }
        }

        public void Remove(AsyncSocketUserToken userToken)
        {
            lock (m_list)
            {
                m_list.Remove(userToken);
            }
        }

        /// <summary>
        /// 查询某个端点的索引
        /// </summary>
        /// <param name="point"></param>
        public int Find(IPEndPoint point)
        {
            lock (m_list)
            {
                int index = m_list.FindIndex((x) =>
                {
                    IPEndPoint p = ((IPEndPoint)(x.ConnectSocket.RemoteEndPoint));
                    return p.Port == point.Port && p.Address.Equals(point.Address);
                });
                return index;
            }
        }

        /// <summary>
        /// 移除除某个客户端之外所有的数据
        /// </summary>
        /// <param name="point"></param>
        public void RemoveAllWithout(IPEndPoint point)
        {
            lock (m_list)
            {
                m_list.RemoveAll((x) =>
                {
                    IPEndPoint p = ((IPEndPoint)(x.ConnectSocket.RemoteEndPoint));
                    return !(p.Port == point.Port && p.Address.Equals(point.Address));
                });
            }
        }

        /// <summary>
        /// 移除 所有的数据
        /// </summary>
        /// <param name="point"></param>
        public void RemoveAll()
        {
            lock (m_list)
            {
                m_list.Clear();
            }
        }

        public void CopyList(ref AsyncSocketUserToken[] array)
        {
            lock (m_list)
            {
                array = new AsyncSocketUserToken[m_list.Count];
                m_list.CopyTo(array);
            }
        }


    }
}
