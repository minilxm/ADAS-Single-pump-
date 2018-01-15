using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    public class GetAlarm : BaseCommand
    {
        private List<SinglePumpPackage> m_PumpPackages = new List<SinglePumpPackage>();

        public List<SinglePumpPackage> PumpPackages
        {
            get { return m_PumpPackages;}
        }

        /// <summary>
        /// 对于ADAS来说，此命令是接收命令。只能回应
        /// </summary>
        public GetAlarm()
            : base(0x01, 0x07)
        { }

        /// <summary>
        /// 将要发送的命令变成字节数组,本命令只是回应，不能主动发送
        /// </summary>
        /// <returns></returns>
        public override List<byte> GetBytes()
        {
            //先计算Payload长度
            UpdatePayloadLength(0);
            m_Direction = 1;
            //命令头部（payload length（含）之前）
            List<byte> basebuffer = base.GetBytes();
            //取checksum字节
            uint checksum = CRC32.CalcCRC32Partial(basebuffer, basebuffer.Count, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
            byte[] arrChecksum = StructConverter.StructureToByte<uint>(checksum);
            basebuffer.AddRange(arrChecksum);
            return basebuffer;
        }
 
        public  List<byte> GetBytesDebug()
        {
            byte payloadLength = (byte)(m_PumpPackages.Count*this.m_Channel);
            this.m_Direction = 0;
            //先计算Payload长度
            UpdatePayloadLength(payloadLength);
            //命令头部（payload length（含）之前）
            List<byte> basebuffer = base.GetBytes();
            for(int i=0;i<m_PumpPackages.Count;i++)
            {
                basebuffer.AddRange(m_PumpPackages[i].GetBytes());
            }
            //取checksum字节
            uint checksum = CRC32.CalcCRC32Partial(basebuffer, basebuffer.Count, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
            byte[] arrChecksum = StructConverter.StructureToByte<uint>(checksum);
            basebuffer.AddRange(arrChecksum);
            return basebuffer;
        }

        /// <summary>
        /// 泵报警信息命令比较特殊，它的通道号不是指具体的通道ID，而是一个单泵包的字节大小，
        /// 现在定义是22字节
        /// </summary>
        /// <param name="payloadData"></param>
        public override void SetBytes(byte[] payloadData)
        {
            if(payloadData.Length==0)
            {
                Logger.Instance().Error("报警信息数据包有误,数据包长度为0！");
                return;
            }
            byte packageSize = this.Channel;
            byte payloadLength = this.m_PayloadLength;
            if(payloadLength%packageSize!=0)
            {
                Logger.Instance().ErrorFormat("报警信息数据包有误不是单包的整数倍，单包大小={0},包总大小={1}",packageSize,payloadLength);
                return;
            }
            int count = payloadLength/packageSize;      //必须是整数倍，否则一定是错的
            byte[] temp = new byte[packageSize];
            for(int iLoop=0;iLoop<count;iLoop++)
            {
                Array.Clear(temp, 0, packageSize);
                Array.Copy(payloadData,iLoop*packageSize,temp,0,packageSize);
                m_PumpPackages.Add(new SinglePumpPackage(temp));
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
}
