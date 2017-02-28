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
        static void Main(string[] args)
        {
            OpeningText();

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

                                //TODO Extract here

                                Extract(csvRead);

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

            Console.ReadLine();
        }
        public static void OpeningText()
        {
            Console.WriteLine("*AUROS FEATURE EXTRACTOR*");
            Console.WriteLine("Place this file in root directory of trainning or classification");
            Console.WriteLine("Make sure the data files is not read-only");
            Console.WriteLine("=================================================================");
        }


        public static string[][] Extract(string[][] data)
        {
            string[][] extractedData = new string[data.Length][];
            //iterate trhough feature, index 0 mulai dari sebelah kanan time_stamp
            for (int featureIndex = 1; featureIndex < data.Length - 1; featureIndex++)
            {
                string[][] selectedData = columnSelector(data, featureIndex);
                List<string> max = DataMax(selectedData);
                    //min
            }

            return extractedData;
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

        #region data - max
        /// <summary>
        /// Mencari nilai maksimal data
        /// </summary>
        /// <param name="data">jagged array 3 indeks 1= timestamp, 2= fitur, 3= trimming id</param>
        /// <returns></returns>
        public static List<string> DataMax(string[][] data)
        {
            List<double> dataBuffer = new List<double>();
            List<string> dataMaxList = new List<string>();
            try
            {
                string trimming_id = "0";
                foreach (string[] d in data)
                {
                    if (d != data[0])
                    {
                        if (d[2] != trimming_id || d == data[data.Length - 1]) //trimming id switch
                        {
                            dataMaxList.Add(DataMax(dataBuffer).ToString());
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
            return dataMaxList;
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
        #endregion

    }
}
