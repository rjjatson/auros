using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace Auros
{
    public class Serial
    {
        public string messageBuffer { get; set; }
        private SerialPort glovePort;

        public Serial()
        {

        }
        public void OpenPort(int comPortNumber)
        {
            try
            {
                //setup port
                glovePort = new SerialPort();
                glovePort.BaudRate = Definitions.BaudRate;
                glovePort.DataBits = Definitions.DataBits;
                glovePort.Parity = Definitions.DataParity;
                glovePort.StopBits = Definitions.DataStopBits;
                glovePort.Handshake = Definitions.DataHandShake;
                //glovePort.PortName = (Definitions.IsAutoSearchSerialPort) ? ("COM" + comPortNumber.ToString()) : ("COM" + Definitions.PortNumber.ToString());
                glovePort.PortName = "COM" + Definitions.PortNumber.ToString();
                glovePort.ReadTimeout = Definitions.ReadTimeout;
                glovePort.WriteTimeout = Definitions.ReadTimeout;

                glovePort.Open();
                Debug.WriteLine("[Success]Serial port oppened");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]Fail opening serial port > " + e.Message);
                if (Definitions.IsAutoSearchSerialPort && comPortNumber < Definitions.MaxPortNumber)
                {
                    Debug.WriteLine("[Debug]Try opening higher port number > ");
                    glovePort = null;
                    OpenPort(comPortNumber++);
                }
            }

        }
        public void ClosePort()
        {
            try
            {
                glovePort.Close();
                Debug.WriteLine("[Success]Serial port clsoed");
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]Fail closing serial port > " + e.Message);
            }
        }

        public string ReadPort()
        {
            string message = "null";
            try
            {
                message = glovePort.ReadLine();
                Debug.WriteLine(message);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Error]fail reading serial port > " + e.Message);
            }
            return message;
        }
    }
}
