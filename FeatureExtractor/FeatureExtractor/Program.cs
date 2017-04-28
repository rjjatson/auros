using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FeatureExtractor
{
    class Program
    {
        public enum Operation { Min, Max, Avg, Var, Jerk, Speed, Accel };
        public static double clipTolerance = 0.3;

        static void Main(string[] args)
        {
            //FileFixer();

            bool isLog = false;
            FileStream ostrm = null;
            StreamWriter writer = null;
            TextWriter oldOut = Console.Out;
            if (isLog)
            {
                try
                {
                    ostrm = new FileStream("./Log.txt", FileMode.OpenOrCreate, FileAccess.Write);
                    writer = new StreamWriter(ostrm);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Error] Cannot open Log.txt for writing");
                    Console.WriteLine(e.Message);
                    return;
                }
                Console.SetOut(writer);
            }

            Init();

            //find root directory
            string[] codePath = null;
            codePath = Directory.GetDirectories("Files/Training/Preproc/");

            //HACK create extract files , if extract file exist, delete
            string extractionPath = "Files/Training/Extract/";
            if (!Directory.Exists(extractionPath))
            {
                Directory.CreateDirectory(extractionPath);
            }
            else
            {
                Directory.Delete(extractionPath, true);
                Directory.CreateDirectory(extractionPath);
            }

            if (codePath != null)
            {
                foreach (string cP in codePath)
                {
                    string[] userPath = null;
                    string assessmentCode = cP.Split('/')[cP.Split('/').Length - 1];
                    try
                    {
                        userPath = Directory.GetDirectories(cP + "/");
                        //deleting dummies
                        foreach (string uP in userPath)
                        {
                            if (uP.Contains("Dummy"))
                            {
                                Directory.Delete(uP, true);
                            }
                        }
                        userPath = Directory.GetDirectories(cP + "/");
                        Console.WriteLine();
                        Console.WriteLine("[On Progress]Extracting user data on " + cP.ToString() + ".....");
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("[Error]Loading user directory >" + exc.Message.ToString());
                    }
                    //find every file
                    foreach (string uP in userPath)
                    {
                        string[] filePath = Directory.GetFiles(uP + "/");
                        Console.WriteLine("[On Progress]Extracting file " + uP.ToString() + ".....");
                        foreach (string fp in filePath)
                        {
                            //read data
                            try
                            {
                                string[] fileRead = File.ReadAllLines(fp);
                                string[][] csvRead = new string[fileRead.Length][];

                                int readIndex = 0;

                                foreach (string line in fileRead)
                                {
                                    csvRead[readIndex] = line.Split(',');
                                    readIndex++;
                                }

                                //save the file for review

                                //TODO preprocess here

                                string[][] ExtractedData = Extract(csvRead, fp.Split('_')[fp.Split('_').Length - 1]);

                                string savePath = extractionPath + assessmentCode + ".csv";
                                StringBuilder csvBuilder = new StringBuilder();
                                bool isHeader = File.Exists(savePath);
                                foreach (string[] line in ExtractedData)
                                {
                                    if (!isHeader)
                                    {
                                        string lineString = string.Empty;
                                        bool isLabel = true;
                                        foreach (string col in line)
                                        {
                                            if (!isLabel) lineString += ",";
                                            lineString += col;
                                            isLabel = false;
                                        }
                                        csvBuilder.AppendLine(lineString);
                                    }
                                    isHeader = false;
                                }

                                if (File.Exists(savePath))
                                {
                                    File.AppendAllText(savePath, csvBuilder.ToString());
                                }
                                else
                                {
                                    File.WriteAllText(savePath, csvBuilder.ToString());
                                }

                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine("[Error]Fail reading fle > " + exc.Message.ToString());
                            }
                        }

                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("[Error]Cant find root directory");
            }

            if (isLog)
            {
                Console.SetOut(oldOut);
                writer.Close();
                ostrm.Close();
                Console.WriteLine("[Success] Saving Session to Log.txt");
            }
            Console.ReadLine();
        }
        
        public static void Init()
        {
            Console.WriteLine("*AUROS FEATURE EXTRACTOR*");
            Console.WriteLine("Place this file in root directory of trainning or classification");
            Console.WriteLine("Make sure the data files is not read-only");
            Console.WriteLine("=================================================================");
        }

        public static string[][] Extract(string[][] data, string labelScore)
        {
            List<List<string>> extractedData = new List<List<string>>();
            //TODO insert labeeeel

            //iterate trhough feature, index 0 mulai dari sebelah kanan time_stamp
            for (int featureIndex = 1; featureIndex < data[0].Length - 1; featureIndex++)
            {
                if (data[0][featureIndex]=="FlexWrist")
                {
                    featureIndex++;
                }
                string[][] selectedData = columnSelector(data, featureIndex);

                List<string> max = DataOperation(selectedData, Operation.Max);
                extractedData.Add(max);

                List<string> min = DataOperation(selectedData, Operation.Min);
                extractedData.Add(min);

                List<string> avg = DataOperation(selectedData, Operation.Avg);
                extractedData.Add(avg);

                List<string> var = DataOperation(selectedData, Operation.Var);
                extractedData.Add(var);

                if (data[0][featureIndex].Contains("Angle") || data[0][featureIndex] == "WristFlexion")
                {
                    List<string> jerk = DataOperation(selectedData, Operation.Jerk);
                    extractedData.Add(jerk);
                }

            }

            //add label
            List<string> label = new List<string>();
            label.Add("label");
            for (int i = 1; i < extractedData[0].Count; i++) label.Add(labelScore.Split('.')[0]);
            extractedData.Insert(0, label);


            string[][] extractedDataString = new string[extractedData[0].Count][];

            for (int i = 0; i < extractedDataString.Length; i++)
            {
                extractedDataString[i] = new string[extractedData.Count];
                for (int j = 0; j < extractedDataString[i].Length; j++)
                {
                    extractedDataString[i][j] = extractedData[j][i];
                }
            }

            return extractedDataString;
        }

        /// <summary>
        /// returning data dengan waktu, fitur, dan trimming id
        /// </summary>
        /// <param name="data">jagged data </param>
        /// <param name="rowNum">nomor fitur</param>
        /// <returns></returns>
        public static string[][] columnSelector(string[][] data, int rowNum)
        {
            string[][] selectedData = new string[data.Length][];
            try
            {
                for (int i = 0; i < data.Length; i++)
                {
                    selectedData[i] = new string[3];
                    selectedData[i][0] = data[i][0];
                    selectedData[i][1] = data[i][rowNum];
                    selectedData[i][2] = data[i][data[i].Length - 1];
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("[Error]Fail selecting data column >" + exc.Message.ToString());
            }

            return selectedData;
        }

        public static List<string> DataOperation(string[][] data, Operation op)
        {
            List<double> dataBuffer = new List<double>();
            List<double> timeBuffer = new List<double>();
            List<string> dataReturn = new List<string>();
            dataReturn.Add(data[0][1].ToString() + "_" + op.ToString());

            try
            {
                string trimming_id = "0";
                foreach (string[] d in data)
                {
                    if (d != data[0]) //remove the header
                    {
                        if (d[2] != trimming_id || d == data[data.Length - 1]) //event : trimming id switch
                        {
                            //TODO clipping data buffer here, overwrite data buffer

                            List<double>[] clip = ClippedData(timeBuffer, dataBuffer);//cut timer buffer

                            timeBuffer = clip[0]; dataBuffer = clip[1];

                            switch (op)
                            {
                                case Operation.Max:
                                    dataReturn.Add(DataMax(dataBuffer).ToString());
                                    break;
                                case Operation.Min:
                                    dataReturn.Add(DataMin(dataBuffer).ToString());
                                    break;
                                case Operation.Avg:
                                    dataReturn.Add(DataAvg(dataBuffer).ToString());
                                    break;
                                case Operation.Var:
                                    dataReturn.Add(DataVar(dataBuffer).ToString());
                                    break;
                                case Operation.Speed:
                                    //dataReturn.Add(DataMin(dataBuffer).ToString());
                                    break;
                                case Operation.Accel:
                                    //dataReturn.Add(DataMin(dataBuffer).ToString());
                                    break;
                                case Operation.Jerk:
                                    dataReturn.Add(DataJerk(timeBuffer, dataBuffer).ToString()); //TODO create jerk extraction from accel data
                                    break;
                            }
                            dataBuffer.Clear();
                            timeBuffer.Clear();

                            dataBuffer.Add(Convert.ToDouble(d[1]));
                            timeBuffer.Add(Convert.ToDouble(d[0]));
                            trimming_id = d[2];
                        }
                        else
                        {
                            dataBuffer.Add(Convert.ToDouble(d[1]));
                            timeBuffer.Add(Convert.ToDouble(d[0]));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("[Error]Fail processing >" + exc.Message.ToString());
            }
            return dataReturn;
        }

        private static List<double>[] ClippedData(List<double> time, List<double> data)
        {
            //RawDataExporter(data, "rawData");
            List<double> clippedData = new List<double>();
            List<double> clippedTime = new List<double>();
            int lastCLip = data.Count() - 1, firstClip = 0;
            List<double> absSpeed = new List<double>();
            double avgSpeed = 0.0;
            try
            {
                absSpeed =
                Abs
                (
                Differentiate(time, data)
                );
                avgSpeed = DataAvg(absSpeed);
                for (int i = 0; i < data.Count(); i++)
                {
                    if (lastCLip == data.Count() - 1)
                    {
                        if (absSpeed[absSpeed.Count() - 1 - i] > avgSpeed) lastCLip = data.Count() - 1 - i;
                    }
                    if (firstClip == 0)
                    {
                        if (absSpeed[i] > avgSpeed) firstClip = i;
                    }
                    if (firstClip != 0 && lastCLip != data.Count() - 1) break;
                }

                //Tolerating clipping ratio
                double clipRat = Convert.ToDouble(lastCLip-firstClip+1) / Convert.ToDouble(data.Count());
                if (clipRat < clipTolerance)
                {
                    lastCLip = data.Count() - 1;
                    firstClip = 0;
                }

                for (int i = firstClip; i <= lastCLip; i++)
                {
                    clippedData.Add(data[i]);
                    clippedTime.Add(time[i]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error]Cliping data >" + ex.Message);
            }

            //double clipRats = Convert.ToDouble(clippedData.Count()) / Convert.ToDouble(data.Count());
            //if (clippedData.Count() < data.Count()) Console.WriteLine("[Evaluae] Clipping ratio " + (clipRats * 100.0).ToString() + "%");
            //RawDataExporter(clippedData, "clipped");
           
            return new List<double>[] { clippedTime, clippedData };
        }

        #region statistic extractor
        public static List<double> Abs(List<double> data)
        {
            List<double> absVal = new List<double>();
            foreach (double d in data)
            {
                absVal.Add(Math.Abs(d));
            }
            return absVal;
        }

        public static double DataMax(List<double> data)
        {
            double maxData = data[0];
            foreach (double d in data)
            {
                if (d > maxData) maxData = d;
            }
            return maxData;
        }

        public static double DataMin(List<double> data)
        {
            double minData = data[0];
            foreach (double d in data)
            {
                if (d < minData) minData = d;
            }
            return minData;
        }

        public static double DataAvg(List<double> data)
        {
            double avgData = 0.0;
            foreach (double d in data)
            {
                avgData += d;
            }
            return avgData / data.Count();
        }

        public static double DataVar(List<double> data)
        {
            double avgData = DataAvg(data);
            double varData = 0.0;
            foreach (double d in data)
            {
                varData += Math.Pow(d - avgData, 2);
            }
            return varData;
        }

        public static double DataJerk(List<double> time, List<double> data)
        {
            //RawDataPlotter(time, data, "rawData");
            //RawDataPlotter(time, Differentiate(time, data), "firstDiffData");
            //RawDataPlotter(time, Differentiate(time, Differentiate(time, data)), "secondDiffData");
            try
            {
                List<double> squaredJerk = SquareValue(Differentiate(time, Differentiate(time, Differentiate(time, data))));
                double integralJerk = Integral(time, squaredJerk);
                double length = FindLength(data);
                double duration = time[time.Count() - 1] - time[0];
                return Math.Sqrt(0.5 * integralJerk * (Math.Pow(duration, 5) / Math.Pow(length, 2)));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Error]Jerk fail >" + e.Message);
            }

            return 0.0;
        }

        #endregion

        #region timeseries extractor
        /// <summary>
        /// Mencari hasil turunan data time series terhadap waktu 
        /// dValue / dt
        /// </summary>
        /// <param name="time">list time in mili sec</param>
        /// <param name="rawData">list of values</param>
        /// <returns></returns>
        public static List<double> Differentiate(List<double> time, List<double> rawData)
        {
            List<double> diffData = new List<double>();
            for (int i = 0; i < rawData.Count(); i++)
            {
                if (i == 0)
                {
                    diffData.Add((rawData[i + 1] - rawData[i]) / (time[i + 1] - time[i]));
                }
                else
                {
                    diffData.Add((rawData[i] - rawData[i - 1]) / (time[i] - time[i - 1]));
                }
            }
            return diffData;
        }

        /// <summary>
        /// Integral between time interval
        /// </summary>
        /// <param name="time"></param>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static double Integral(List<double> time, List<double> rawData)
        {
            double totalArea = 0.0;

            //counting integral using trapezoid value
            for (int i = 1; i < rawData.Count(); i++)
            {
                totalArea += (((rawData[i] + rawData[i - 1]) / 2) * (time[i] - time[i - 1]));
            }
            return totalArea;
        }

        public static List<double> SquareValue(List<double> baseData)
        {
            List<double> sqVal = new List<double>();
            foreach (double bd in baseData)
            {
                sqVal.Add(Math.Pow(bd, 2));
            }
            return sqVal;
        }

        public static double FindLength(List<double> rawData)
        {
            double maxDiff = 0.0;
            foreach (double rd in rawData)
            {
                if (Math.Abs(rawData[0] - rd) > maxDiff)
                {
                    maxDiff = Math.Abs(rawData[0] - rd);
                }
            }
            return maxDiff;
        }

        #endregion

        #region misc file fixer
        public static void FileFixer()
        {
            //BUG u78 V2 wrist flexion do not show correct value on preproc
            //root cause : forget to chirp data array each element
            // soving, overwrite preproc data
          
        }

        private static double TwoPointDistance(Point3D a, Point3D b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2) + Math.Pow(b.Z - a.Z, 2));
        }

        private static double AngleFromThreePoints(Point3D p, Point3D a, Point3D b, bool fullRotation)
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
                Console.WriteLine("[Error] Fail counting angle" + exc.Message.ToString());
                return 1;
            }
        }
        #endregion

        /// <summary>
        /// export satu list double
        /// </summary>
        /// <param name="data"> list double</param>
        /// <param name="fileName">nama file tanpa *.csv</param>
        public static void RawDataExporter(List<double> data, string fileName)
        {
            StringBuilder stringPlotter = new StringBuilder();
            for (int i = 0; i < data.Count(); i++)
            {
                stringPlotter.AppendLine(data[i].ToString());
            }
            File.AppendAllText("Files/" + fileName + ".csv", stringPlotter.ToString());
        }
    }

    internal class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3D()
        {

        }
    }
}
