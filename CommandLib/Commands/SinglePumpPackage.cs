using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    public class SinglePumpPackage
    {
        private byte m_Chanel = 0;
        private PowerInfo m_Power;
        private AlarmInfo m_Alarm;


        public byte Chanel
        {
            get { return m_Chanel;}
        }
        public AlarmInfo Alarm
        {
            get { return m_Alarm;}
        }

         public PowerInfo Power
        {
            get { return m_Power;}
        }

        public SinglePumpPackage() { }

        public SinglePumpPackage(byte[] data) 
        { 
            if(data.Length!=22)
            {
                Logger.Instance().ErrorFormat("单泵信息有误，长度不等于22字节,data={0}", data.Length);
            }
            else
            {
                m_Chanel = data[0];
                byte[] tempPower = new byte[9];
                Array.Copy(data,1,tempPower,0,9);
                m_Power = new PowerInfo(tempPower);
                byte[] tempAlarm = new byte[12];
                Array.Copy(data,10,tempAlarm,0,12);
                m_Alarm = new AlarmInfo(tempAlarm);
            }
        }

        public List<byte> GetBytes()
        {
            List<byte> buffer = new List<byte>();
            buffer.Add(m_Chanel);
            buffer.AddRange(m_Power.GetBytes());
            buffer.AddRange(m_Alarm.GetBytes());
            return buffer;
        }
    }

    public class PowerInfo
    {
        private byte[]          m_Head        = new byte[]{0x55,0xAA};
        private byte            m_Length      = 0x05;
        private ProductID       m_PID;
        private byte            m_Pump2PC     = 0x00;
        private byte            m_MessageID   = 0x58;
        private byte            m_AppDC       = 0x01;
        private PumpPowerStatus m_PowerStatus = PumpPowerStatus.External;
        private byte            m_CheckCode;

        public PumpPowerStatus PowerStatus
        {
            get { return m_PowerStatus;}
            set { m_PowerStatus = value;}
        }

        public PowerInfo(){}

        public PowerInfo(byte [] data)
        {
            if(data.Length!=9)
            {
                Logger.Instance().Error("PowerInfo实例化失败，没有9个字节！");
            }
            else
            {
                m_Head[0]     =                 data[0];
                m_Head[1]     =                 data[1];
                m_Length      =                 data[2];
                //m_PID         =(ProductID)data[3];
                m_Pump2PC     =                 data[4];   
                m_MessageID   =                 data[5];   
                m_AppDC       =                 data[6];   
                if(Enum.IsDefined(typeof(PumpPowerStatus), data[7]))
                {
                    m_PowerStatus =(PumpPowerStatus)data[7];   
                }
                else
                {
                    m_PowerStatus = PumpPowerStatus.External;
                }
                m_CheckCode   =                 data[8];    
            }
        }

        public List<byte> GetBytes()
        {
             List<byte> buffer = new List<byte>();
             buffer.AddRange(m_Head);
             buffer.Add((byte)m_PID);
             buffer.Add(m_Pump2PC);
             buffer.Add(m_MessageID);
             buffer.Add(m_AppDC);
             buffer.Add((byte)m_PowerStatus);
             buffer.Add(m_CheckCode);
             return buffer;
        }
    }

     public class AlarmInfo
    {
        private byte[]          m_Head        = new byte[]{0x55,0xAA};
        private byte            m_Length      = 0x05;
        private ProductID       m_PID;
        private byte            m_Pump2PC     = 0x00;
        private byte            m_MessageID   = 0x57;
        private byte            m_AppDC       = 0x04;
        private uint            m_Alarm       = 0x00;
        private byte            m_CheckCode;

        public uint Alarm
        {
            get { return m_Alarm;}
            set { m_Alarm = value;}
        }

        public AlarmInfo(){}

        public AlarmInfo(byte [] data)
        {
            if(data.Length!=12)
            {
                Logger.Instance().Error("AlarmInfo实例化失败，没有12个字节！");
            }
            else
            {
                m_Head[0]     =    data[0];
                m_Head[1]     =    data[1];
                m_Length      =    data[2];
                //m_PID         =    (ProductID)data[3];
                m_Pump2PC     =    data[4];   
                m_MessageID   =    data[5];   
                m_AppDC       =    data[6];        
                m_Alarm       =    (uint)(data[7] & 0x000000FF);
                m_Alarm       +=   (uint)(data[8] << 8 & 0x0000FF00);   
                m_Alarm       +=   (uint)(data[9] << 16 & 0x00FF0000);   
                m_Alarm       +=   (uint)(data[10] << 24 & 0xFF000000);   
                m_CheckCode   =    data[11];    
            }
        }

         public List<byte> GetBytes()
        {
             List<byte> buffer = new List<byte>();
             buffer.AddRange(m_Head);
             buffer.Add((byte)m_PID);
             buffer.Add(m_Pump2PC);
             buffer.Add(m_MessageID);
             buffer.Add(m_AppDC);
             buffer.Add((byte)(m_Alarm & 0x000000FF));
             buffer.Add((byte)(m_Alarm >> 8 &0x000000FF));
             buffer.Add((byte)(m_Alarm >> 16 &0x000000FF));
             buffer.Add((byte)(m_Alarm >> 24 &0x000000FF));
             buffer.Add(m_CheckCode);
             return buffer;
        }

        
    }
}
