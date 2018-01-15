using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using AsyncSocket;

namespace Cmd
{
    /// <summary>
    /// 控制器管理类，统一管理所有控制器信息，初始化时加载配置文件
    /// </summary>
    public class ControllerManager
    {
        private static ControllerManager m_Manager = null;
        private List<Controller> m_ControllerList = null;

        private ControllerManager()
        {
            m_ControllerList = new List<Controller>();
        }

        public static ControllerManager Instance()
        {
            if (m_Manager == null)
                m_Manager = new ControllerManager();
            return m_Manager;
        }

        /// <summary>
        /// 初始化所有在配置文件中定义的控制器，一次性加载到列表中
        /// </summary>
        public void Init()
        {
            string executePath = Assembly.GetEntryAssembly().Location;
            int index = executePath.LastIndexOf('\\');
            string path = executePath.Substring(0, index + 1) + "Config\\IP.txt";
            LoadController(path);
        }

        /// <summary>
        /// 读配置文件，静态IP与货架映射表
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool LoadController(string path)
        {
            if (!File.Exists(path))
            {
                Logger.Instance().ErrorFormat("控制器IP配置文件不存在 path={0}", path);
                return false;
            }
            try
            {
                int[] outValue = new int[2];
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
                        if (int.TryParse(factor[1], out outValue[0]) && int.TryParse(factor[2], out outValue[1]))
                        {
                            //如果转换成功，就新建一个Controller对象
                            m_ControllerList.Add(new Controller(IP2Long(factor[0]), outValue[0], outValue[1]));
                            continue;
                        }
                        #endregion
                    }
                }
                else
                {
                    Logger.Instance().Error("LoadController()读文件错误");
                }
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("LoadController()读文件错误,Message={0}", e.Message);
            }
            return true;
        }

        /// <summary>
        /// 通过IP地址可以删除一个控制器信息
        /// </summary>
        /// <param name="ip"></param>
        public void Delete(long ip)
        {
            int index = m_ControllerList.FindIndex((x) => { return x.IP == ip; });
            if (index >= 0)
                m_ControllerList.RemoveAt(index);
        }

        /// <summary>
        /// 清除所有控制器
        /// </summary>
        public void Clear()
        {
            m_ControllerList.Clear();
        }

        /// <summary>
        /// 通过IP可以查找一个控制器
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public Controller Get(long ip)
        {
            return m_ControllerList.Find((x) => { return x.IP == ip; });
        }

        /// <summary>
        /// 通过SOCKET也可以查找一个控制器
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public Controller Get(Socket socket)
        {
            Controller controller = null;
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                    return null;
                IPEndPoint point = socket.RemoteEndPoint as IPEndPoint;
                byte[] ipByte = point.Address.GetAddressBytes();
                long ip = Bytes2Long(ipByte);
                controller = Get(ip);
            }
            catch(Exception e)
            {
                Logger.Instance().ErrorFormat("ControllerManager::Get() Error Message={0}", e.Message);
            }
            return controller;
        }

        /// <summary>
        /// 通过SOCKET也可以查找一个控制器
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public Controller Get(AsyncSocketUserToken usertoken)
        {
            Controller controller = null;
            try
            {
                if (usertoken == null)
                    return null;
                Socket socket = usertoken.ConnectSocket;
                if (socket.RemoteEndPoint == null)
                    return null;
                IPEndPoint point = socket.RemoteEndPoint as IPEndPoint;
                byte[] ipByte = point.Address.GetAddressBytes();
                long ip = Bytes2Long(ipByte);
                controller = Get(ip);
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("ControllerManager::Get() Error Message={0}", e.Message);
            }
            return controller;
        }

        /// <summary>
        /// 查找某个货架上的所有控制器
        /// </summary>
        /// <param name="dockNo"></param>
        /// <returns></returns>
        public List<Controller> Get(int dockNo)
        {
            List<Controller> controllers = new List<Controller>();
            for (int i = 0; i < m_ControllerList.Count; i++)
            {
                if (m_ControllerList[i].DockNo == dockNo)
                    controllers.Add(m_ControllerList[i]);
            }
            return controllers;
        }

        /// <summary>
        /// 将"192.168.1.2"的IP转换成long
        /// 192在低8位，168在中8位， 1 高8位，2在最高8位
        /// </summary>
        /// <param name="szIP"></param>
        /// <returns></returns>
        public static long IP2Long(string szIP)
        {
            if (string.IsNullOrEmpty(szIP))
            {
                Logger.Instance().Error("IP地址转换错误,IP为空");
                return -1;
            }
            string[] arrIP = szIP.Split('.');
            if (arrIP.Length != 4)
            {
                Logger.Instance().ErrorFormat("IP地址转换错误,IP={0}", szIP);
                return -2;
            }
            byte ip1 = Convert.ToByte(arrIP[0]);
            byte ip2 = Convert.ToByte(arrIP[1]);
            byte ip3 = Convert.ToByte(arrIP[2]);
            byte ip4 = Convert.ToByte(arrIP[3]);

            long ip = ip1 & 0x00000000000000FF;
            ip += (ip2 << 8) & 0x000000000000FF00;
            ip += (ip3 << 16) & 0x0000000000FF0000;
            ip += (ip4 << 24) & 0x00000000FF000000;
            return ip;
        }

        /// <summary>
        /// 4字节数组转成IP
        /// </summary>
        /// <param name="arrIP"></param>
        /// <returns></returns>
        public static long Bytes2Long(byte[] arrIP)
        {
            if (arrIP.Length != 4)
            {
                Logger.Instance().Error("Bytes2Long()->IP地址转换错误");
                return -2;
            }
            byte ip1 = arrIP[0];
            byte ip2 = arrIP[1];
            byte ip3 = arrIP[2];
            byte ip4 = arrIP[3];

            long ip = ip1 & 0x00000000000000FF;
            ip += (ip2 << 8) & 0x000000000000FF00;
            ip += (ip3 << 16) & 0x0000000000FF0000;
            ip += (ip4 << 24) & 0x00000000FF000000;
            return ip;
        }

        /// <summary>
        /// 将long的IP转换成"192.168.1.1"
        /// </summary>
        /// <param name="szIP"></param>
        /// <returns></returns>
        public static string Long2IP(long ipAddr = 0)
        {
            long ip = ipAddr;
            if (ip <= 0)
            {
                Logger.Instance().Error("IP地址转换错误,IP为空");
                return string.Empty;
            }
            byte ip1 = (byte)(ip & 0x000000FF);
            byte ip2 = (byte)(ip >> 8 & 0x000000FF);
            byte ip3 = (byte)(ip >> 16 & 0x000000FF);
            byte ip4 = (byte)(ip >> 24 & 0x000000FF);
            string szIP = string.Format("{0}.{1}.{2}.{3}", ip1, ip2, ip3, ip4);
            return szIP;
        }

        /// <summary>
        /// 将一个SOCKET的IP地址转换成long
        /// </summary>
        /// <param name="sock"></param>
        /// <returns></returns>
        public static long GetLongIPFromSocket(AsyncSocketUserToken sockToken)
        {
            if (sockToken != null)
            {
                Socket sock = sockToken.ConnectSocket;
                if (sock != null && sock.RemoteEndPoint != null)
                {
                    IPEndPoint endpoint = sock.RemoteEndPoint as IPEndPoint;
                    byte[] addrBytes = endpoint.Address.GetAddressBytes();
                    if (addrBytes.Length != 4)
                    {
                        Logger.Instance().Debug("GetLongIPFromSocket()->endpoint.GetAddressBytes() Error");
                        return 0;
                    }
                    long ip = addrBytes[0] & 0xFF;
                    ip += addrBytes[1] << 8 & 0x00FF00;
                    ip += addrBytes[2] << 16 & 0x00FF0000;
                    ip += addrBytes[3] << 24 & 0x00FF000000;
                    return ip;
                }
                else
                    return 0;
            }
            return 0;
        }
    }

}
