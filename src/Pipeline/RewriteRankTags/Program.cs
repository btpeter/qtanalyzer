using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RewriteRankTags
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("RewriteRankTags [input file name] [output file name]");
                return;
            }

            StreamReader sr = new StreamReader(args[0]);
            StreamWriter sw = new StreamWriter(args[1]);

            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split();
                List<string> termList = new List<string>();
                List<string> tagList = new List<string>();
                int Rank0Cnt = 0, Rank1Cnt = 0, Rank2Cnt = 0;
                foreach (string item in items)
                {
                    int pos = item.LastIndexOf('[');
                    string strTerm = item.Substring(0, pos);
                    string strTag = item.Substring(pos + 1, item.Length - pos - 2);
                    termList.Add(strTerm);
                    tagList.Add(strTag);

                    if (strTag == "RANK_0")
                    {
                        Rank0Cnt++;
                    }
                    else if (strTag == "RANK_1")
                    {
                        Rank1Cnt++;
                    }
                    else if (strTag == "RANK_2")
                    {
                        Rank2Cnt++;
                    }
                }

                if (Rank2Cnt > Rank1Cnt + Rank0Cnt)
                {
                    continue;
                }

                if (Rank1Cnt == 0 && Rank2Cnt > Rank0Cnt)
                {
                    continue;
                }

                if (Rank2Cnt == 0 && Rank1Cnt > Rank0Cnt)
                {
                    continue;
                }

                if (Rank2Cnt == 0 && Rank1Cnt == 1)
                {
                    for (int i = 0; i < tagList.Count; i++)
                    {
                        if (tagList[i] == "RANK_1")
                        {
                            tagList[i] = "RANK_2";
                        }
                    }
                }

                string strRstLine = "";
                for (int i = 0; i < termList.Count; i++)
                {
                    strRstLine += termList[i] + "[" + tagList[i] + "] ";
                }
                sw.WriteLine(strRstLine.Trim());
            }

            sw.Close();
            sr.Close();
        }
    }
}
