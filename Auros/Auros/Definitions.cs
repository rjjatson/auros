using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.IO.Ports;

namespace Auros
{
    public class Definitions
    {
        /// <summary>
        /// if true cari port com1 sampai com50, else pakai Definitions.PortNumber
        /// </summary>
        public static bool IsAutoSearchSerialPort = false;
        public static int PortNumber = 5;
        public static int MaxPortNumber = 50;

        public static int BaudRate = 9600;
        public static int DataBits = 8;
        public static Parity DataParity = Parity.None;
        public static StopBits DataStopBits = StopBits.One;
        public static Handshake DataHandShake = Handshake.None;
        public static int ReadTimeout = 500;
        public static int WriteTimeout = 500;
        /// <summary>
        /// list joints raw data urut dari atas kebawah kiri ke kanan kepala dan alat gerak
        /// </summary>
        public static List<JointType> FeaturedJointList = new List<JointType>()
        {
            JointType.Head,
            JointType.HandLeft,
            JointType.WristLeft,
            JointType.ElbowLeft,
            JointType.ShoulderLeft,
            JointType.SpineShoulder,
            JointType.ShoulderRight,
            JointType.ElbowRight,
            JointType.WristRight,
            JointType.HandRight,
            JointType.FootLeft,
            JointType.AnkleLeft,
            JointType.KneeLeft,
            JointType.HipLeft,
            JointType.SpineBase,
            JointType.HipRight,
            JointType.KneeRight,
            JointType.AnkleRight,
            JointType.FootRight
        };
    }
}
