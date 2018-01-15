using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    /// <summary>
    /// 老化补电命令
    /// </summary>
    public class CmdRecharge : BaseCommand
    {
        /// <summary>
        /// 对于ASAD来说，只有回应一条命令。
        /// </summary>
        public CmdRecharge()
            : base(0x00, 0x05)
        { }

        /// <summary>
        /// 将要发送的命令变成字节数组
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
            //if(payloadData.Length!=2)
            //{
            //    Logger.Instance().ErrorFormat("CmdUploadSN::SetBytes() Error! payloadDataLength!=2 Length={0}", payloadData.Length);
            //    return;
            //}
            //else
            //{
            //    this.m_SerialNo =  (ushort)(payloadData[1] << 8 & 0xFF00);
            //    this.m_SerialNo += (ushort)(payloadData[0] & 0x00FF);
            //}
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
