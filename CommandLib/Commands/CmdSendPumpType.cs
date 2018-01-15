using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    public class CmdSendPumpType : BaseCommand
    {
        private ProductID m_ProductID;
        private ushort m_QueryInterval;  //查询周期单位：秒
        private List<PumpStatus> m_PumpStatusList = new List<PumpStatus>(); //泵状态，此属性由WIFI模块返回,多个泵状态，由通道号从低到高排列

        /// <summary>
        /// 泵型号
        /// </summary>
        public ProductID  PID
        {
            get { return m_ProductID; }
            set { m_ProductID = value; }
        }

        /// <summary>
        /// 查询周期单位：秒
        /// </summary>
        public ushort QueryInterval
        {
            get { return m_QueryInterval; }
            set { m_QueryInterval = value; }
        }

        /// <summary>
        /// 泵状态，此属性由WIFI模块返回,多个泵状态，由通道号从低到高排列
        /// </summary>
        public List<PumpStatus> PumpStatusList
        {
            get { return m_PumpStatusList; }
            set { m_PumpStatusList = value; }
        }

        /// <summary>
        /// 对于ASAD来说，只有回应一条命令。
        /// </summary>
        public CmdSendPumpType()
            : base(0x00, 0x02)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid">产品ID</param>
        /// <param name="queryInterval">WIFI模块自动上传周期</param>
        public CmdSendPumpType(ProductID pid, ushort queryInterval)
            : base(0x00, 0x02)
        {
            m_ProductID = pid;
            m_QueryInterval = queryInterval;
        }

        /// <summary>
        /// 将要发送的命令变成字节数组
        /// </summary>
        /// <returns></returns>
        public override List<byte> GetBytes()
        {
            //先计算Payload长度
            UpdatePayloadLength(3);
            //命令头部（payload length（含）之前）
            List<byte> basebuffer = base.GetBytes();
            basebuffer.Add((byte)(m_ProductID));
            basebuffer.Add((byte)(m_QueryInterval & 0x00FF));
            basebuffer.Add((byte)(m_QueryInterval >> 8 & 0x00FF));
            //取checksum字节
            uint checksum = CRC32.CalcCRC32Partial(basebuffer, basebuffer.Count, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
            byte[] arrChecksum = StructConverter.StructureToByte<uint>(checksum);
            basebuffer.AddRange(arrChecksum);
            return basebuffer;
        }

        /// <summary>
        /// 将要发送的命令变成字节数组
        /// </summary>
        /// <returns></returns>
        public  List<byte> GetBytesDebug()
        {
            //先计算Payload长度
            UpdatePayloadLength(6);
            //命令头部（payload length（含）之前）
            this.m_Channel = 0x3F;

            List<byte> basebuffer = base.GetBytes();
            if (m_PumpStatusList == null)
                m_PumpStatusList = new List<PumpStatus>();
            m_PumpStatusList.Clear();
            m_PumpStatusList.Add(PumpStatus.Stop);
            m_PumpStatusList.Add(PumpStatus.Run);
            m_PumpStatusList.Add(PumpStatus.Stop);
            m_PumpStatusList.Add(PumpStatus.Run);
            m_PumpStatusList.Add(PumpStatus.Stop);
            m_PumpStatusList.Add(PumpStatus.Run);

            for (int i = 0; i < m_PumpStatusList.Count;i++ )
            {
                basebuffer.Add((byte)(m_PumpStatusList[i]));
            }
            //取checksum字节
            uint checksum = CRC32.CalcCRC32Partial(basebuffer, basebuffer.Count, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
            byte[] arrChecksum = StructConverter.StructureToByte<uint>(checksum);
            basebuffer.AddRange(arrChecksum);
            return basebuffer;
        }

        public override void SetBytes(byte[] payloadData)
        {
            m_PumpStatusList.Clear();
            //每个泵一个状态，所以数据长度不是1,而是动态变化的
            for (int i = 0; i < payloadData.Length; i++)
            {
                m_PumpStatusList.Add((PumpStatus)payloadData[i]);
            }
        }

        /// <summary>
        /// 复制命令
        /// </summary>
        /// <param name="other"></param>
        public override void Copy(BaseCommand other)
        {
            base.Copy(other);
            this.PumpStatusList = ((CmdSendPumpType)other).PumpStatusList;
        }
        public override void InvokeResponse()
        {
            base.InvokeResponse();
        }

        /// <summary>
        /// 当超时后调用回调函数
        /// </summary>
        public override void InvokeTimeOut()
        {
            base.InvokeTimeOut();
        }

    }
}
