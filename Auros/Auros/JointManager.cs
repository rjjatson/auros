using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Kinect;

namespace Auros
{
    class JointManager
    {
        /// <summary>
        /// Menentukan jarak abs 2 point
        /// </summary>
        /// <param name="a">point 1</param>
        /// <param name="b"> point 2</param>
        /// <returns></returns>
        public double TwoPointDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        /// <summary>
        /// Menentukan jarak abs 2 point
        /// </summary>
        /// <param name="a">point 1</param>
        /// <param name="b"> point 2</param>
        /// <returns></returns>
        public double TwoPointDistance(CameraSpacePoint a, CameraSpacePoint b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2) +Math.Pow(b.Z-a.Z,2));
        }

        /// <summary>
        /// Menentukan jarak abs 2 point
        /// </summary>
        /// <param name="a">point 1</param>
        /// <param name="b"> point 2</param>
        /// <returns></returns>
        public double DotProduct3D(CameraSpacePoint a, CameraSpacePoint b)
        {
            return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        }

        /// <summary>
        /// return sudut antara 3 titik
        /// </summary>
        /// <param name="p">titik pivot</param>
        /// <param name="a">titik ujung dengan joint sequence paling dalam, patokan</param>
        /// <param name="b">titik ujung denan joint sequence paling luar</param>
        /// <returns> sudut di titik p , searah jarum jam positif, berlawanan negatif</returns>
        public double AngleFromThreePoints(Point p, Point a, Point b)
        {
            try
            {
                double sideA = TwoPointDistance(a, p);
                double sideB = TwoPointDistance(b, p);
                double sideC = TwoPointDistance(a, b);
                

                double angleDirection = Math.Sign(((a.X - p.X) * (b.Y - p.Y)) - ((a.Y - p.Y) * (b.X - p.X)));
                if (angleDirection == 0) angleDirection = 1;

                return angleDirection * Math.Acos((Math.Pow(sideA, 2) + Math.Pow(sideB, 2) - Math.Pow(sideC, 2)) / (2 * sideA * sideB));
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]fail AnlgeFromThreePoints" + exc.Message.ToString());
                return 1;
            }
        }

        /// <summary>
        /// return sudut antara 3 titik
        /// </summary>
        /// <param name="p">titik pivot</param>
        /// <param name="a">titik ujung dengan joint sequence paling dalam, patokan</param>
        /// <param name="b">titik ujung denan joint sequence paling luar</param>
        /// <returns> sudut di titik p , searah jarum jam positif, berlawanan negatif</returns>
        public double AngleFromThreePoints(CameraSpacePoint p, CameraSpacePoint a, CameraSpacePoint b)
        {
            try
            {
                double sideA = TwoPointDistance(a, p);
                double sideB = TwoPointDistance(b, p);
                double sideC = TwoPointDistance(a, b);

                //double angleDirection = Math.Sign(((a.X - p.X) * (b.Y - p.Y)) - ((a.Y - p.Y) * (b.X - p.X)));
                //if (angleDirection == 0) double angleDirection = 1;
                double angleDirection = 180/ Math.PI;
                
                return angleDirection * Math.Acos( (Math.Pow(sideA, 2) + Math.Pow(sideB, 2) - Math.Pow(sideC, 2)) / (2 * sideA * sideB) );
            }
            catch (Exception exc)
            {
                Debug.WriteLine("[Error]fail AnlgeFromThreePoints" + exc.Message.ToString());
                return 1;
            }
        }

    }
}
