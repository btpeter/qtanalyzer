using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MergeTrainCorpus
{
    class Program
    {
        static string[] MergeTokens(string[] tokens)
        {
            List<string> rstList = new List<string>();
            int pos = tokens[0].IndexOf('[');
            string strToken = tokens[0].Substring(0, pos);
            string strTag = tokens[0].Substring(pos);
            for (int i = 1; i < tokens.Length; i++)
            {
                pos = tokens[i].IndexOf('[');
                string strCurrToken = tokens[i].Substring(0, pos);
                string strCurrTag = tokens[i].Substring(pos);

                if (strCurrTag == strTag)
                {
                    strToken = strToken + strCurrToken;
                }
                else
                {
                    rstList.Add(strToken + strTag);
                    strToken = strCurrToken;
                    strTag = strCurrTag;
                }

            }

            rstList.Add(strToken + strTag);

            return rstList.ToArray();
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("MergeTrainCorpus [Merged train corpus file name] [input file 1] [input file 2] ... [input file N]");
                return;
            }
            Dictionary<string, string> dict = new Dictionary<string, string>();

            StreamWriter sw = new StreamWriter(args[0]);
            for (int i = 1; i < args.Length; i++)
            {
                StreamReader sr = new StreamReader(args[i]);
                int totalLine = 0;
                int dropLine = 0;
                while (sr.EndOfStream == false)
                {
                    string strLine = sr.ReadLine();
                    string[] items = strLine.Split('\t');
                    if (dict.ContainsKey(items[0]) == false)
                    {
                        string[] tokens = items[2].Split(' ');
                        tokens = MergeTokens(tokens);

                        int redTermCnt = 0;
                        totalLine++;
                        //foreach (string token in tokens)
                        //{
                        //    if (term2freq.ContainsKey(token) == true && term2freq[token] > 5)
                        //    {
                        //        redTermCnt++;
                        //    }
                        //}

                        if (redTermCnt == tokens.Length)
                        {
                            dropLine++;
                            dict.Add(items[0], null);
                        }
                        else
                        {
                            dict.Add(items[0], items[2]);
                        }
                    }
                }
                sr.Close();

                Console.WriteLine("Process {0} is done. Total Line: {1}, Drop Line: {2}", args[i], totalLine, dropLine);
            }

            foreach (KeyValuePair<string, string> pair in dict)
            {
                if (pair.Value != null)
                {
                    //string[] tokens = pair.Value.Split(' ');
                    //tokens = MergeTokens(tokens);
                    sw.WriteLine(pair.Value);
                }
            }
            sw.Close();
        }
    }
}
