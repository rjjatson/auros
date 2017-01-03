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

        public enum AssessSide { Right, Left };
        public enum AssessLimb { Upper, Lower };
        public enum AssessItemCode
        {
            //TODO : explain each item in brief
            U1A, //BICEPT REFLEX 0/2
            U1B, //TRICEPT REFLEX 0/2
            U2A, //
            U2B,
            U2C,
            U2D,
            U2E,
            U2F,
            U3A,
            U3B,
            U3C,
            U4A,
            U4B,
            U4C,
            U5A,
            U5B,
            U5C,
            U6A,
            U7A,
            U7B,
            U7C,
            U7D,
            U7E,
            U8A,
            U8B,
            U8C,
            U8D,
            U8E,
            U8F,
            U8G,
            U9A,
            U9B,
            U9C,
            /// <summary>
            /// achilles reflex
            /// </summary>
            L1A,
            L1B,
            L2A,
            L2B,
            L2C,
            L2D,
            L2E,
            L2F,
            L2G,
            L3A,
            L3B,
            L4A,
            L4B,
            L5A,
            L6A,
            L6B,
            L6C

        };

        public static List<String> AssessItemName = new List<string>()
        {
            //Upper Extremity
            "Reflex Activity", //1ab
            "Flexor Synergy",//2abcdef
            "Extensor Synergy",//3abc
            "Hand to Lumbar Spine",//4a
            "Shoulder Flexion to 90° (Elbow at 0°)",//4b
            "Pronation/Supination of Forearm (Elbow at 90° Shoulder at 0°)",//4c
            "Shoulder Abduction to 90° (Elbow at 0° and Forearm Pronated)",//5a
            "Shoulder Flexion from 90°-180° (Elbow at 0° and Forearm in Mid-Position)",//5b
            "Pronation/supination of Forearm (Elbow at 0° and Shoulder at 30°-90° of Flexion)",//5c
            "Normal Reflexes",//6a
            "Stability (Elbow at 90° and Shoulder at 0°)",//7a
            "Flexion/Extension (Elbow at 90° and Shoulder at 0°)",//7b
            "Stability (Elbow at 0° and Shoulder at 30° Flexion)",//7c
            "Flexion/Extension (Elbow at 0° and Shoulder at 30° Flexion)",//7d
            "Circumduction",//7e
            "Finger Mass Flexion",//8a
            "Finger Mass Exetension",//8b
            "Grasp I",//8c
            "Grasp II",//8d
            "Grasp III",//8e
            "Grasp IV",//8f
            "Grasp V",//8g
            "Finger to Nose",//9abc

            // Lower Extremitiy
            "Reflex Activity", //1ab
            "Flexor Synergy", //2abc
            "Extensor Synergy", //2defg
            "Knee Flexion Beyond 90°", //3a
            "Ankle Dorsiflexion (sit)", //3b
            "Knee Flexion", //4a
            "Angkle Dorsiflexion (stand)",//4b
            "Normal Reflex", //5a
            "Heel to Opposite Knee"//6abc

        };
    }
}
