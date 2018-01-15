using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;

namespace Trans
{
    public class SerialPortDevice : ConnectionDevice
    {
        #region "Const"
        private readonly int        BAUDRATE = 115200;
        private readonly int        DATABITS = 8;               
        private readonly StopBits   STOPBITS = StopBits.One;
        private readonly Parity     PARITY = Parity.None;
        private readonly Handshake  HANDSHAKE=Handshake.None; 
        #endregion
        #region "Device varialbes"
        private SerialPort m_ConnectedSerialPort = null;
        #endregion
        
        #region "Event"
        public event EventHandler<DataTransmissionEventArgs> DataReceived;
        #endregion
        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public SerialPortDevice()
        {
        }
        #endregion
        #region "Initialize serial port device"
        /// <summary>
        /// use serial port and set the port name
        /// </summary>
        /// <param name="portName">serial port name</param>
        public void InitializeDevice(string portName)
        {
            InitializeDevice(portName, BAUDRATE, DATABITS, STOPBITS, PARITY, HANDSHAKE);
        }
        /// <summary>
        /// use serial port, set the port name and baud rate
        /// </summary>
        /// <param name="portName">serial port name</param>
        /// <param name="baudRate">serial port baud rate</param>
        public void InitializeDevice(string portName, int baudRate)
        {
            InitializeDevice(portName, baudRate, DATABITS, STOPBITS, PARITY, HANDSHAKE);
        }
        /// <summary>
        /// use serial port, set the port name,baud rate,data bits,stopbits,parity
        /// </summary>
        /// <param name="portName">serial port name</param>
        /// <param name="baudRate">serial port baud rate</param>
        /// <param name="dataBits">serial port data bits</param>
        /// <param name="StopBits">serial port stopbits</param>
        /// <param name="Parity">serial port parity</param>
        public void InitializeDevice(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity)
        {
            InitializeDevice(portName, baudRate, dataBits, stopBits, parity, HANDSHAKE);
        }

        /// <summary>
        /// use serial port, set the port name,baud rate,data bits,stopbits,parity
        /// </summary>
        /// <param name="portName">serial port name</param>
        /// <param name="baudRate">serial port baud rate</param>
        /// <param name="dataBits">serial port data bits</param>
        /// <param name="StopBits">serial port stopbits</param>
        /// <param name="Parity">serial port parity</param>
        /// <param name="handshake">handshake type(hardware )</param>
        public void InitializeDevice(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, Handshake handshake)
        {
            ResetDevice();
            if (null != m_ConnectedSerialPort)
            {
                m_ConnectedSerialPort.PortName = portName;
                m_ConnectedSerialPort.BaudRate = baudRate;
                m_ConnectedSerialPort.DataBits = dataBits;
                m_ConnectedSerialPort.StopBits = stopBits;
                m_ConnectedSerialPort.Parity = parity;
                m_ConnectedSerialPort.Handshake = handshake;
            }
        }

        /// <summary>
        /// use serial port and set port information
        /// </summary>
        /// <param name="devInfo">serial port to use</param>
        public void InitializeDevice(SerialPort devInfo)
        {
            ResetDevice();
            if (null != m_ConnectedSerialPort)
            {
                m_ConnectedSerialPort = devInfo;
            }
        }
        /// <summary>
        /// set device type with SerialPort and close other devices
        /// </summary>
        private void ResetDevice()
        {
            try
            {
                Close();
            }
            catch (Exception)
            {
            }
            if (null == m_ConnectedSerialPort)
            {
                m_ConnectedSerialPort = new SerialPort();
            }
        }

        /// <summary>
        /// The number of bytes in the internal input buffer
        /// before a System.IO.Ports.SerialPort.DataReceived event is fired
        /// </summary>
        /// <param name="len"></param>
        public void SetReceivedBytesThreshold(int len = 1)
        {
            if (m_ConnectedSerialPort != null)
                m_ConnectedSerialPort.ReceivedBytesThreshold = len;
        }
        #endregion
        #region "Open Close IsOpen"
        public override void Open()
        {
            if (null != m_ConnectedSerialPort)
            {
                if (!m_ConnectedSerialPort.IsOpen)
                {
                    try
                    { 
                        m_ConnectedSerialPort.DataReceived += SerialPortDataReceived;
                        m_ConnectedSerialPort.Open();
                    }
                    catch (Exception e)
                    {
                        //throw new Exception("Open - ", e);
                    }
                }
            }
        }
        public override void Close()
        {
            if (null != m_ConnectedSerialPort)
            {
                m_ConnectedSerialPort.DataReceived -= SerialPortDataReceived;
                if (m_ConnectedSerialPort.IsOpen)
                {
                    try
                    {
                        m_ConnectedSerialPort.DiscardInBuffer();
                        m_ConnectedSerialPort.DiscardOutBuffer();
                        m_ConnectedSerialPort.Close();
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Close - ", e);
                    }
                }
            }
        }
        /// <summary>
        /// Gets a value that indicates whether the connected device is open 
        /// </summary>
        /// <returns>true if the device is open; otherwise,false</returns>
        public override bool IsOpen()
        {
            bool isOpen = false;
            if (null != m_ConnectedSerialPort)
            {
                isOpen = m_ConnectedSerialPort.IsOpen;
            }
            return isOpen;
        }
        #endregion
        #region "Send data"
        public override void SendData(byte[] data)
        {
            if (m_ConnectedSerialPort.IsOpen)
            {
                try
                {
                    // Writer raw data
                    this.m_ConnectedSerialPort.Write(data, 0, data.Length);
                }
                catch (Exception e)
                {
                    throw new Exception("Write - ", e);
                }
            }
            else
            {
                throw new Exception("Write - Port is not open");
            }
        }
        #endregion
        #region "Data Recerived"
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //read from stream
                int n = m_ConnectedSerialPort.BytesToRead;
                byte[] buff = new byte[n];
                m_ConnectedSerialPort.Read(buff, 0, n);
                //save to the buffer
                if (DataReceived != null)
                {
                    DataReceived(this, new DataTransmissionEventArgs(buff));
                }

            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message, exception);
                
            }
        }       
        #endregion
    }
}
