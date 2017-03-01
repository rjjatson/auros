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
                        Point3D[] hipProjectedShoulder = new Point3D[data.Length - 1];
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
                        //1. accelY
                        Point3D[] accelPoint = SearchGloveData<Point3D[]>(Definitions.GloveDataType.Accel, data);
                        double[] accelY = new double[data.Length - 1];
                        double[] accelZ = new double[data.Length - 1];
                        int searchIndex = 0;
                        foreach (Point3D ap in accelPoint)
                        {
                            accelY[searchIndex] = ap.Y;
                            if (side == Definitions.AssessSide.Right) accelY[searchIndex] *= -1.0;
                            searchIndex++;
                        }

                        //2. accelX
                        searchIndex = 0;
                        foreach (Point3D ap in accelPoint)
                        {
                            accelZ[searchIndex] = ap.Z;
                            searchIndex++;
                        }

                        //3. gyroX
                        Point3D[] gyroPoint = SearchGloveData<Point3D[]>(Definitions.GloveDataType.Gyro, data);
                        double[] gyroX = new double[data.Length - 1];
                        searchIndex = 0;
                        foreach (Point3D gp in gyroPoint)
                        {
                            gyroX[searchIndex] = gp.X;
                            if (side == Definitions.AssessSide.Right) gyroX[searchIndex] *= -1.0;
                            searchIndex++;
                        }

                        //4. elbow extension
                        Point3D[] wrist = null;
                        Point3D[] shoulder = null;
                        Point3D[] elbow = null;
                        double[] elbowExtensionAngle;
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

                        //5. Shoulder flexion
                        Point3D[] xProjectedShoulder = new Point3D[data.Length - 1];
                        int copyIndex = 0;
                        foreach (Point3D sh in shoulder)
                        {
                            xProjectedShoulder[copyIndex] = new Point3D(sh.X, 0, sh.Z);
                            copyIndex++;
                        }
                        Point3D[] shoulderProjectedElbow = new Point3D[data.Length - 1];
                        copyIndex = 0;
                        foreach (Point3D el in elbow)
                        {
                            shoulderProjectedElbow[copyIndex] = new Point3D(shoulder[copyIndex].X, el.Y, el.Z);
                            copyIndex++;

                        }
                        double[] shoulderFlexionAngle = AngleFromThreePoints(shoulder, shoulderProjectedElbow, xProjectedShoulder, false);

                        //build preproc file                        
                        string[][] buildPreproc = new string[data.Length][];

                        //labelling
                        buildPreproc[0] = new string[7];
                        buildPreproc[0][0] = "Time_Stamp";
                        buildPreproc[0][1] = "AccelYArmPronation";
                        buildPreproc[0][2] = "AccelXArmPronation";
                        buildPreproc[0][3] = "GyroXArmPronation";
                        buildPreproc[0][4] = "ElbowExtensionAngle";
                        buildPreproc[0][5] = "ShoulderFlexionAngle";
                        buildPreproc[0][6] = "Trimming_Id";

                        //fill data
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            buildPreproc[i + 1] = new string[7] {
                                data[i + 1][0],
                                accelY[i].ToString(),
                                accelZ[i].ToString(),
                                gyroX[i].ToString(),
                                elbowExtensionAngle[i].ToString(),
                                shoulderFlexionAngle[i].ToString(),
                                data[i + 1][Array.IndexOf(data[0], "TrimmingId")]
                            };
                        }
                        return buildPreproc;
                    }
                #endregion
                case Definitions.ItemCode.U5B:
                    #region preproc item
                    //load featured joints
                    {
                        //1. accelY
                        Point3D[] accelPoint = SearchGloveData<Point3D[]>(Definitions.GloveDataType.Accel, data);
                        double[] accelY = new double[data.Length - 1];
                        double[] accelZ = new double[data.Length - 1];
                        int searchIndex = 0;
                        foreach (Point3D ap in accelPoint)
                        {
                            accelY[searchIndex] = ap.Y;
                            if (side == Definitions.AssessSide.Right) accelY[searchIndex] *= -1.0;
                            searchIndex++;
                        }

                        //2. accelX
                        searchIndex = 0;
                        foreach (Point3D ap in accelPoint)
                        {
                            accelZ[searchIndex] = ap.Z;
                            searchIndex++;
                        }

                        //3. gyroX
                        Point3D[] gyroPoint = SearchGloveData<Point3D[]>(Definitions.GloveDataType.Gyro, data);
                        double[] gyroX = new double[data.Length - 1];
                        searchIndex = 0;
                        foreach (Point3D gp in gyroPoint)
                        {
                            gyroX[searchIndex] = gp.X;
                            if (side == Definitions.AssessSide.Right) gyroX[searchIndex] *= -1.0;
                            searchIndex++;
                        }

                        //4. elbow extension
                        Point3D[] wrist = null;
                        Point3D[] shoulder = null;
                        Point3D[] elbow = null;
                        double[] elbowExtensionAngle;
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

                        //5. Shoulder elevation
                        Point3D[] spShoulder = SearchJointData(Microsoft.Kinect.JointType.SpineShoulder, data);      
                        double[] elevationAngle;
                        if (side == Definitions.AssessSide.Left)
                        {
                            elevationAngle = AngleFromThreePoints(shoulder, elbow, spShoulder, true);
                        }
                        else
                        {
                            elevationAngle = AngleFromThreePoints(shoulder, spShoulder, elbow, true);
                        }

                        //6. Shoulder flexion
                        Point3D[] xProjectedShoulder = new Point3D[data.Length - 1];
                        int copyIndex = 0;
                        foreach (Point3D sh in shoulder)
                        {
                            xProjectedShoulder[copyIndex] = new Point3D(sh.X, 0, sh.Z);
                            copyIndex++;
                        }
                        Point3D[] shoulderProjectedElbow = new Point3D[data.Length - 1];
                        copyIndex = 0;
                        foreach (Point3D el in elbow)
                        {
                            shoulderProjectedElbow[copyIndex] = new Point3D(shoulder[copyIndex].X, el.Y, el.Z);
                            copyIndex++;

                        }
                        double[] shoulderFlexionAngle = AngleFromThreePoints(shoulder, shoulderProjectedElbow, xProjectedShoulder, false);

                        //build preproc file                        
                        string[][] buildPreproc = new string[data.Length][];

                        //labelling
                        buildPreproc[0] = new string[8];
                        buildPreproc[0][0] = "Time_Stamp";
                        buildPreproc[0][1] = "AccelYArmPronation";
                        buildPreproc[0][2] = "AccelXArmPronation";
                        buildPreproc[0][3] = "GyroXArmPronation";
                        buildPreproc[0][4] = "ElbowExtensionAngle";
                        buildPreproc[0][5] = "ShoulderAbductionAngle";
                        buildPreproc[0][5] = "ShoulderFlexionAngle";
                        buildPreproc[0][6] = "Trimming_Id";

                        //fill data
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            buildPreproc[i + 1] = new string[8] {
                                data[i + 1][0],
                                accelY[i].ToString(),
                                accelZ[i].ToString(),
                                gyroX[i].ToString(),
                                elbowExtensionAngle[i].ToString(),
                                elevationAngle[i].ToString(),
                                shoulderFlexionAngle[i].ToString(),
                                data[i + 1][Array.IndexOf(data[0], "TrimmingId")]
                            };
                        }
                        return buildPreproc;
                    }
                #endregion
                case Definitions.ItemCode.U7B:
                    #region preproc item
                    //load featured joints
                    {
                        //1. accelZ
                        Point3D[] accelPoint = SearchGloveData<Point3D[]>(Definitions.GloveDataType.Accel, data);
                        double[] accelZ = new double[data.Length - 1];
                        int searchIndex = 0;
                        foreach (Point3D ap in accelPoint)
                        {
                            accelZ[searchIndex] = ap.Z;
                            if (side == Definitions.AssessSide.Right) accelZ[searchIndex] *= -1.0;
                            searchIndex++;
                        }

                        //2. flex
                        double[] flex = SearchGloveData<double[]>(Definitions.GloveDataType.Flex, data);

                        //3. wristflexion

                        Point3D[] hand = null;
                        Point3D[] wrist = null;
                        Point3D[] elbow = null;
                        double[] wristFlexion = null;
                        //assign joint
                        if (side == Definitions.AssessSide.Left)
                        {
                            wrist = SearchJointData(Microsoft.Kinect.JointType.WristLeft, data);
                            hand = SearchJointData(Microsoft.Kinect.JointType.HandLeft, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowLeft, data);
                        }
                        else
                        {
                            wrist = SearchJointData(Microsoft.Kinect.JointType.WristRight, data);
                            hand = SearchJointData(Microsoft.Kinect.JointType.HandRight, data);
                            elbow = SearchJointData(Microsoft.Kinect.JointType.ElbowRight, data);
                        }

                        wristFlexion = AngleFromThreePoints(wrist, elbow, hand, false);


                        //build preproc file                        
                        string[][] buildPreproc = new string[data.Length][];

                        //labelling
                        buildPreproc[0] = new string[5];
                        buildPreproc[0][0] = "Time_Stamp";
                        buildPreproc[0][1] = "AccelZWrist";
                        buildPreproc[0][2] = "FlexWrist";
                        buildPreproc[0][3] = "WristFlexion";
                        buildPreproc[0][4] = "Trimming_Id";

                        //fill data
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            buildPreproc[i + 1] = new string[5] {
                                data[i + 1][0],
                                accelZ[i].ToString(),
                                flex[i].ToString(),
                                wristFlexion[i].ToString(),
                                data[i + 1][Array.IndexOf(data[0], "TrimmingId")] };
                        }
                        return buildPreproc;
                    }
                #endregion
                case Definitions.ItemCode.U8C:
                    #region preproc item
                    //load featured joints
                    {
                        
                        //1. flex
                        double[] flex = SearchGloveData<double[]>(Definitions.GloveDataType.Flex, data);

                        //2. force
                        double[] force = SearchGloveData<double[]>(Definitions.GloveDataType.Force, data);
                       

                        //build preproc file                        
                        string[][] buildPreproc = new string[data.Length][];

                        //labelling
                        buildPreproc[0] = new string[4];
                        buildPreproc[0][0] = "Time_Stamp";
                        buildPreproc[0][1] = "FlexFinger";
                        buildPreproc[0][2] = "ForcePalm";
                        buildPreproc[0][3] = "Trimming_Id";

                        //fill data
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            buildPreproc[i + 1] = new string[4] {
                                data[i + 1][0],
                                flex[i].ToString(),
                                force[i].ToString(),
                                data[i + 1][Array.IndexOf(data[0], "TrimmingId")] };
                        }
                        return buildPreproc;
                    }
                    #endregion
            }
            Debug.WriteLine("[Error] Cant find preprocessor for item >" + item.ItemCode);       
            return null;
        }
    }
}
