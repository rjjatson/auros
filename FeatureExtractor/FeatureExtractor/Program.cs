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
                    Console.WriteLine("Cannot open Log.txt for writing");
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
                    #region read user path foreach asessment
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
                        Console.WriteLine("[Success]Found (" + userPath.Length + ") users on " + cP.ToString());
                        Console.WriteLine();
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("[Error]Loading user directory >" + exc.Message.ToString());
                    }
                    #endregion
                    //find every file
                    foreach (string uP in userPath)
                    {
                        string[] filePath = Directory.GetFiles(uP + "/");
                        Console.WriteLine("[Success]Found (" + filePath.Length + ") data files on " + uP.ToString());
                        Console.WriteLine();
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
                                string[][] ExtractedData = Extract(csvRead, fp.Split('_')[fp.Split('_').Length - 1]);

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

        /// <summary>
        /// Mencari nilai maksimal data
        /// </summary>
        /// <param name="data">jagged array 3 indeks 1= timestamp, 2= fitur, 3= trimming id</param>
        /// <returns></returns>
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
        public static double DataMax(List<double> data)
        {
            double maxData = 0.0;
            foreach (double d in data)
            {
                if (d > maxData) maxData = d;
            }
            return maxData;
        }

        public static double DataMin(List<double> data)
        {
            double minData = 999999999999.0;
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

    }
}
