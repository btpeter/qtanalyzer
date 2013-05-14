using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RefineRank_0_Tag
{
    public class TermTag
    {
        public string strTerm;
        public string strTag;
    }

    class Program
    {
        public static Dictionary<string, string> query2tags = new Dictionary<string, string>();

        public static List<TermTag> ParseTermTagList(string str)
        {
            try
            {
                string[] items = str.Split();
                List<TermTag> rstList = new List<TermTag>();

                int pos = items[0].LastIndexOf('[');
                string strTerm = items[0].Substring(0, pos);
                string strTag = items[0].Substring(pos + 1, items[0].Length - pos - 2);

                string strCurrentTerm = strTerm;
                string strCurrentTag = strTag;
                for (int i = 1; i < items.Length; i++)
                {
                    pos = items[i].LastIndexOf('[');
                    strTerm = items[i].Substring(0, pos);
                    strTag = items[i].Substring(pos + 1, items[i].Length - pos - 2);

                    if (strTag != strCurrentTag)
                    {
                        TermTag tt = new TermTag();
                        tt.strTerm = strCurrentTerm;
                        tt.strTag = strCurrentTag;
                        rstList.Add(tt);
                        strCurrentTerm = "";
                    }

                    strCurrentTerm = strCurrentTerm + strTerm;
                    strCurrentTag = strTag;
                }

                if (strCurrentTag.Length > 0)
                {
                    TermTag tt = new TermTag();
                    tt.strTerm = strCurrentTerm;
                    tt.strTag = strCurrentTag;
                    rstList.Add(tt);
                }

                return rstList;
            }
            catch (System.Exception err)
            {
                return null;
            }
        }


        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("RefineRank0_Tag.exe [threshold score] [refine file name] [perfect match file name1] ... [perfeact match file nameN]");
                return;
            }

            StreamReader sr;
            string strLine = "";
            double threshold = double.Parse(args[0]);
            for (int i = 2; i < args.Length; i++)
            {
                LoadPerfectMatchRecord(args[i], threshold);
            }

            sr = new StreamReader(args[1]);
            StreamWriter sw = new StreamWriter(args[1] + ".RefineRankTag");
            strLine = "";
            while ((strLine = sr.ReadLine()) != null)
            {
                strLine = strLine.Trim();
                string[] items = strLine.Split('\t');
                string strTermScore = "";
                for (int j = 2; j < items.Length; j++)
                {
                    if (items[j].Trim().IndexOf('[') > 0)
                    {
                        strTermScore = strTermScore + items[j] + " ";
                    }
                }
                strTermScore = strTermScore.Trim();

                List<TermTag> ttList = ParseTermTagList(strTermScore);
                if (ttList == null)
                {
                    continue;
                }

                string strRst = "";
                foreach (TermTag tt in ttList)
                {
                    if (tt.strTerm.Length > 3 && tt.strTag == "1" && query2tags.ContainsKey(tt.strTerm) == true)
                    {
                        //Found the term is in lexical dictionary, rewrite it
                        strRst = strRst + query2tags[tt.strTerm] + "\t";
                    }
                    else
                    {
                        strRst = strRst + tt.strTerm + "[" + tt.strTag + "]\t";
                    }
                }

                strRst = strRst.Trim();
                strRst = items[0] + "\t" + items[1] + "\t" + strRst;
                sw.WriteLine(strRst);
            }
            sr.Close();
            sw.Close();
        }

        private static void LoadPerfectMatchRecord(string strFileName, double threshold)
        {
            StreamReader sr;
            sr = new StreamReader(strFileName);
            string strLine = "";
            while ((strLine = sr.ReadLine()) != null)
            {
                if (strLine.Contains("[1]") == true &&
                    strLine.Contains("[0.") == true)
                {
                    string[] items = strLine.Split('\t');
                    if (query2tags.ContainsKey(items[0]) == false)
                    {
                        string strTermScore = "";
                        double minScore = 2.0;
                        double maxScore = -1.0;
                        for (int j = 2; j < items.Length; j++)
                        {
                            int pos = items[j].Trim().LastIndexOf('[');
                            if (pos > 0)
                            {
                                strTermScore = strTermScore + items[j] + "\t";
                                double score = double.Parse(items[j].Trim().Substring(pos + 1, items[j].Trim().Length - pos - 2));
                                if (score > maxScore)
                                {
                                    maxScore = score;
                                }
                                if (score < minScore)
                                {
                                    minScore = score;
                                }
                            }
                        }
                        strTermScore = strTermScore.Trim();

                        if (maxScore - minScore >= threshold)
                        {
                            query2tags.Add(items[0], strTermScore);
                        }
                    }
                }
            }
            sr.Close();
        }
    }
}
