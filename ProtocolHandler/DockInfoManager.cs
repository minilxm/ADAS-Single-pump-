using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using AsyncSocket;

namespace Analyse
{
    public class DockInfo
    {
        private int   m_DockNo;
        private int   m_RowCount;
        private int   m_ColumnCount;

        /// <summary>
        /// 货架编号(1~N)
        /// </summary>
        public int DockNo
        {
            get { return m_DockNo; }
            set { m_DockNo = value; }
        }

        /// <summary>
        /// 货架的行数
        /// </summary>
        public int RowCount
        {
            get { return m_RowCount; }
            set { m_RowCount = value; }
        }

        /// <summary>
        /// 货架的列数
        /// </summary>
        public int ColumnCount
        {
            get { return m_ColumnCount; }
            set { m_ColumnCount = value; }
        }

        public DockInfo(int dockNo, int rowCount, int columnCount)
        {
           m_DockNo      = dockNo;    
           m_RowCount    = rowCount;
           m_ColumnCount = columnCount;
        }

    }

    public class DockInfoManager
    {
        private static DockInfoManager m_Manager = null;
        private Hashtable m_HashDock = null;            //存放格式<dockNO, 机位总数>

        private DockInfoManager()
        {
            m_HashDock = new Hashtable();
        }

        public static DockInfoManager Instance()
        {
            if (m_Manager == null)
                m_Manager = new DockInfoManager();
            return m_Manager;
        }

        public void Init()
        {
            string executePath = Assembly.GetEntryAssembly().Location;
            int index = executePath.LastIndexOf('\\');
            string path = executePath.Substring(0, index + 1) + "Config\\AgingRack.txt";
            LoadDockInfo(path);
        }

        public bool LoadDockInfo(string path)
        {
            if(!File.Exists(path))
            {
                Logger.Instance().ErrorFormat("货架信息文件不存在 path={0}", path);
                return false;
            }
            try
            {
                int[] outValue = new int[3];   
                string factorString = string.Empty;
                char[] separatorLine = new char[2] { (char)0x0D, (char)0x0A };
                char[] separator = new char[2] { '\t', ' ' };
                StreamReader reader = new StreamReader(path);
                if (reader != null)
                {
                    factorString = reader.ReadToEnd();
                    reader.Close();
                    string[] factors = factorString.Split(separatorLine, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in factors)
                    {
                        #region
                        string[] factor = s.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        if (factor.Length != 3)
                            continue;
                        if (int.TryParse(factor[0], out outValue[0]) && int.TryParse(factor[1], out outValue[1]) && int.TryParse(factor[2], out outValue[2]))
                        {
                            //如果转换成功，将货架编号和机位总数放在HASH表中
                            m_HashDock.Add(outValue[0], outValue[1] * outValue[2]);
                            continue;
                        }
                        #endregion
                    }
                }
                else
                {
                    Logger.Instance().Error("LoadDockInfo()读文件错误");
                }
            }
            catch(Exception e)
            {
                Logger.Instance().ErrorFormat("LoadDockInfo()读文件错误,Message={0}", e.Message);
            }
            return true;
        }

        public void Delete(int dockNo)
        {
            m_HashDock.Remove(dockNo);
        }

        public void Clear()
        {
            m_HashDock.Clear();
        }

        /// <summary>
        /// 通过dockNo查找相关对象
        /// </summary>
        /// <param name="dockNo"></param>
        /// <returns></returns>
        public int Get(int dockNo)
        {
            if (m_HashDock.ContainsKey(dockNo))
                return (int)m_HashDock[dockNo];
            else
                return 0;
        }

    }
}
