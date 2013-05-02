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

    public class ScoreItem
    {
        public double score;
        public bool bThreshold;
        public double gap;
    }

    class Program
    {
        static WordSeg.WordSeg wordseg;
        static WordSeg.Tokens wbTokens;
        static int MAX_THRESHOLD_NUM = 2;
        const int SCORE_FACTOR = 20;
        static double MIN_WEIGHT_SCORE_GAP = 0.1;
        static SortedDictionary<int, int> gap2cnt = new SortedDictionary<int, int>();

        //Merge adjacent tokens with the sam weight score
        static List<Token> MergeTokenList(List<Token> tkList)
        {
            List<Token> rstList = new List<Token>();
            rstList.Add(tkList[0]);

            for (int i = 1; i < tkList.Count; i++)
            {
                if (tkList[i].fWeight == rstList[rstList.Count - 1].fWeight)
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

        static bool CheckScoreListQuality(List<ScoreItem> scoreList)
        {
            for (int i = 1; i < scoreList.Count; i++)
            {
                if (scoreList[i].bThreshold != scoreList[i - 1].bThreshold)
                {
                    double sumGap = Math.Abs(scoreList[i].gap + scoreList[i - 1].gap);
                    if (scoreList[i].gap / sumGap >= 0.1 && scoreList[i].gap / sumGap <= 0.9)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        //Calculate threshold list
        //scoreList : all weight score in a query, the scores in the list must be sorted from big to small
        //thresholdCnt : the number of thresholds
        //minGap : the minimum score gap between two adjacent scores
        static List<double> CalcThreshold(List<ScoreItem> scoreList, int thresholdCnt, double minGap)
        {
            //Calculate all score gaps
            SortedList<double, List<ScoreItem>> gap2scoreList = new SortedList<double, List<ScoreItem>>();
            double v = scoreList[0].score;
            scoreList[0].gap = 0.0;
            for (int i = 1; i < scoreList.Count; i++)
            {
                double gap = Math.Abs(v - scoreList[i].score);
                if (gap2scoreList.ContainsKey(gap) == false)
                {
                    gap2scoreList.Add(gap, new List<ScoreItem>());
                }
                gap2scoreList[gap].Add(scoreList[i]);
                scoreList[i].gap = gap;
                v = scoreList[i].score;

                int iGap = (int)(gap * SCORE_FACTOR);
                if (gap2cnt.ContainsKey(iGap) == false)
                {
                    gap2cnt.Add(iGap, 0);
                }
                gap2cnt[iGap]++;
            }

            //Found the top-thresholdCnt max gaps and save thresholds
            SortedList<double, bool> slist = new SortedList<double, bool>();
            bool bEnough = false;
            foreach (KeyValuePair<double, List<ScoreItem>> pair in gap2scoreList.Reverse())
            {
                if (pair.Key < minGap)
                {
                    break;
                }

                foreach (ScoreItem item in pair.Value)
                {
                    if (thresholdCnt <= 0)
                    {
                        bEnough = true;
                        break;
                    }
                    thresholdCnt--;
                    if (slist.ContainsKey(item.score) == false)
                    {
                        slist.Add(item.score, true);
                        item.bThreshold = true;
                    }
                }

                if (bEnough == true)
                {
                    break;
                }
            }

            //Sort the thresholds from bigger to smaller, and save them into the result list
            List<double> thresholdList = new List<double>();
            foreach (KeyValuePair<double, bool> pair in slist.Reverse())
            {
                thresholdList.Add(pair.Key);
            }

            return thresholdList;
        }

        //The scores distribution
        //For each scores range (from threshold[i] to threshold[i+1]), check the ratio between scores length and threshold length
        static bool CheckThreshold(List<ScoreItem> scoreList, List<double> thresholdList, double maxGapRate)
        {
            double topThreshold = 1.0;
            for (int i = 0; i < thresholdList.Count; i++)
            {
                double maxScore = 0.0;
                double minScore = 1.0;
                for (int j = 0; j < scoreList.Count; j++)
                {
                    if (scoreList[j].score <= topThreshold && scoreList[j].score > thresholdList[i])
                    {
                        if (maxScore < scoreList[j].score)
                        {
                            maxScore = scoreList[j].score;
                        }
                        if (minScore > scoreList[j].score)
                        {
                            minScore = scoreList[j].score;
                        }
                    }
                }

                if ((maxScore - minScore) / (topThreshold - thresholdList[i]) >= maxGapRate)
                {
                    return false;
                }

                topThreshold = thresholdList[i];
            }

            //last part is not necessary to test

            return true;
        }

        static void Main(string[] args)
        {
            if (args.Length != 6)
            {
                Console.WriteLine("BuildQueryTermWeightCorpus.txt [Min frequency in query] [Query Segment Labels] [Min Segment Gap] [Lexical dictionary file name] [Query term weight score file name] [Training corpus file name]");
                return;
            }

            int minFreq = int.Parse(args[0]);

            string[] labItems = args[1].Split(',');

            MAX_THRESHOLD_NUM = labItems.Length - 1;
            MIN_WEIGHT_SCORE_GAP = double.Parse(args[2]);

            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(args[3], true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);

            if (File.Exists(args[4]) == false)
            {
                Console.WriteLine("Query term weight file {0} is not existed.", args[4]);
                return;
            }

            StreamReader sr = new StreamReader(args[4]);
            StreamWriter sw = new StreamWriter(args[5]);

            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split('\t');
                string strRawQuery = items[0];
                int queryFreq = int.Parse(items[1]);

                //Ignore queries with less frequency
                if (int.Parse(items[1]) < minFreq)
                {
                    continue;
                }

                try
                {
                    //Get query features
                    SortedDictionary<double, int> sdict = new SortedDictionary<double, int>();
                    double maxWeight = -1.0;
                    double minWeight = 2.0;
                    int coreTerm = 0;
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
                        if (sdict.ContainsKey(fWeight) == false)
                        {
                            sdict.Add(fWeight, 0);
                        }
                        sdict[fWeight] += strTerm.Length;

                        if (fWeight >= maxWeight)
                        {
                            maxWeight = fWeight;
                        }
                        if (fWeight <= minWeight)
                        {
                            minWeight = fWeight;
                        }

                        if (fWeight == 1.0)
                        {
                            coreTerm++;
                        }
                    }

                    if (maxWeight < 1.0)
                    {
                        continue;
                    }

                    //Sort weight score list
                    List<ScoreItem> scoreList = new List<ScoreItem>();
                    foreach (KeyValuePair<double, int> pair in sdict.Reverse())
                    {
                        ScoreItem scoreItem = new ScoreItem();
                        scoreItem.score = pair.Key;
                        scoreItem.bThreshold = false;
                        scoreItem.gap = 0.0;

                        scoreList.Add(scoreItem);
                    }

                    //Find top-ThresholdNum threshold value
                    List<double> thresholdList = null;
                    thresholdList = CalcThreshold(scoreList, MAX_THRESHOLD_NUM, MIN_WEIGHT_SCORE_GAP);
                    if (thresholdList.Count != MAX_THRESHOLD_NUM)
                    {
                        continue;
                    }

                    if (CheckScoreListQuality(scoreList) == false)
                    {
                        continue;
                    }

                    //If distribution of score in each threshold is diversity, ignore the query
                    if (CheckThreshold(scoreList, thresholdList, 0.1) == false)
                    {
                        continue;
                    }

                    if (thresholdList.Count > 0)
                    {
                        int coreCnt = 0;
                        int otherCnt = 0;
                        //Check core term
                        foreach (KeyValuePair<double, int> pair in sdict)
                        {
                            if (pair.Key > thresholdList[0])
                            {
                                coreCnt += pair.Value;
                            }
                            else
                            {
                                otherCnt += pair.Value;
                            }
                        }
                        if (coreCnt < 2)
                        {
                            continue;
                        }
                    }


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


                        bool bProcessed = false;
                        //Label tags according term's weight and thresholds
                        for (int j = 0; j < thresholdList.Count; j++)
                        {
                            if (fWeight > thresholdList[j])
                            {
                                tk.strTag = labItems[j];
                                bProcessed = true;
                                break;
                            }
                        }

                        //Label the last part
                        if (bProcessed == false)
                        {
                            int j = thresholdList.Count;
                            tk.strTag = labItems[j];
                        }

                        tkList.Add(tk);
                    }

                    tkList = MergeTokenList(tkList);
                    tkList = ResegmentTokenList(tkList);
                    string strOutput = "";
                    foreach (Token tk in tkList)
                    {
                        string strTag = tk.strTag;
                        strOutput += tk.strTerm + "[" + strTag + "] ";
                    }
                    sw.WriteLine("{0}\t{1}\t{2}", strRawQuery, queryFreq, strOutput.Trim());
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
