using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Auros
{
    class Preprocessor
    {
        public Preprocessor()
        {

        }

        #region math module

        /// <summary>
        /// Menentukan jarak abs 2 point
        /// </summary>
        /// <param name="a">point 1</param>
        /// <param name="b"> point 2</param>
        /// <returns></returns>
        private double TwoPointDistance(Point3D a, Point3D b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2) + Math.Pow(b.Z - a.Z, 2));
        }

        /// <summary>
        /// return sudut antara 3 titik dalam satuan degree
        /// </summary>
        /// <param name="p">titik pivot</param>
        /// <param name="a">titik ujung dengan joint sequence paling dalam, patokan</param>
        /// <param name="b">titik ujung denan joint sequence paling luar</param>
        /// <returns> sudut di titik p , posotif searah SEARAH JARUM JAM DARI DEPAN thumb in</returns>
        private double AngleFromThreePoints(Point3D p, Point3D a, Point3D b, bool fullRotation)
        {
            try
            {
                double sideA = TwoPointDistance(a, p);
                double sideB = TwoPointDistance(b, p);

                Point3D vectorA = new Point3D(a.X - p.X, a.Y - p.Y, a.Z - p.Z);
                Point3D vectorB = new Point3D(b.X - p.X, b.Y - p.Y, b.Z - p.Z);
                Point3D normA = new Point3D(vectorA.X / sideA, vectorA.Y / sideA, vectorA.Z / sideA);
                Point3D normB = new Point3D(vectorB.X / sideB, vectorB.Y / sideB, vectorB.Z / sideB);
                //double angleMagnitude = Math.Acos((Math.Pow(sideA, 2) + Math.Pow(sideB, 2) - Math.Pow(sideC, 2)) / (2 * sideA * sideB));
                double angleMagnitude = Math.Acos(normA.X * normB.X + normA.Y * normB.Y + normA.Z * normB.Z);

                //cari komponen z dari vektor A, kalo hasilnya + berarti panah masuk , hasil - panah keluar
                double angleDirection = Math.Sign(((vectorA.X) * (vectorB.Y)) - ((vectorA.Y) * (vectorB.X)));


                //standardize the joint angle
                if (fullRotation && angleDirection < 0) //only clip angle if full rotate
                {
                    angleMagnitude = (2 * Math.PI) + angleDirection * angleMagnitude;
                }
                return (angleMagnitude / Math.PI) * 180.0;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error] Fail counting angle" + exc.Message.ToString());
                return 1;
            }
        }

        private double[] AngleFromThreePoints(Point3D[] p, Point3D[] a, Point3D[] b, bool fullRotation)
        {
            double[] tempPointCol = new double[p.Length];
            if (p.Length != a.Length || a.Length != b.Length)
            {
                Debug.WriteLine("[Error] fail counting angle, array length mismatch");
            }
            else
            {
                int i = 0;
                foreach (Point3D pPoint in p)
                {
                    tempPointCol[i] = AngleFromThreePoints(p[i], a[i], b[i], fullRotation);
                    i++;
                }
            }
            return tempPointCol;
        }

        /// <summary>
        /// potong kolom joint data yang ada di raw data
        /// </summary>
        /// <param name="jointType"></param>
        /// <returns>reutrn sebanyak baris data raw - 1</returns>
        private Point3D[] SearchJointData(Microsoft.Kinect.JointType jointType, string[][] data)
        {
            int searchIndex = 0;
            Point3D[] pointColumn = new Point3D[data.Length - 1];
            try
            {
                foreach (string label in data[0])
                {
                    if (label == jointType.ToString())
                    {
                        break;
                    }
                    searchIndex++;
                }

                //copy joint data mulai index ke 1

                for (int i = 1; i < data.Length; i++)
                {
                    string[] stringPoint = data[i][searchIndex].Split(';');
                    Point3D tempPoint = new Point3D(Convert.ToDouble(stringPoint[0]), Convert.ToDouble(stringPoint[1]), Convert.ToDouble(stringPoint[2]));
                    pointColumn[i - 1] = tempPoint;
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]Fail searching joint data column >" + exc.Message);
            }
            return pointColumn;
        }
        private T SearchGloveData<T>(Definitions.GloveDataType dataType, string[][] data)
        {
            int searchIndex = 0;
            Point3D[] pointColumn = new Point3D[data.Length - 1];
            double[] valueColumn = new double[data.Length - 1];
            try
            {
                foreach (string label in data[0])
                {
                    if (label == dataType.ToString())
                    {
                        break;
                    }
                    searchIndex++;
                }

                //copy joint data mulai index ke 1
                for (int i = 1; i < data.Length; i++)
                {
                    string[] stringPoint = data[i][searchIndex].Split(';');
                    if (dataType == Definitions.GloveDataType.Gyro || dataType == Definitions.GloveDataType.Accel)
                    {
                        Point3D tempPoint = new Point3D(Convert.ToDouble(stringPoint[0]), Convert.ToDouble(stringPoint[1]), Convert.ToDouble(stringPoint[2]));
                        pointColumn[i - 1] = tempPoint;
                    }
                    else
                    {
                        valueColumn[i - 1] = Convert.ToDouble(stringPoint[0]);
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]Fail searching IMU data column >" + exc.Message);
            }

            if (dataType == Definitions.GloveDataType.Gyro || dataType == Definitions.GloveDataType.Accel)
            {
                return (T)Convert.ChangeType(pointColumn, typeof(T));
            }
            else
            {
                return (T)Convert.ChangeType(valueColumn, typeof(T));
            }

        }

        #endregion

        public String[][] Preprocess(String[][] data, Assessment assess, Definitions.AssessSide side, Item item)
        {
            switch (item.ItemCode)
            {
                case Definitions.ItemCode.U2A:
                    #region preproc item
                    //load featured joints
                    {
                        Point3D[] spShoulder = SearchJointData(Microsoft.Kinect.JointType.SpineShoulder, data);
                        Point3D[] shoulder = null;
                        Point3D[] elbow = null;
                        double[] elevationAngle;
                        //assign joint
                        if (side == Definitions.AssessSide.Left)
                        {
                            shoulder = SearchJointData(Microsoft.Kinect.JointType.ShoulderLeft, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowLeft, data);
                            elevationAngle = AngleFromThreePoints(shoulder, elbow, spShoulder, true);
                        }
                        else
                        {
                            shoulder = SearchJointData(Microsoft.Kinect.JointType.ShoulderRight, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowRight, data);
                            elevationAngle = AngleFromThreePoints(shoulder, spShoulder, elbow, true);
                        }

                        //build preproc file                        
                        string[][] buildPreproc = new string[data.Length][];

                        //labelling
                        buildPreproc[0] = new string[3];
                        buildPreproc[0][0] = "Time_Stamp";
                        buildPreproc[0][1] = "ShoulderElevationAngle";
                        buildPreproc[0][2] = "Trimming_Id";

                        //fill data
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            buildPreproc[i + 1] = new string[3] { data[i + 1][0], elevationAngle[i].ToString(), data[i + 1][Array.IndexOf(data[0], "TrimmingId")] };
                        }
                        return buildPreproc;
                    }
                #endregion
                case Definitions.ItemCode.U3B:
                    #region preproc item
                    //load featured joints
                    {
                        Point3D[] wrist = null;
                        Point3D[] shoulder = null;
                        Point3D[] elbow = null;
                        double[] elbowExtensionAngle;
                        //extracting elbow extension
                        if (side == Definitions.AssessSide.Left)
                        {
                            shoulder = SearchJointData(Microsoft.Kinect.JointType.ShoulderLeft, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowLeft, data);
                            wrist = SearchJointData(Microsoft.Kinect.JointType.WristLeft, data);
                            elbowExtensionAngle = AngleFromThreePoints(elbow, wrist, shoulder, false);
                        }
                        else
                        {
                            shoulder = SearchJointData(Microsoft.Kinect.JointType.ShoulderRight, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowRight, data);
                            wrist = SearchJointData(Microsoft.Kinect.JointType.WristRight, data);
                            elbowExtensionAngle = AngleFromThreePoints(elbow, shoulder, wrist, false);
                        }


                        Point3D[] spineBase = SearchJointData(Microsoft.Kinect.JointType.SpineBase, data);

                        //extracting trunk flexion
                        Point3D[] spineShoulder = SearchJointData(Microsoft.Kinect.JointType.SpineShoulder, data);
                        Point3D[] yProjectedSpineShoulder = new Point3D[data.Length - 1];
                        int CopyIndex = 0;
                        foreach (Point3D ss in spineShoulder)
                        {
                            yProjectedSpineShoulder[CopyIndex] = new Point3D(ss.X, ss.Y, spineBase[CopyIndex].Z);
                            CopyIndex++;
                        }
                        double[] trunkFlexionAngle = AngleFromThreePoints(spineBase, spineShoulder, yProjectedSpineShoulder, false);

                        //extracting trunk rotation
                        Point3D[] hipProjectedShoulder = null;
                        Point3D[] hip = null;
                        if (side == Definitions.AssessSide.Left)
                        {
                            hip = SearchJointData(Microsoft.Kinect.JointType.HipLeft, data);

                        }
                        else
                        {
                            hip = SearchJointData(Microsoft.Kinect.JointType.HipRight, data);
                        }
                        CopyIndex = 0;
                        foreach (Point3D sh in shoulder)
                        {
                            hipProjectedShoulder[CopyIndex] = new Point3D(sh.X, hip[CopyIndex].Y, sh.Z);
                            CopyIndex++;
                        }
                        double[] trunkRotationAngle = AngleFromThreePoints(spineBase, hip, hipProjectedShoulder, false);


                        //build preproc file                        
                        string[][] buildPreproc = new string[data.Length][];
                        //labelling
                        buildPreproc[0] = new string[5];
                        buildPreproc[0][0] = "Time_Stamp";
                        buildPreproc[0][1] = "ElbowExtensionAngle";
                        buildPreproc[0][2] = "TrunkFlexionAngle";
                        buildPreproc[0][3] = "TrunkRotationAngle";
                        buildPreproc[0][4] = "TrimmingId";

                        //fill data
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            buildPreproc[i + 1] = new string[5] { data[i + 1][0], elbowExtensionAngle[i].ToString(), trunkFlexionAngle[i].ToString(), trunkRotationAngle[i].ToString(), data[i + 1][Array.IndexOf(data[0], "TrimmingId")] };
                        }
                        return buildPreproc;
                    }
                #endregion
                case Definitions.ItemCode.U4C:
                    #region preproc item
                    //load featured joints
                    {
                        Point3D[] accelPoint = new Point3D[data.Length - 1];
                        accelPoint = SearchGloveData<Point3D[]>(Definitions.GloveDataType.Accel, data);

                        Point3D[] wrist = null;
                        Point3D[] shoulder = null;
                        Point3D[] elbow = null;
                        double[] elbowExtensionAngle = null;
                        //extracting elbow extension
                        if (side == Definitions.AssessSide.Left)
                        {
                            shoulder = SearchJointData(Microsoft.Kinect.JointType.ShoulderLeft, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowLeft, data);
                            wrist = SearchJointData(Microsoft.Kinect.JointType.WristLeft, data);
                            elbowExtensionAngle = AngleFromThreePoints(elbow, wrist, shoulder, false);
                        }
                        else
                        {
                            shoulder = SearchJointData(Microsoft.Kinect.JointType.ShoulderRight, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowRight, data);
                            wrist = SearchJointData(Microsoft.Kinect.JointType.WristRight, data);
                            elbowExtensionAngle = AngleFromThreePoints(elbow, shoulder, wrist, false);
                        }

                        //sholder extension angle
                        Point3D[] xProjectedShoulder = new Point3D[data.Length - 1];
                        int copyIndex = 0;
                        foreach (Point3D sh in shoulder)
                        {
                            xProjectedShoulder[copyIndex] = new Point3D(sh.X, 0, sh.Z);
                            copyIndex++;
                        }

                        double[] shoulderFlexionAngle = AngleFromThreePoints(shoulder, elbow, xProjectedShoulder, false);

                        //build preproc file                        
                        string[][] buildPreproc = new string[data.Length][];

                        //labelling
                        buildPreproc[0] = new string[6];
                        buildPreproc[0][0] = "Time_Stamp";
                        buildPreproc[0][1] = "AccelZ";
                        buildPreproc[0][2] = "AccelY";
                        buildPreproc[0][3] = "ElbowFlexionAngle";
                        buildPreproc[0][4] = "ShoulderFlexionAngle";
                        buildPreproc[0][5] = "TrimmingId";

                        //fill data horizontally
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            if (side == Definitions.AssessSide.Left)
                            {
                                buildPreproc[i + 1] = new string[6] { data[i + 1][0], accelPoint[i].Z.ToString(), accelPoint[i].Y.ToString(), elbowExtensionAngle[i].ToString(),shoulderFlexionAngle[i].ToString(), data[i + 1][Array.IndexOf(data[0], "TrimmingId")] };
                            }
                            else
                            {
                                //reverse y accel value in left hand
                                buildPreproc[i + 1] = new string[6] { data[i + 1][0], accelPoint[i].Z.ToString(), (-1.0 * accelPoint[i].Y).ToString(), elbowExtensionAngle[i].ToString(), shoulderFlexionAngle[i].ToString(),data[i + 1][Array.IndexOf(data[0], "TrimmingId")] };
                            }

                        }
                        return buildPreproc;
                    }
                #endregion

                case Definitions.ItemCode.U5B:
                    break;

                case Definitions.ItemCode.U7B:
                    break;

                case Definitions.ItemCode.U8C:
                    break;
            }

            //loop array preproc 
            return null;
        }
    }
}
