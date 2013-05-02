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
            if (args.Length < 2)
            {
                Console.WriteLine("MergeTrainCorpus [Merged train corpus file name] [input file 1] [input file 2] ... [input file N]");
                return;
            }

            HashSet<string> setQuery = new HashSet<string>();
            List<string> corpusList = new List<string>();

            //Merge multi-train corpus into one corpus.
            //If many train corpus contain the same query with different label result, the first one will be saved,
            //and others will be dropped.
            StreamWriter sw = new StreamWriter(args[0]);
            for (int i = 1; i < args.Length; i++)
            {
                StreamReader sr = new StreamReader(args[i]);
                while (sr.EndOfStream == false)
                {
                    string strLine = sr.ReadLine();
                    string[] items = strLine.Split('\t');

                    items[0] = items[0].Trim();
                    if (items[0].EndsWith("英文") == true ||
                        items[0].EndsWith("英语怎么说") == true ||
                        items[0].EndsWith("英语怎么读") == true ||
                        items[0].EndsWith("英语是什么") == true ||
                        items[0].EndsWith("英文怎么说") == true ||
                        items[0].EndsWith("英文是什么") == true ||
                        items[0].EndsWith("英文怎么读") == true ||
                        items[0].EndsWith("英文怎么写") == true ||
                        items[0].EndsWith("英语") == true ||
                        items[0].EndsWith("英文翻译") == true ||
                        items[0].EndsWith("英文单词") == true ||
                        items[0].EndsWith("英语翻译") == true ||
                        items[0].EndsWith("英语单词") == true ||
                        items[0].EndsWith("翻译英文") == true ||
                        items[0].EndsWith("翻译英语") == true ||
                        items[0].EndsWith("怎么翻译") == true ||
                        items[0].EndsWith("怎样翻译") == true ||
                        items[0].EndsWith("如何翻译") == true)
                    {
                        continue;
                    }

                    int freq = int.Parse(items[1]);
                    if (setQuery.Contains(items[0]) == false)
                    {
                        setQuery.Add(items[0]);

                        //int freqLog = (int)Math.Log((double)freq);
                        //if (freqLog == 0)
                        //{
                        //    freqLog++;
                        //}
                        //for (int j = 0; j < freqLog; j++)
                        //{
                            sw.WriteLine(items[2]);
                        //}
                    }
                }
                sr.Close();
            }
            sw.Close();
        }
    }
}
