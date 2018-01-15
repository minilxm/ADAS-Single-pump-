using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cmd;
using System.Net;
using System.Net.Sockets;
using AsyncSocket;

namespace Analyse
{
    public class CommandManage
    {
        private void AddCommand(long ip, BaseCommand cmd, EventHandler<EventArgs> func)
        {
            cmd.HandleResponse += func;
            ProtocolEngine.Instance().AddCommand(ip, cmd);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="cmd"></param>
        /// <param name="func">命令返回成功处理函数</param>
        /// <param name="TimeoutFunc">命令超时处理函数</param>＝
        private void AddCommand(long ip, BaseCommand cmd, EventHandler<EventArgs> func, EventHandler<EventArgs> timeoutFunc)
        {
            cmd.HandleResponse += func;
            cmd.HandleTimeOut += timeoutFunc;
            ProtocolEngine.Instance().AddCommand(ip, cmd);
        }

        #region 泵类型命令
        /// <summary>
        /// 添加通道号的原因是由于控制器轮询一次6个串口很费时间，不如指定具体的串口
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="queryInterval"></param>
        /// <param name="remoteSocket"></param>
        /// <param name="func"></param>
        /// <param name="channel"></param>
        public void SendPumpType(ProductID pid, ushort queryInterval, AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, byte channel = 0xFF)
        {
            CmdSendPumpType cmd = new CmdSendPumpType(pid, queryInterval);
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func);
        }

        /// <summary>
        /// 添加通道号的原因是由于控制器轮询一次6个串口很费时间，不如指定具体的串口
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="queryInterval"></param>
        /// <param name="remoteSocket"></param>
        /// <param name="func"></param>
        /// <param name="timeoutFunc"></param>
        public void SendPumpType(ProductID pid, ushort queryInterval, AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, EventHandler<EventArgs> timeoutFunc, byte channel = 0xFF)
        {
            CmdSendPumpType cmd = new CmdSendPumpType(pid, queryInterval);
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func, timeoutFunc);
        }
       
        #endregion

        #region 充电命令

        /// <summary>
        /// 添加通道号的原因是由于控制器轮询一次6个串口很费时间，不如指定具体的串口
        /// </summary>
        /// <param name="rate"></param>
        /// <param name="volume"></param>
        /// <param name="remoteSocket"></param>
        /// <param name="func"></param>
        /// <param name="timeoutFunc"></param>
        /// <param name="channel"></param>
        public void SendCmdCharge(decimal rate, decimal volume, AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, EventHandler<EventArgs> timeoutFunc, byte channel = 0xFF)
        {
            CmdCharge cmd = new CmdCharge();
            cmd.Channel = channel;
            cmd.SetRate(rate);
            cmd.SetVolume(volume);
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func, timeoutFunc);
        }

        #endregion

        #region 放电命令

        /// <summary>
        /// 发送放电命令
        /// </summary>
        /// <param name="handle">回调函数对象</param>
        public void SendCmdDischarge(AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, EventHandler<EventArgs> timeoutFunc, byte channel = 0xFF)
        {
            if (remoteSocket == null)
            {
                Logger.Instance().ErrorFormat("CommandManage::SendCmdDischarge()->参数remoteSocket为null");
                return;
            }
            CmdDischarge cmd = new CmdDischarge();
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func, timeoutFunc);
        }
        #endregion

        #region 补电命令
 
        /// <summary>
        /// 发送补电命令
        /// </summary>
        /// <param name="channel">通道号，按位表示例如：2号通道=0x02,3号通道=0x04,4号=0x08</param>
        /// <param name="remoteSocket"></param>
        /// <param name="func"></param>
        public void SendCmdRecharge(byte channel, AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func)
        {
            CmdRecharge cmd = new CmdRecharge();
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func);
        }

        /// <summary>
        /// 发送补电命令
        /// </summary>
        /// <param name="handle">回调函数对象</param>
        public void SendCmdRecharge(byte channel, AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, EventHandler<EventArgs> timeoutFunc)
        {
            CmdRecharge cmd = new CmdRecharge();
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func, timeoutFunc);
        }

        #endregion

        #region 老化结束命令

        /// <summary>
        /// 发送老化结束命令
        /// </summary>
        /// <param name="handle">回调函数对象</param>
        public void SendCmdFinishAging(AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, byte channel = 0xFF)
        {
            CmdFinishAging cmd = new CmdFinishAging();
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func);
        }

        /// <summary>
        /// 发送老化结束命令
        /// </summary>
        /// <param name="handle">回调函数对象</param>
        public void SendCmdFinishAging(AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, EventHandler<EventArgs> timeoutFunc, byte channel = 0xFF)
        {
            CmdFinishAging cmd = new CmdFinishAging();
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func, timeoutFunc);
        }
        #endregion

        #region 发送读泵状态命令

        /// <summary>
        /// 发送读泵状态命令
        /// </summary>
        /// <param name="handle">回调函数对象</param>
        public void SendCmdGetPumpStatus(AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, byte channel = 0xFF)
        {
            CmdGetPumpStatus cmd = new CmdGetPumpStatus();
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func);
        }

        /// <summary>
        /// 发送读泵状态命令
        /// </summary>
        /// <param name="handle">回调函数对象</param>
        public void SendCmdGetPumpStatus(AsyncSocketUserToken remoteSocket, EventHandler<EventArgs> func, EventHandler<EventArgs> timeoutFunc, byte channel = 0xFF)
        {
            CmdGetPumpStatus cmd = new CmdGetPumpStatus();
            cmd.Channel = channel;
            cmd.RemoteSocket = remoteSocket;
            long ip = ControllerManager.GetLongIPFromSocket(remoteSocket);
            AddCommand(ip, cmd, func, timeoutFunc);
        }
        #endregion

    }
}
