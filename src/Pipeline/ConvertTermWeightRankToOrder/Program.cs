using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;

namespace ConvertTermWeightRankToOrder
{
    public class TermTag
    {
        public string strTerm;
        public double iRank;
        public string strOrder;
    }

    class Program
    {
        public static WordSeg.WordSeg wordseg;
        public static Tokens tokens;

        private static void InitializeWordBreaker(string strLexicalDictionary)
        {
            wordseg = new WordSeg.WordSeg();
            wordseg.LoadLexicalDict(strLexicalDictionary, true);
            tokens = wordseg.CreateTokens(1024);
        }

        public static List<TermTag> ParseRecord(string[] items)
        {
            List<TermTag> ttList = new List<TermTag>();
            for (int i = 2;i < items.Length;i++)
            {
                string item = items[i];
                int pos = item.LastIndexOf('[');
                string strTerm = item.Substring(0, pos).Trim();
                if (strTerm == "")
                {
                    return null;
                }

                string strTag = item.Substring(pos + 1, item.Length - pos - 2);
                TermTag tt = new TermTag();
                tt.strTerm = strTerm;
                tt.iRank = double.Parse(strTag);
                tt.strOrder = "";

                ttList.Add(tt);
            }

            return ttList;
        }

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("ConvertTermWeightRankToOrder.exe [Word Breaker Lexical Dictionary] [Min Query Frequency] [input file name] [output file name]");
                return;
            }

            InitializeWordBreaker(args[0]);
            int minFreq = int.Parse(args[1]);
            StreamReader sr = new StreamReader(args[2]);
            StreamWriter sw = new StreamWriter(args[3]);

            string strLine = "";
            while ((strLine = sr.ReadLine()) != null)
            {
                strLine = strLine.Trim();
                string[] items = strLine.Split('\t');
                int freq = int.Parse(items[1]);
                if (freq < minFreq)
                {
                    continue;
                }

                List<TermTag> ttList = ParseRecord(items);
                if (ttList == null)
                {
                    Console.WriteLine(strLine);
                    continue;
                }

                int changeCnt = 0;
                for (int i = 0; i < ttList.Count - 1; i++)
                {
                    if (ttList[i].iRank - ttList[i + 1].iRank > 0.01)
                    {
                        ttList[i].strOrder = ">";
                        changeCnt++;
                    }
                    else if (ttList[i + 1].iRank - ttList[i].iRank > 0.01)
                    {
                        ttList[i].strOrder = "<";
                        changeCnt++;
                    }
                    else
                    {
                        ttList[i].strOrder = "=";
                    }
                }
                ttList[ttList.Count - 1].strOrder = "E";

                if (changeCnt >= 2)
                {
                    string strOutput = "";
                    foreach (TermTag tt in ttList)
                    {
                        tokens.Clear();
                        wordseg.Segment(tt.strTerm, tokens, false);
                        for (int i = 0;i < tokens.tokenList.Count - 1;i++)
                        {
                            strOutput += tokens.tokenList[i].strTerm + "[=] ";
                        }
                        strOutput += tokens.tokenList[tokens.tokenList.Count - 1].strTerm + "[" + tt.strOrder + "] ";
                    }
                    sw.WriteLine(strOutput.Trim());
                }
            }
            sr.Close();
            sw.Close();
        }
    }
}
