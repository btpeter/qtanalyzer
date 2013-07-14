using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using QueryTermWeightAnalyzer;


namespace RankingFeatureExtractor
{
    class Program
    {
        static public bool IsEnglishTerm(string strTerm)
        {
            foreach (char ch in strTerm)
            {
                if (ch < 'a' || ch > 'z')
                {
                    return false;
                }
            }

            return true;
        }

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("RankingFeatureExtractor.exe [configuration file name] [input file name] [output file name] [corpus size]");
                return;
            }

            QueryTermWeightAnalyzer.QueryTermWeightAnalyzer analyzer = new QueryTermWeightAnalyzer.QueryTermWeightAnalyzer();
            if (analyzer.Initialize(args[0]) == false)
            {
                Console.WriteLine("Initialize the analyzer failed.");
                return;
            }

            StreamReader sr = new StreamReader(args[1]);
            StreamWriter sw_train = new StreamWriter(args[2] + ".train");
            StreamWriter sw_test = new StreamWriter(args[2] + ".test");
            int maxSize = int.Parse(args[3]);

            //Write column header into file (include feature set name)
            sw_train.WriteLine("m:Rating\tm:QueryId\tTerm\tQuery\t" + analyzer.GetFeatureName());
            sw_test.WriteLine("m:Rating\tm:QueryId\tTerm\tQuery\t" + analyzer.GetFeatureName());

            //Write all active feature name into file
            string strAF = analyzer.GetFeatureName();
            string[] afitems = strAF.Split('\t');
            File.WriteAllLines("activefeatures.txt", afitems);

            HashSet<string> setLine = new HashSet<string>();
            int g_id = 10000;
            int cnt = 0;
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine().Trim();
                if (setLine.Contains(strLine) == true)
                {
                    continue;
                }
                setLine.Add(strLine);

                //Parse training corpus
                string[] items = strLine.Split();
                List<string> termList = new List<string>();
                List<string> tagList = new List<string>();
                StringBuilder sbQuery = new StringBuilder();
                foreach (string item in items)
                {
                    int pos = item.LastIndexOf('[');
                    string strTerm = item.Substring(0, pos);
                    string strTag = item.Substring(pos + 1, item.Length - pos - 2);

                    termList.Add(strTerm.ToLower());
                    tagList.Add(strTag);

                    sbQuery.Append(strTerm);
                }

                //Extract each term's features
                List<string> featureList = analyzer.ExtractFeature(termList);
                if (featureList == null || featureList.Count != termList.Count)
                {
                    //Failed to analyze term weight
                    Console.WriteLine("Failed to analyze {0}", strLine);
                    continue;
                }

                //Format: m:Rating\tm:QueryId\tTerm\tQuery\tFeatureSet
                for (int i = 0; i < featureList.Count; i++)
                {
                    //The [0, maxSize] queries are for training corpus
                    //The [maxSize + 1, maxSize * 2] queries are for test corpus
                    if (cnt <= maxSize)
                    {
                        sw_train.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", tagList[i], g_id, termList[i], sbQuery.ToString().Trim(), featureList[i]);
                    }
                    else
                    {
                        sw_test.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", tagList[i], g_id, termList[i], sbQuery.ToString().Trim(), featureList[i]);
                    }
                }
                System.Threading.Interlocked.Increment(ref g_id);

                cnt++;
                if (cnt > maxSize * 2)
                {
                    break;
                }

            }
            sr.Close();
            sw_train.Close();
            sw_test.Close();
        }
    }
}
