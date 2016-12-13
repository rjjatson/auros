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

        public Serial()
        {          

        }
        public void readPort()
        {
            string portName = "COM20";
            string message="nothing";
            bool isReading = false;
            int dataRate = 3;
            

            //setup port
            SerialPort glovePort = new SerialPort();
            glovePort.BaudRate = 9600;
            glovePort.DataBits = 8;
            glovePort.Parity = Parity.None;
            glovePort.StopBits = StopBits.One;
            glovePort.Handshake = Handshake.None;
            glovePort.PortName = portName;
            glovePort.ReadTimeout = 500;
            glovePort.WriteTimeout = 500;

            try
            {               
                glovePort.Open();
                isReading = true;
                Debug.WriteLine("Serial port oppened...");
            }
            catch(Exception e)
            {
                Debug.WriteLine("[Error]fail opening serial port > " + e.Message);
            }
            

            while(isReading)
            {
                try
                {
                    message = glovePort.ReadLine();
                    Debug.WriteLine(message);
                    Thread.Sleep(1000/dataRate);
                }
                catch(Exception e)
                {
                    Debug.WriteLine("[Error]fail reading serial port > " + e.Message);
                    isReading = false;
                }
            }
            glovePort.Close();
        }
           
    }
}
