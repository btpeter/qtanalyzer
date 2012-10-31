using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BuildQueryTermWeightCorpus
{
    public class Token
    {
        public string strTerm;
        public string strTag;
        public double fWeight;
    }

    class Program
    {
        static WordSeg.WordSeg wordseg;
        static WordSeg.Tokens wbTokens;

        //Merge adjacent tokens with the sam weight score
        static List<Token> MergeTokenList(List<Token> tkList)
        {
            List<Token> rstList = new List<Token>();
            rstList.Add(tkList[0]);

            for (int i = 1; i < tkList.Count; i++)
            {
                if (tkList[i].strTag == rstList[rstList.Count - 1].strTag)
                {
                    rstList[rstList.Count - 1].strTerm += tkList[i].strTerm;
                }
                else
                {
                    rstList.Add(tkList[i]);
                }
            }

            return rstList;
        }

        //According word breaker's grain to re-segment tokens
        static List<Token> ResegmentTokenList(List<Token> tkList)
        {
            List<Token> rstList = new List<Token>();
            foreach (Token item in tkList)
            {
                wordseg.Segment(item.strTerm, wbTokens, false);
                foreach (WordSeg.Token token in wbTokens.tokenList)
                {
                    Token tk = new Token();
                    tk.strTerm = token.strTerm;
                    tk.strTag = item.strTag;
                    tk.fWeight = item.fWeight;
                    rstList.Add(tk);
                }
            }
            return rstList;
        }

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("BuildQueryTermWeightCorpus.txt [Min frequency in query] [Lexical dictionary file name] [Query term weight score file name] [Training corpus file name]");
                return;
            }

            int minFreq = int.Parse(args[0]);

            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(args[1], true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);

            StreamReader sr = new StreamReader(args[2]);
            StreamWriter sw = new StreamWriter(args[3]);

            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split('\t');
                int queryFreq = int.Parse(items[1]);

                //Ignore queries with less frequency
                if (int.Parse(items[1]) < minFreq)
                {
                    continue;
                }

                try
                {
                    //Get query features
                    SortedDictionary<double, bool> sdict = new SortedDictionary<double, bool>();
                    HashSet<string> setTerm = new HashSet<string>();
                    bool dupTerm = false;
                    double maxWeight = -1.0;
                    double minWeight = 2.0;
                    int core_cnt = 0;
                    for (int i = 2; i < items.Length; i++)
                    {
                        if (items[i].Trim().Length == 0)
                        {
                            continue;
                        }

                        int pos = items[i].IndexOf('[');
                        string strTerm = items[i].Substring(0, pos).Trim().ToLower();
                        if (strTerm.Length == 0)
                        {
                            continue;
                        }
                        if (setTerm.Contains(strTerm) == true)
                        {
                            dupTerm = true;
                            break;
                        }
                        setTerm.Add(strTerm);

                        string strWeight = items[i].Substring(pos + 1, items[i].Length - (pos + 1) - 1);
                        double fWeight = double.Parse(strWeight);
                        if (sdict.ContainsKey(fWeight) == false)
                        {
                            sdict.Add(fWeight, true);
                        }
                        if (fWeight >= maxWeight)
                        {
                            maxWeight = fWeight;
                        }
                        if (fWeight <= minWeight)
                        {
                            minWeight = fWeight;
                        }

                        if (fWeight >= 0.98)
                        {
                            core_cnt++;
                        }
                    }

                    //If query only contains single core term OR max weight is less than 1.0 OR
                    //the query contains duplicated terms, the query will be ignored.
                    if (core_cnt < 2 || maxWeight < 1.0 || dupTerm == true)
                    {
                        continue;
                    }

                    bool bIgnoreQuery = false;
                    bool bOnlyRank0 = true;
                    List<Token> tkList = new List<Token>();
                    for (int i = 2; i < items.Length; i++)
                    {
                        if (items[i].Trim().Length == 0)
                        {
                            continue;
                        }

                        int pos = items[i].IndexOf('[');
                        string strTerm = items[i].Substring(0, pos).Trim().ToLower();
                        if (strTerm.Length == 0)
                        {
                            continue;
                        }

                        string strWeight = items[i].Substring(pos + 1, items[i].Length - (pos + 1) - 1);
                        double fWeight = double.Parse(strWeight);

                        Token tk = new Token();
                        tk.fWeight = fWeight;
                        tk.strTerm = strTerm;

                        //the thresholds below is generated by TuningTermWeightThreshold
                        if (fWeight == 1.0)
                        {
                            tk.strTag = "RANK_0";
                        }
                        else if (fWeight < 0.90 && fWeight >= 0.7)
                        {
                            tk.strTag = "RANK_1";
                            bOnlyRank0 = false;
                        }
                        else if (fWeight <= 0.35)
                        {
                            tk.strTag = "RANK_2";
                            bOnlyRank0 = false;
                        }
                        else
                        {
                            bIgnoreQuery = true;
                            break;
                        }

                        tkList.Add(tk);
                    }

                    if (bOnlyRank0 == true && queryFreq < 1000)
                    {
                        bIgnoreQuery = true;
                    }

                    if (bIgnoreQuery == true)
                    {
                        continue;
                    }


                    tkList = MergeTokenList(tkList);
                    tkList = ResegmentTokenList(tkList);
                    string strOutput = "";
                    foreach (Token tk in tkList)
                    {
                        string strTag = tk.strTag;
                        strOutput += tk.strTerm + "[" + strTag + "] ";
                    }

                    //duplicate some query whose frequency is high
                    int logQueryFreq = (int)Math.Log10(queryFreq);
                    if (logQueryFreq == 0)
                    {
                        logQueryFreq++;
                    }
                    for (int i = 0; i < logQueryFreq; i++)
                    {
                        sw.WriteLine(strOutput.Trim());
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("Invalidated sentence: {0}", strLine);
                    Console.WriteLine("Message: {0}", err.Message);
                    Console.WriteLine("Call stack: {0}", err.StackTrace);
                }
            }

            sr.Close();
            sw.Close();

  
        }
    }
}
