using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ConvertRawTrainingCorpusToCRFSharp
{
    class RankIdSet
    {
        public int rank0;
        public int rank1;
        public int rank2;
        public int rank3;
        public int rank4;
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("ConvertRawTrainingCorpusToCRFSharp.exe [input filename] [output filename]");
                return;
            }

            StreamReader sr = new StreamReader(args[0]);
            StreamWriter sw = new StreamWriter(args[1]);

            Dictionary<string, int> tag2num = new Dictionary<string, int>();
            Dictionary<string, RankIdSet> term2rank = new Dictionary<string, RankIdSet>();

            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split();
                foreach (string item in items)
                {
                    int pos = item.IndexOf('[');
                    string strTerm = item.Substring(0, pos);
                    string strTag = item.Substring(pos + 1, item.Length - pos - 2);
                    sw.WriteLine("{0}\tS_{1}", strTerm, strTag);

                    if (tag2num.ContainsKey(strTag) == false)
                    {
                        tag2num.Add(strTag, 0);
                    }
                    tag2num[strTag]++;

                    if (term2rank.ContainsKey(strTerm) == false)
                    {
                        term2rank.Add(strTerm, new RankIdSet());
                    }
                    switch (strTag)
                    {
                        case "RANK_0":
                            term2rank[strTerm].rank0++;
                            break;
                        case "RANK_1":
                            term2rank[strTerm].rank1++;
                            break;
                        case "RANK_2":
                            term2rank[strTerm].rank2++;
                            break;
                        case "RANK_3":
                            term2rank[strTerm].rank3++;
                            break;
                        case "RANK_4":
                            term2rank[strTerm].rank4++;
                            break;
                    }
                }
                sw.WriteLine();
            }

            foreach (KeyValuePair<string, int> pair in tag2num)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

            sr.Close();
            sw.Close();

            sw = new StreamWriter(args[1] + ".rankNum");
            foreach (KeyValuePair<string, RankIdSet> pair in term2rank)
            {
                double totalCnt = pair.Value.rank0 + pair.Value.rank1 + pair.Value.rank2 + pair.Value.rank3 + pair.Value.rank4;
                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", pair.Key, (double)(pair.Value.rank0) / totalCnt * 100.0, 
                    (double)(pair.Value.rank1) / totalCnt * 100.0, 
                    (double)(pair.Value.rank2) / totalCnt * 100.0,
                    (double)(pair.Value.rank3) / totalCnt * 100.0,
                    (double)(pair.Value.rank4) / totalCnt * 100.0);
            }
            sw.Close();
        }
    }
}
