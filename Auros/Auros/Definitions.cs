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

        public static string TempFileName = "tempdata.csv";

        public static int[] FMALabel = new int[3] {0,1,2 };

        ///HACK pindah file resource
        public static string FeaturedDataEachAssessmentPath = "D://Project//AUROS//Desktop/Auros//Auros//data//filter//FeaturedDataEachAssessment.csv";
        public static string ItemEachAssessmentPath = "D://Project//AUROS//Desktop//Auros//Auros//data//filter//ItemEachAssessment.csv";

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
        public enum AssessmentCode
        {
            U11,
            U21,
            U31,
            U41,
            U42,
            U43,
            U51,
            U52,
            U53,
            U61,
            U71,
            U72,
            U73,
            U74,
            U75,
            U81,
            U82,
            U83,
            U84,
            U85,
            U86,
            U87,
            U91,
            L11,
            L21,
            L22,
            L31,
            L32,
            L41,
            L42,
            L51,
            L61
        };

        public enum FunctionMode
        {
            Training,
            Classify,
            Report,
            Setting
        }

        public static List<String> AssessItemName = new List<string>()
        {
            //Upper Extremity
            "Reflex Activity",                                                                      //U11
            "Flexor Synergy",                                                                       //U21
            "Extensor Synergy",                                                                     //U31
            "Hand to Lumbar Spine",                                                                 //U41
            "Shoulder Flexion to 90° (Elbow at 0°)",                                                //U42
            "Pronation/Supination of Forearm (Elbow at 90° Shoulder at 0°)",                        //U43
            "Shoulder Abduction to 90° (Elbow at 0° and Forearm Pronated)",                         //U51
            "Shoulder Flexion from 90°-180° (Elbow at 0° and Forearm in Mid-Position)",             //U52
            "Pronation/supination of Forearm (Elbow at 0° and Shoulder at 30°-90° of Flexion)",     //U53
            "Normal Reflexes",                                                                      //U61
            "Stability (Elbow at 90° and Shoulder at 0°)",                                          //U71
            "Flexion/Extension (Elbow at 90° and Shoulder at 0°)",                                  //U72
            "Stability (Elbow at 0° and Shoulder at 30° Flexion)",                                  //U73
            "Flexion/Extension (Elbow at 0° and Shoulder at 30° Flexion)",                          //U74
            "Circumduction",                                                                        //U75
            "Finger Mass Flexion",                                                                  //U81
            "Finger Mass Exetension",                                                               //U82
            "Grasp I",                                                                              //U83
            "Grasp II",                                                                             //U84
            "Grasp III",                                                                            //U85
            "Grasp IV",                                                                             //U86
            "Grasp V",                                                                              //U87
            "Finger to Nose",                                                                       //U91

            // Lower Extremitiy
            "Reflex Activity",                                                                      //L11
            "Flexor Synergy",                                                                       //L21
            "Extensor Synergy",                                                                     //L22
            "Knee Flexion Beyond 90°",                                                              //L31
            "Ankle Dorsiflexion (sit)",                                                             //L32
            "Knee Flexion",                                                                         //L41
            "Angkle Dorsiflexion (stand)",                                                          //L42
            "Normal Reflex",                                                                        //L51
            "Heel to Opposite Knee"                                                                 //L61
        };
        public enum ItemCode
        {
            //TODO explain each item in brief
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
        /// <summary>
        /// kode user yang memakai aps
        /// current (therapist, Patient A)
        /// </summary>
        public enum UserCode
        {
            Therapist,
            PatientA
        }

        /// <summary>
        /// Direktori level pertama penyimpanan data
        /// current (train, classify)
        /// </summary>
       

        public enum TrainingState
        {
            Video,
            Idle,
            Recording,
            Hold,
            Labelling,
            Confirmation
        }

        public enum ClassifyingState
        {
            Video,
            Idle,
            Recording,
            Hold,
            Confirmation,
        }
        
    }
}
