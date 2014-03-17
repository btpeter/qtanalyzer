using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MergeTrainCorpus
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("MergeTrainCorpus [IsWeight] [Merged train corpus file name] [input file 1] [input file 2] ... [input file N]");
                return;
            }

            HashSet<string> setQuery = new HashSet<string>();
            List<string> corpusList = new List<string>();

            //Merge multi-train corpus into one corpus.
            //If many train corpus contain the same query with different label result, the first one will be saved,
            //and others will be dropped.
            bool bWeight = false;
            try
            {
                bWeight = bool.Parse(args[0]);
            }
            catch (Exception err)
            {
                Console.WriteLine("FAILED: Invalidated option for weight");
                return;
            }

            StreamWriter sw = new StreamWriter(args[1]);
            for (int i = 2; i < args.Length; i++)
            {
                StreamReader sr = new StreamReader(args[i]);
                while (sr.EndOfStream == false)
                {
                    string strLine = sr.ReadLine();
                    string[] items = strLine.Split('\t');
                    items[0] = items[0].Trim();

                    int freq = int.Parse(items[1]);
                    if (setQuery.Contains(items[0]) == false)
                    {
                        setQuery.Add(items[0]);

                        if (bWeight == true)
                        {
                            int freqLog = (int)Math.Log((double)freq);
                            if (freqLog == 0)
                            {
                                freqLog++;
                            }
                            for (int j = 0; j < freqLog; j++)
                            {
                                sw.WriteLine(items[2]);
                            }
                        }
                        else
                        {
                            sw.WriteLine(items[2]);
                        }
                    }
                }
                sr.Close();
            }
            sw.Close();
        }
    }
}
