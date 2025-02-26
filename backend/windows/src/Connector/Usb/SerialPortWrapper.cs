using System;
using System.IO.Ports;

namespace Connector
{
    /// <summary>
    /// Interface for a serial port. This is used to abstract the serial port implementation for testing.
    /// </summary>
    public interface ISerialPort
    {
        bool IsOpen { get; }
        void Open();
        void Close();
        void Write(byte[] bytes, int offset, int count);
        int ReadByte();
        void DiscardInBuffer();
        void DiscardOutBuffer();
    }

    /// <summary>
    /// Wrapper for the SerialPort class to allow for easier testing.
    /// </summary>
    public class SerialPortWrapper : ISerialPort
    {
        private readonly SerialPort _serialPort;

        public SerialPortWrapper(SerialPort serialPort)
        {
            _serialPort = serialPort;
        }

        public bool IsOpen => _serialPort.IsOpen;

        public void Open()
        {
            _serialPort.Open();
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public void Write(byte[] bytes, int offset, int count)
        {
            _serialPort.Write(bytes, offset, count);
        }

        public int ReadByte()
        {
            return _serialPort.ReadByte();
        }

        public void DiscardInBuffer()
        {
            _serialPort.DiscardInBuffer();
        }

        public void DiscardOutBuffer()
        {
            _serialPort.DiscardOutBuffer();
        }
    }
}