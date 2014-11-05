using Utils.Tools;  // Required for the added exceptions.
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

#region SERIAL WRAPPER
namespace Utils.Comms.Serial
{
    public class RS232
    {
        private SerialPort _baseSerial;

        private byte[] byteBuffer;
        private int bufferLength = 0;
        private const int BufferMax = 4096;
        public bool DataAvailable { get; set; }

        private object serialLock = new object();

        #region CONSTRUCTORS
        public RS232(string portName)
            : this(portName, 9600, Parity.None, 8, StopBits.One)
        {
        }

        public RS232(string portName, int baudRate)
            : this(portName, baudRate, Parity.None, 8, StopBits.One)
        {
        }

        public RS232(string portName, int baudRate, Parity parity)
            : this(portName, baudRate, parity, 8, StopBits.One)
        {
        }

        public RS232(string portName, int baudRate, Parity parity, int dataBits)
            : this(portName, baudRate, parity, dataBits, StopBits.One)
        {
        }

        public RS232(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _baseSerial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            byteBuffer = new byte[BufferMax];
            _baseSerial.DataReceived += OnDataReceived;
        }
        #endregion

        /// <summary>
        /// Opens the serial line.
        /// </summary>
        public void OpenLine()
        {
            try
            { _baseSerial.Open(); }
            catch
            {
                Debug.Print("Unable to open serial port '" + _baseSerial.PortName + "'");
                throw;
            }
        }

        /// <summary>
        /// Cleses the serial line.
        /// </summary>
        public void CloseLine()
        {
            try
            { _baseSerial.Close(); }
            catch
            {
                Debug.Print("Unable to close serial port '" + _baseSerial.PortName + "'");
                throw;
            }
        }

        /// <summary>
        /// Discards the serial line buffers.
        /// </summary>
        public void ClearBuffer()
        {
            _baseSerial.DiscardOutBuffer();
            _baseSerial.DiscardInBuffer();
            byteBuffer = new byte[BufferMax];
        }

        #region DATA RECEIVED EVENTS
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (serialLock)
            {
                int bytesReceived = 0;
                if (_baseSerial.IsOpen)
                { bytesReceived = _baseSerial.Read(byteBuffer, bufferLength, _baseSerial.BytesToRead); }

                if (bytesReceived > 0)
                {
                    bufferLength += bytesReceived;

                    if (bufferLength >= BufferMax)
                        throw new BufferOverflowException("Buffer overflow.");

                    DataAvailable = true;
                }
            }
        }
        #endregion

        #region READ DATA
        /// <summary>
        /// Reads the next available line of text in the buffer.
        /// </summary>
        /// <returns>The line if one was found, otherwise an empty string.</returns>
        public string ReadLine()
        {
            string line = "";

            lock (serialLock)
            {
                for (int i = 0; i < bufferLength; i++)
                {
                    if (byteBuffer[i] == '\n' || byteBuffer[i] > 127)
                    {
                        byteBuffer[i] = 0;

                        try
                        { line += new string(Encoding.UTF8.GetChars(byteBuffer, 0, i)); }
                        catch
                        { }

                        if (line != null && line != "")
                            Debug.Print("Line rcvd: " + line);

                        bufferLength = bufferLength - i - 1;

                        Array.Copy(byteBuffer, i + 1, byteBuffer, 0, bufferLength);

                        break;
                    }
                }
            }

            return line.Trim();
        }

        /// <summary>
        /// Reads a set number of bytes from the buffer.
        /// </summary>
        /// <param name="bytesToRead">The number of bytes to read in.</param>
        /// <returns>An array of bytes if the buffer has that many, otherwise null.</returns>
        public byte[] ReadBytes(int bytesToRead)
        {
            byte[] bytes = new byte[bytesToRead];

            lock (serialLock)
            {
                if (bufferLength >= bytesToRead)
                {
                    // Copy X number of bytes from class buffer into local buffer
                    Array.Copy(byteBuffer, 0, bytes, 0, bytesToRead);

                    // Update bufferLength
                    bufferLength -= bytesToRead;

                    // Move bytes remaining in buffer to the beginning of the buffer
                    Array.Copy(byteBuffer, bytesToRead, byteBuffer, 0, bufferLength);
                }
                else
                {
                    bytes = null;
                }
            }

            return bytes;
        }
        #endregion

        #region WRITE DATA
        /// <summary>
        /// Writes a string of text to the serial line.
        /// </summary>
        /// <param name="data">The text to write.</param>
        public void Write(string data)
        {
            Write(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes an array of bytes to the serial line
        /// </summary>
        /// <param name="buffer">The bytes to send through the serial line.</param>
        /// <param name="offset">The starting point in the array.</param>
        /// <param name="count">How many bytes to senf.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            _baseSerial.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes an array of bytes to the serial line
        /// </summary>
        /// <param name="buffer">The bytes to send through the serial line.</param>
        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }
        #endregion
    }
#endregion
}
