using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TuningTermWeightThreshold
{
    class Program
    {
        const int THRESHOLD_NUM = 2;
        const int SCORE_FACTOR = 20;
        const int WEIGHT_LEVEL_NUM = THRESHOLD_NUM + 1;

        static SortedDictionary<int, int> gap2cnt = new SortedDictionary<int, int>();

        //The scores distribution
        static bool CheckThreshold(List<double> scoreList, List<double> thresholdList)
        {
            double topThreshold = 1.0;
            for (int i = 0; i < thresholdList.Count; i++)
            {
                double maxScore = 0.0;
                double minScore = 1.0;
                for (int j = 0; j < scoreList.Count; j++)
                {
                    if (scoreList[j] <= topThreshold && scoreList[j] > thresholdList[i])
                    {
                        if (maxScore < scoreList[j])
                        {
                            maxScore = scoreList[j];
                        }
                        if (minScore > scoreList[j])
                        {
                            minScore = scoreList[j];
                        }
                    }
                }

                if (maxScore - minScore >= 0.15)
                {
                    return false;
                }

                topThreshold = thresholdList[i];
            }

            //last part is not necessary to test

            return true;
        }

        //Calculate threshold list
        //scoreList : all weight score in a query, the scores in the list must be sorted from big to small
        //thresholdCnt : the number of thresholds
        //minGap : the minimum score gap between two adjacent scores
        static List<double> CalcThreshold(List<double> scoreList, int thresholdCnt, double minGap)
        {
            //Calculate all score gaps
            SortedList<double, List<double>> gap2scoreList = new SortedList<double, List<double>>();
            double v = scoreList[0];
            for (int i = 1; i < scoreList.Count; i++)
            {
                double gap = Math.Abs(v - scoreList[i]);
                if (gap2scoreList.ContainsKey(gap) == false)
                {
                    gap2scoreList.Add(gap, new List<double>());
                }
                gap2scoreList[gap].Add(scoreList[i]);
                v = scoreList[i];

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
            foreach (KeyValuePair<double, List<double>> pair in gap2scoreList.Reverse())
            {
                if (pair.Key < minGap)
                {
                    break;
                }

                foreach (double item in pair.Value)
                {
                    if (thresholdCnt <= 0)
                    {
                        bEnough = true;
                        break;
                    }
                    thresholdCnt--;
                    if (slist.ContainsKey(item) == false)
                    {
                        slist.Add(item, true);
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

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("TuningTermWeightThreshold [Min frequency in query] [Query term weight score file name]");
                return;
            }

            int minFreq = int.Parse(args[0]);
            StreamReader sr = new StreamReader(args[1]);

            double[] arrSumRankWeight;
            int[] arrRankFreq;
            SortedDictionary<int, int>[] arrRank2Freq;

            arrSumRankWeight = new double[WEIGHT_LEVEL_NUM];
            arrRankFreq = new int[WEIGHT_LEVEL_NUM];
            arrRank2Freq = new SortedDictionary<int, int>[WEIGHT_LEVEL_NUM];
            for (int i = 0; i < arrRank2Freq.Length; i++)
            {
                arrRank2Freq[i] = new SortedDictionary<int, int>();
            }

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

                    //Sort weight score list
                    List<double> scoreList = new List<double>();
                    foreach (KeyValuePair<double, bool> pair in sdict.Reverse())
                    {
                        scoreList.Add(pair.Key);
                    }

                    //Find top-ThresholdNum threshold value
                    List<double> thresholdList;
                    thresholdList = CalcThreshold(scoreList, THRESHOLD_NUM, 0.10);

                    //If distribution of score in each threshold is diversity, ignore the query
                    //if (CheckThreshold(scoreList, thresholdList) == false)
                    //{
                    //    continue;
                    //}

                    if (thresholdList.Count != THRESHOLD_NUM)
                    {
                        continue;
                    }

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

                        bool bProcessed = false;
                        //Label tags according term's weight and thresholds
                        for (int j = 0; j < thresholdList.Count; j++)
                        {
                            if (fWeight > thresholdList[j])
                            {
                                bProcessed = true;
                                arrSumRankWeight[j] += fWeight;
                                arrRankFreq[j]++;

                                int iWeight = (int)(fWeight * SCORE_FACTOR);
                                if (arrRank2Freq[j].ContainsKey(iWeight) == false)
                                {
                                    arrRank2Freq[j].Add(iWeight, 0);
                                }
                                arrRank2Freq[j][iWeight]++;

                                break;
                            }
                        }

                        //Label the last part
                        if (bProcessed == false)
                        {
                            int j = thresholdList.Count;
                            arrSumRankWeight[j] += fWeight;
                            arrRankFreq[j]++;

                            int iWeight = (int)(fWeight * SCORE_FACTOR);
                            if (arrRank2Freq[j].ContainsKey(iWeight) == false)
                            {
                                arrRank2Freq[j].Add(iWeight, 0);
                            }
                            arrRank2Freq[j][iWeight]++;
                        }
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

            Console.WriteLine("Gap distribution list:");
            foreach (KeyValuePair<int, int> pair in gap2cnt)
            {
                Console.WriteLine("Gap {0} : {1}", (double)pair.Key / SCORE_FACTOR, pair.Value);
            }

            Console.WriteLine("Average Ranks weight list :");
            for (int i = 0; i < arrRankFreq.Length; i++)
            {
                Console.WriteLine("Average Rank {0} weight : {1} (Sum freq: {2})", i, arrSumRankWeight[i] / arrRankFreq[i], arrRankFreq[i]); 
            }

            for (int i = 0; i < arrRank2Freq.Length - 1; i++)
            {
                Console.WriteLine();
                Console.WriteLine("Compare with Rank {0} and Rank {1}", i, i + 1);
                int sumFreq1 = 0, sumFreq2 = 0;

                for (int iWeight = 0; iWeight <= SCORE_FACTOR; iWeight++)
                {
                    double rawScore = (double)iWeight / SCORE_FACTOR;
                    int freq1 = 0, freq2 = 0;
                    if (arrRank2Freq[i].ContainsKey(iWeight) == true)
                    {
                        freq1 = arrRank2Freq[i][iWeight];
                    }
                    if (arrRank2Freq[i + 1].ContainsKey(iWeight) == true)
                    {
                        freq2 = arrRank2Freq[i + 1][iWeight];
                    }
                    sumFreq1 += freq1;
                    sumFreq2 += freq2;

                    Console.WriteLine("{0}~{1}\tRank {2}:{4}({6}%)\tRank {3}:{5}({7}%)\t{8}", rawScore, rawScore + 1.0 / SCORE_FACTOR, 
                        i, i + 1, sumFreq1, sumFreq2,
                        ((double)sumFreq1 / (double)arrRankFreq[i] * 100.0).ToString("0.##"), 
                        ((double)sumFreq2 / (double)arrRankFreq[i + 1] * 100.0).ToString("0.##"), 
                        ((double)(sumFreq1) / (double)(sumFreq2 + 1)).ToString("0.##"));

                }
            }
        }
    }
}
