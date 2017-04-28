// This code requires the Nuget package Microsoft.AspNet.WebApi.Client to be installed.
// Instructions for doing this in Visual Studio:
// Tools -> Nuget Package Manager -> Package Manager Console
// Install-Package Microsoft.AspNet.WebApi.Client

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;


namespace RegressorCompare
{

    public class Rootobject
    {
        public Results Results { get; set; }
    }

    public class Results
    {
        public In_Crossvalidatecompare_Summary In_CrossValidateCompare_Summary { get; set; }
    }

    public class In_Crossvalidatecompare_Summary
    {
        public string type { get; set; }
        public Value value { get; set; }
    }

    public class Value
    {
        public string[] ColumnNames { get; set; }
        public string[] ColumnTypes { get; set; }
        public string[][] Values { get; set; }
    }


    public class StringTable
    {
        public string[] ColumnNames { get; set; }
        public string[,] Values { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Try Calling Web Service . . .");

            string[] AssesmentCode = new string[6] { "U2A", "U3B", "U4C", "U5B", "U7B", "U8C" };
            int iterationNum = 35;


            foreach (string code in AssesmentCode)
            {
                for (int i = 0; i < iterationNum; i++)
                {
                    InvokeRequestResponseService(code, i).Wait(); //hack, check random seed working or not
                }
            }
        }

        static async Task InvokeRequestResponseService(string assessmentCode, int iterationIndex)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    GlobalParameters = new Dictionary<string, string>()
                        {
                            { "Data source URL", "http://jatsono.azurewebsites.net/Auros/"+assessmentCode+".csv" },
                            //{ "Random seed", "98728371" },
                            { "Random seed", GenerateRandomSeed(iterationIndex).ToString() },
                        }
                };
                const string apiKey = "r/th/CdafYdPOxR/AtE+sDC+v1DfVHmmjRpQp+qpsY5u2paGFVKsT4rK6ABqbNo3Xjc+j/fEsROw8jSQUXlB9w=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress = new Uri("https://asiasoutheast.services.azureml.net/workspaces/d79f5f333ebe4fa8b76f1ae9162d74b5/services/1932502da08649c0beea61d9913c7fc6/execute?api-version=2.0&details=true");

                // WARNING: The 'await' statement below can result in a deadlock if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false) so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)


                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    Rootobject cvsSummary = new JavaScriptSerializer().Deserialize<Rootobject>(result);
                    //TODO save the result
                    StringBuilder chunkBuilder = new StringBuilder();
                    string fileName = "Result/" + assessmentCode + "_summary.csv";
                    if (!Directory.Exists("Result/"))
                        Directory.CreateDirectory("Result/");
                    if (!File.Exists(fileName))
                    {
                        chunkBuilder.AppendLine(commaDelimiting(cvsSummary.Results.In_CrossValidateCompare_Summary.value.ColumnNames) + ",IterationIndex");
                    }
                    foreach (string[] valueArray in cvsSummary.Results.In_CrossValidateCompare_Summary.value.Values)
                    {
                        string valueLine = commaDelimiting(valueArray) + "," + iterationIndex.ToString();
                        chunkBuilder.AppendLine(valueLine);
                    }

                    if (!File.Exists(fileName))
                    {
                        File.WriteAllText(fileName, chunkBuilder.ToString());
                    }
                    else
                    {
                        File.AppendAllText(fileName, chunkBuilder.ToString());
                    }

                    Console.WriteLine("[Success]Saving regressor compare summary assessment" + assessmentCode + " iteration " + iterationIndex.ToString());

                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                }
            }
        }

        public static int GenerateRandomSeed(int grandRandom)
        {
            Random rand = new Random(grandRandom);
            int r = rand.Next(11, 99999999);
            return r;
        }

        public static string commaDelimiting(string[] rawString)
        {
            bool firstIndex = true;
            string commaDelimited = string.Empty;
            foreach (string s in rawString)
            {
                if (!firstIndex)
                {
                    commaDelimited += ",";
                }
                commaDelimited += s;
                firstIndex = false;
            }
            return commaDelimited;
        }
    }
}
