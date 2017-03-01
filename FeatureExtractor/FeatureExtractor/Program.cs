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

        static void Main(string[] args)
        {
            FileFixer();

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

            //TODO create extract files
            string extractionPath = "Files/Training/Extract/";
            if (!Directory.Exists(extractionPath)) Directory.CreateDirectory(extractionPath);

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
                        Console.WriteLine("[Success]Found (" + userPath.Length + ") users on " + cP.ToString());
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("[Error]Loading user directory >" + exc.Message.ToString());
                    }
                    //find every file
                    foreach (string uP in userPath)
                    {
                        string[] filePath = Directory.GetFiles(uP + "/");
                        Console.WriteLine("[Success]Found (" + filePath.Length + ") data files on " + uP.ToString());
                        foreach (string fp in filePath)
                        {
                            //read data
                            try
                            {
                                String[] fileRead = File.ReadAllLines(fp);
                                String[][] csvRead = new string[fileRead.Length][];

                                int readIndex = 0;

                                foreach (string line in fileRead)
                                {
                                    csvRead[readIndex] = line.Split(',');
                                    readIndex++;
                                }
                                //TODO clipping here

                                //TODO preprocess here

                                //Extract here
                                if (assessmentCode != "U7B")//HACK debug sementara tanpa u7b
                                {
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

                                //TODO save the result here, check first if extract data exist


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
                string[][] selectedData = columnSelector(data, featureIndex);

                List<string> max = DataOperation(selectedData, Operation.Max);
                extractedData.Add(max);

                List<string> min = DataOperation(selectedData, Operation.Min);
                extractedData.Add(min);

                List<string> avg = DataOperation(selectedData, Operation.Avg);
                extractedData.Add(avg);

                List<string> var = DataOperation(selectedData, Operation.Var);
                extractedData.Add(var);

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
            List<string> dataReturn = new List<string>();
            dataReturn.Add(data[0][1].ToString() + "_" + op.ToString());

            try
            {
                string trimming_id = "0";
                foreach (string[] d in data)
                {
                    if (d != data[0])
                    {
                        if (d[2] != trimming_id || d == data[data.Length - 1]) //trimming id switch
                        {
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
                                    //dataReturn.Add(DataMin(dataBuffer).ToString());
                                    break;
                            }
                            dataBuffer.Clear();
                            trimming_id = d[2];
                        }
                        else
                        {
                            dataBuffer.Add(Convert.ToDouble(d[1]));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("[Error]Fail Calculating data max >" + exc.Message.ToString());
            }
            return dataReturn;
        }

        #region statistic extractor
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
        #endregion

        public static void FileFixer()
        {
            //BUG u78 wrist flexion do not show correct value on preproc
            //root cause : forget to chirp data array each element
            // soving, overwrite preproc data
            Console.WriteLine("Start fixing U7B bug..");
            string rawFolderPath = "Files/Training/Raw/U72/";
            string[] userRawDir = Directory.GetDirectories(rawFolderPath);
            //iterates trhough user files
            foreach (string rd in userRawDir)
            {
                if (rd.Split('/')[rd.Split('/').Length - 1] != "Dummy")
                {
                    string[] userRawFile = Directory.GetFiles(rd + "/", "*.csv");
                    foreach (string rf in userRawFile)
                    {
                        string prerpocFolderPath = "Files/Training/Preproc/U7B/" + rf.Split('/')[rf.Split('/').Length - 2] + "/" + rf.Split('/')[rf.Split('/').Length - 1];
                        string[] line = File.ReadAllLines(rf);
                        string[][] sourceData = new string[line.Length][];

                        int lineIndex = 0;
                        Point3D[] hand = new Point3D[line.Length - 1];
                        Point3D[] wrist = new Point3D[line.Length - 1];
                        Point3D[] elbow = new Point3D[line.Length - 1];
                        double[] wristFlexion = new double[line.Length - 1];

                        foreach (string l in line)
                        {
                            if (lineIndex != 0)
                            {
                                string[] lineData = l.Split(',');

                                string[] wristStr = lineData[5].Split(';');
                                wrist[lineIndex - 1] = new Point3D(Convert.ToDouble(wristStr[0]), Convert.ToDouble(wristStr[1]), Convert.ToDouble(wristStr[2]));

                                string[] handStr = lineData[4].Split(';');
                                hand[lineIndex - 1] = new Point3D(Convert.ToDouble(handStr[0]), Convert.ToDouble(handStr[1]), Convert.ToDouble(handStr[2]));

                                string[] elbowStr = lineData[6].Split(';');
                                elbow[lineIndex - 1] = new Point3D(Convert.ToDouble(elbowStr[0]), Convert.ToDouble(elbowStr[1]), Convert.ToDouble(elbowStr[2]));

                                wristFlexion[lineIndex - 1] = AngleFromThreePoints(wrist[lineIndex - 1], elbow[lineIndex - 1], hand[lineIndex - 1], false);

                            }
                            lineIndex++;
                        }

                        line = File.ReadAllLines(prerpocFolderPath);
                        string[][] destinationData = new string[line.Length][];
                        lineIndex = 0;
                        foreach (string l in line)
                        {
                            destinationData[lineIndex] = l.Split(',');
                            if (lineIndex != 0)
                            {
                                destinationData[lineIndex][3] = wristFlexion[lineIndex - 1].ToString();
                            }
                            lineIndex++;
                        }

                        StringBuilder prepString = new StringBuilder();
                        foreach(string[] destLine in destinationData)
                        {
                            bool isFirst = true;
                            string stringLine = string.Empty;
                            foreach(string destEl in destLine)
                            {
                                if (!isFirst) stringLine += ",";
                                stringLine += destEl;
                                isFirst = false;
                            }
                            prepString.AppendLine(stringLine);
                        }
                        File.WriteAllText(prerpocFolderPath,prepString.ToString());

                    }
                }
            }
            Console.WriteLine("Finish fixing U7B bug..");
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
