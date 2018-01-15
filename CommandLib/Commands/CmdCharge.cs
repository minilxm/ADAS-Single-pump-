using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    /// <summary>
    /// 老化充电命令
    /// </summary>
    public class CmdCharge : BaseCommand
    {
        private byte[] m_Rate = new byte[3] { 0, 0, 0 };     //低中高对应的索引为0,1,2
        private ScaleValue m_RateScale = ScaleValue.None;
        private byte[] m_Volume = new byte[3] { 0, 0, 0 };     //低中高对应的索引为0,1,2
        private ScaleValue m_VolumeScale = ScaleValue.None;
        private OcclusionLevel m_OcclusionLevel = OcclusionLevel.H;
        /// <summary>
        /// 对于ASAD来说，只有回应一条命令。
        /// </summary>
        public CmdCharge()
            : base(0x00, 0x03)
        { }

        public void SetRate(decimal rate)
        {
            decimal intPart = decimal.Truncate(rate);
            int intRate = decimal.ToInt32(intPart); //速率的整数部分
            decimal decimalRate = rate - intRate;   //速率的小数部分
            int decimalRateLength = decimalRate.ToString().Length-2;

            switch (decimalRateLength)
            {
                case 0:
                case 1:
                    intRate *= 10;
                    intRate += (int)(decimalRate * 10);
                    m_RateScale = ScaleValue.Ten;
                    break;
                case 2:
                    intRate *= 100;
                    intRate += (int)(decimalRate * 100);
                    m_RateScale = ScaleValue.Hundred;
                    break;
                case 3:
                    intRate *= 1000;
                    intRate += (int)(decimalRate * 1000);
                    m_RateScale = ScaleValue.Thousand;
                    break;
                case 4:
                    intRate *= 10000;
                    intRate += (int)(decimalRate * 10000);
                    m_RateScale = ScaleValue.TenThousand;
                    break;
                default:
                    intRate *= 10;
                    intRate += (int)(decimalRate * 10);
                    m_RateScale = ScaleValue.Ten;
                    break;
            }
            m_Rate[0] = (byte)(intRate & 0x000000FF);
            m_Rate[1] = (byte)(intRate>>8 & 0x000000FF);
            m_Rate[2] = (byte)(intRate>>16 & 0x000000FF);
        }

        public void SetVolume(decimal vol)
        {
            decimal intPart = decimal.Truncate(vol);
            int intvol = decimal.ToInt32(intPart); //速率的整数部分
            decimal decimalvol = vol - intvol;     //速率的小数部分
            int decimalvolLength = decimalvol.ToString().Length - 2;

            switch (decimalvolLength)
            {
                case 0:
                case 1:
                    intvol *= 10;
                    intvol += (int)(decimalvol * 10);
                    m_VolumeScale = ScaleValue.Ten;
                    break;
                case 2:
                    intvol *= 100;
                    intvol += (int)(decimalvol * 100);
                    m_VolumeScale = ScaleValue.Hundred;
                    break;
                case 3:
                    intvol *= 1000;
                    intvol += (int)(decimalvol * 1000);
                    m_VolumeScale = ScaleValue.Thousand;
                    break;
                case 4:
                    intvol *= 10000;
                    intvol += (int)(decimalvol * 10000);
                    m_VolumeScale = ScaleValue.TenThousand;
                    break;
                default:
                    intvol *= 10;
                    intvol += (int)(decimalvol * 10);
                    m_VolumeScale = ScaleValue.Ten;
                    break;
            }
            m_Volume[0] = (byte)(intvol & 0x000000FF);
            m_Volume[1] = (byte)(intvol >> 8 & 0x000000FF);
            m_Volume[2] = (byte)(intvol >> 16 & 0x000000FF);
        }

        /// <summary>
        /// 设置压力
        /// </summary>
        /// <param name="level"></param>
        public void SetOcclusionLevel(OcclusionLevel level)
        {
            m_OcclusionLevel = level;
        }

        /// <summary>
        /// 将要发送的命令变成字节数组
        /// </summary>
        /// <returns></returns>
        public override List<byte> GetBytes()
        {
            //先计算Payload长度
            UpdatePayloadLength(9);
            //命令头部（payload length（含）之前）
            List<byte> basebuffer = base.GetBytes();
            basebuffer.AddRange(m_Rate);
            basebuffer.Add((byte)m_RateScale);
            basebuffer.AddRange(m_Volume);
            basebuffer.Add((byte)m_VolumeScale);
            basebuffer.Add((byte)m_OcclusionLevel);
            //取checksum字节
            uint checksum = CRC32.CalcCRC32Partial(basebuffer, basebuffer.Count, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
            byte[] arrChecksum = StructConverter.StructureToByte<uint>(checksum);
            basebuffer.AddRange(arrChecksum);
            return basebuffer;
        }


          public  List<byte> GetBytesDebug()
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

        /// <summary>
        /// 将要发送的命令变成字节数组，用于带序列号的测试
        /// </summary>
        /// <returns></returns>
        public override List<byte> GetBytesEx()
        {
            //先计算Payload长度
            UpdatePayloadLength(0);
            //命令头部（payload length（含）之前）
            List<byte> basebuffer = base.GetBytesEx();
            //取checksum字节
            uint checksum = CRC32.CalcCRC32Partial(basebuffer, basebuffer.Count, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
            byte[] arrChecksum = StructConverter.StructureToByte<uint>(checksum);
            basebuffer.AddRange(arrChecksum);
            return basebuffer;
        }

        public override void SetBytes(byte[] payloadData)
        {
             
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
        /// <summary>
        /// 当超时后调用回调函数
        /// </summary>
        public override void InvokeTimeOut()
        {
            base.InvokeTimeOut();
        }
    }
}
