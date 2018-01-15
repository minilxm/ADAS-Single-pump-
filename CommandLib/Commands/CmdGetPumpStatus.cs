using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    public class CmdGetPumpStatus : BaseCommand
    {
        private List<PumpStatusInfo> m_PumpStatusList = new List<PumpStatusInfo>();

        public List<PumpStatusInfo> PumpStatusList
        {
            get { return m_PumpStatusList; }
        }


        /// <summary>
        /// 对于ADAS来说，此命令是发送命令。
        /// </summary>
        public CmdGetPumpStatus()
            : base(0x00, 0x08)
        { }

        /// <summary>
        /// 将要发送的命令变成字节数组,本命令只是回应，不能主动发送
        /// </summary>
        /// <returns></returns>
        public override List<byte> GetBytes()
        {
            //先计算Payload长度
            UpdatePayloadLength(0);
            //命令头部（payload length（含）之前）
            List<byte> basebuffer = base.GetBytes();
            //取checksum字节
            uint checksum = CRC32.CalcCRC32Partial(basebuffer, basebuffer.Count, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
            byte[] arrChecksum = StructConverter.StructureToByte<uint>(checksum);
            basebuffer.AddRange(arrChecksum);
            return basebuffer;
        }

        public override void SetBytes(byte[] payloadData)
        {
            if (payloadData.Length == 0)
            {
                Logger.Instance().Error("报警信息数据包有误,数据包长度为0！");
                return;
            }
            byte packageSize = this.Channel;
            byte payloadLength = this.m_PayloadLength;
            if (payloadLength % packageSize != 0)
            {
                Logger.Instance().ErrorFormat("报警信息数据包有误不是单包的整数倍，单包大小={0},包总大小={1}", packageSize, payloadLength);
                return;
            }
            int count = payloadLength / packageSize;      //必须是整数倍，否则一定是错的
            byte[] temp = new byte[packageSize];
            for (int iLoop = 0; iLoop < count; iLoop++)
            {
                Array.Clear(temp, 0, packageSize);
                Array.Copy(payloadData, iLoop * packageSize, temp, 0, packageSize);
                m_PumpStatusList.Add(new PumpStatusInfo(temp));
            }
        }

        /// <summary>
        /// 复制命令
        /// </summary>
        /// <param name="other"></param>
        public override void Copy(BaseCommand other)
        {
            base.Copy(other);
        }

        public override void InvokeResponse()
        {
            base.InvokeResponse();
        }
    }

    public class PumpStatusInfo
    {
        private byte m_Chanel = 0;
        private byte[] m_Head = new byte[] { 0x55, 0xAA };
        private byte m_Length = 0x01;
        private ProductID m_PID;
        private byte m_Pump2PC = 0x00;
        private byte m_MessageID = 0x4B;
        private byte m_AppDC = 0x01;
        private PumpStatus m_PumpStatus = PumpStatus.Off;
        private byte m_CheckCode;


        public PumpStatus PumpState
        {
            get { return m_PumpStatus; }
            set { m_PumpStatus = value; }
        }

        public PumpStatusInfo() { }

        public PumpStatusInfo(byte[] data)
        {
            if (data.Length != 10)
            {
                Logger.Instance().Error("PumpStatusInfo实例化失败，没有10个字节！");
            }
            else
            {
                m_Chanel = data[0];
                m_Head[0] = data[1];
                m_Head[1] = data[2];
                m_Length = data[3];
                //m_PID         =(ProductID)data[3];
                m_Pump2PC = data[5];
                m_MessageID = data[6];
                m_AppDC = data[7];
                if (Enum.IsDefined(typeof(PumpStatus), data[8]))
                {
                    m_PumpStatus = (PumpStatus)data[8];
                }
                else
                {
                    m_PumpStatus = PumpStatus.Off;
                }
                m_CheckCode = data[9];
            }
        }

    }
}
