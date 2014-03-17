using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;
using WordSeg;

namespace GenerateTermSynPattern
{
    public class PatternItem
    {
        public string strPattern;
        public string strKeyTerm;
        public int freq;
    }

    public class QueryItem
    {
        public string strQuery;
        public int freq;
        public double clickweight;
    }

    class Program
    {
        static BigDictionary<string, Dictionary<string, int>> twoTerms2Pattern2Freq = new BigDictionary<string, Dictionary<string, int>>();
        static long totalSampleCnt = 0;
        static BigDictionary<string, int> keyTerm2Freq = new BigDictionary<string, int>();
        static BigDictionary<string, int> pattern2Freq = new BigDictionary<string, int>();
        static WordSeg.WordSeg wordseg = null;
        static WordSeg.Tokens tokens = null;
        static string strSeparator = "[X]";

        //Generate (pattern, term) list from a query
        public static List<PatternItem> GeneratePattern(string strQuery, int freq)
        {
            wordseg.Segment(strQuery, tokens, false);

            List<PatternItem> patItemList = new List<PatternItem>();
            if (tokens.tokenList.Count <= 1)
            {
                return patItemList;
            }

            //Generate each token's context as candidated patterns
            for (int i = 0; i < tokens.tokenList.Count; i++)
            {
                //Generate pattern for unigram
                int begin = tokens.tokenList[i].offset;
                int len = tokens.tokenList[i].len;
                if (len == 0)
                {
                    continue;
                }

                //Extract pattern and keyword from the query
                PatternItem patItem = new PatternItem();
                patItem.strKeyTerm = strQuery.Substring(begin, len);
                patItem.strPattern = strQuery.Substring(0, begin) + strSeparator + strQuery.Substring(begin + len);
                patItem.freq = freq;
                patItemList.Add(patItem);
            }

            return patItemList;
        }

        //Statistics synonmy information from a query cluster
        public static void StatPatternList(List<QueryItem> qiList)
        {
            Dictionary<string, SortedDictionary<string, int>> pattern2key2freq = new Dictionary<string,SortedDictionary<string,int>>();
            foreach (QueryItem item in qiList)
            {
                List<PatternItem> patItemList = GeneratePattern(item.strQuery, item.freq);
                foreach (PatternItem pattern in patItemList)
                {
                    if (pattern2key2freq.ContainsKey(pattern.strPattern) == false)
                    {
                        pattern2key2freq.Add(pattern.strPattern, new SortedDictionary<string, int>());
                    }
                    if (pattern2key2freq[pattern.strPattern].ContainsKey(pattern.strKeyTerm) == false)
                    {
                        pattern2key2freq[pattern.strPattern].Add(pattern.strKeyTerm, pattern.freq);
                    }
                    else
                    {
                        pattern2key2freq[pattern.strPattern][pattern.strKeyTerm] += pattern.freq;
                    }
                }
            }

            foreach (KeyValuePair<string, SortedDictionary<string, int>> pair in pattern2key2freq)
            {
                //Only consider the pattern who has more than one key term
                if (pair.Value.Count <= 1)
                {
                    continue;
                }
                //The key term in below list is ordered.
                List<string> keyTermList = new List<string>();
                foreach (KeyValuePair<string, int> subpair in pair.Value)
                {
                    keyTermList.Add(subpair.Key);
                }

                //Counting pattern frequency
                if (pattern2Freq.ContainsKey(pair.Key) == false)
                {
                    pattern2Freq.Add(pair.Key, keyTermList.Count * (keyTermList.Count - 1) / 2);
                }
                else
                {
                    pattern2Freq[pair.Key] += (keyTermList.Count * (keyTermList.Count - 1) / 2);
                }

                for (int i = 0; i < keyTermList.Count; i++)
                {
                    if (keyTerm2Freq.ContainsKey(keyTermList[i]) == false)
                    {
                        keyTerm2Freq.Add(keyTermList[i], keyTermList.Count - 1);
                    }
                    else
                    {
                        keyTerm2Freq[keyTermList[i]] += keyTermList.Count - 1;
                    }

                    for (int j = i + 1; j < keyTermList.Count; j++)
                    {
                        string strKeySynTerm = keyTermList[i] + "\t" + keyTermList[j];
                        if (twoTerms2Pattern2Freq.ContainsKey(strKeySynTerm) == false)
                        {
                            twoTerms2Pattern2Freq.Add(strKeySynTerm, new Dictionary<string, int>());
                        }
                        if (twoTerms2Pattern2Freq[strKeySynTerm].ContainsKey(pair.Key) == false)
                        {
                            twoTerms2Pattern2Freq[strKeySynTerm].Add(pair.Key, 1);
                        }
                        else
                        {
                            twoTerms2Pattern2Freq[strKeySynTerm][pair.Key]++;
                        }

                        totalSampleCnt++;
                    }
                }
            }
        }


        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("GenerateTermSynPattern.exe [input:word breaker lexical file] [input:query_clusterId_freq_clickweight file] [output:term_syn_pattern file]");
                return;
            }

            wordseg = new WordSeg.WordSeg();
            wordseg.LoadLexicalDict(args[0], true);
            //Create tokens which is local-thread structure
            //The max word segment legnth is MAX_SEGMENT_LENGTH
            tokens = wordseg.CreateTokens();


            StreamReader sr = new StreamReader(args[1]);
            List<QueryItem> qiList = new List<QueryItem>();
            string strLastUrl = "";
            long recordCnt = 0;
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                recordCnt++;

                string[] items = strLine.Split('\t');
                string strClusterId = items[1];
                QueryItem qi = new QueryItem();

                qi.strQuery = items[0];
               // qi.strQuery = JPNUtils.ToHalfKana(qi.strQuery);
                qi.strQuery = JPNUtils.ToDBC(qi.strQuery);
                qi.strQuery = qi.strQuery.Replace(" ", "");
                qi.strQuery = qi.strQuery.ToLower();
                qi.freq = int.Parse(items[2]);
                qi.clickweight = double.Parse(items[3]);

                ////Query url whose frequency or clickweight is too low will be ignored.
                //if (qi.clickweight < 5.0)
                //{
                //    continue;
                //}

                if (strLastUrl == "")
                {
                    qiList.Add(qi);
                }
                else
                {
                    if (strLastUrl == strClusterId)
                    {
                        qiList.Add(qi);
                    }
                    else
                    {
                        //If too many unique queries click the same url,
                        //The url may be low quaility, since it has too many
                        //different meaning, so ignore this cluster
                        if (qiList.Count < 100)
                        {
                            StatPatternList(qiList);
                        }
                        qiList.Clear();
                        qiList.Add(qi);
                    }
                }
                strLastUrl = strClusterId;

                if (recordCnt % 10000000 == 0)
                {
                    Console.WriteLine("Process {0} records...", recordCnt);
                    UpdateSaveTermSyn(args[2]);
                }
            }

            UpdateSaveTermSyn(args[2]);

            sr.Close();
        }

        //Check two terms LLR
        public static double LLR(string strTerm1, string strTerm2)
        {
            if (strTerm1.CompareTo(strTerm2) > 0)
            {
                string strTmp = strTerm1;
                strTerm1 = strTerm2;
                strTerm2 = strTmp;
            }

            string strKey = strTerm1 + "\t" + strTerm2;
            if (twoTerms2Pattern2Freq.ContainsKey(strKey) == false)
            {
                return 0.0;
            }

            double a = 0;
            foreach (KeyValuePair<string, int> pair in twoTerms2Pattern2Freq[strKey])
            {
                a += pair.Value;
            }

            double b = keyTerm2Freq[strTerm1] - a;
            double c = keyTerm2Freq[strTerm2] - a;
            double d = totalSampleCnt - (a + b + c);
            double N = totalSampleCnt;

            double llr = 2.0 * (a * Math.Log((a * N) / ((a + b) * (a + c))) +
                                b * Math.Log((b * N) / ((a + b) * (b + d))) +
                                c * Math.Log((c * N) / ((c + d) * (a + c))) +
                                d * Math.Log((d * N) / ((c + d) * (b + d))));

            return llr;
        }




        public static void UpdateSaveTermSyn(string strFileName)
        {
            StreamWriter sw = new StreamWriter(strFileName);
            SortedDictionary<double, List<string>> sortedResult = new SortedDictionary<double, List<string>>();
            foreach (KeyValuePair<string, Dictionary<string, int>> pair in twoTerms2Pattern2Freq)
            {
                if (pair.Value.Count > 1)
                {
                    //Calculate two terms LLR
                    string[] items = pair.Key.Split('\t');
                    double llr = LLR(items[0], items[1]);
                    if (llr < 100.0)
                    {
                        continue;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(pair.Key);
                    sb.Append("\t");
                    sb.Append(llr);
                    sb.Append("\t");

                    int ctxCnt = pair.Value.Count;

                    sb.Append(ctxCnt);
                    sb.Append("\t");

                    //ranking contexts for each term syn pair
                    int twoTermFreq = 0;
                    foreach (KeyValuePair<string, int> subpair in pair.Value)
                    {
                        twoTermFreq += subpair.Value;
                    }

                    SortedDictionary<double, List<string>> sPatternDict = new SortedDictionary<double, List<string>>();
                    int cntLowPatternLLR = 0;
                    int totalPatternLLR = 0;
                    foreach (KeyValuePair<string, int> subpair in pair.Value)
                    {
                        double N = totalSampleCnt;
                        double a = subpair.Value;
                        double b = twoTermFreq - a;
                        double c = pattern2Freq[subpair.Key] - a;
                        double d = N - (a + b + c);


                        if (N == 0 ||
                            a == 0 ||
                            b == 0 ||
                            c == 0 ||
                            d == 0 ||
                            a + b == 0 ||
                            a + c == 0 ||
                            b + d == 0 ||
                            c + d == 0
                            )
                        {
                            continue;
                        }

                        double llr2 = 2.0 * (a * Math.Log((a * N) / ((a + b) * (a + c))) +
                        b * Math.Log((b * N) / ((a + b) * (b + d))) +
                        c * Math.Log((c * N) / ((c + d) * (a + c))) +
                        d * Math.Log((d * N) / ((c + d) * (b + d))));

                        totalPatternLLR++;

                        if (llr2 < 200.0)
                        {
                            cntLowPatternLLR++;
                            continue;
                        }

                        if (sPatternDict.ContainsKey(llr2) == false)
                        {
                            sPatternDict.Add(llr2, new List<string>());
                        }
                        sPatternDict[llr2].Add(subpair.Key);
                    }

                    if (sPatternDict.Count == 0)
                    {
                        //No high quality context pattern, ignore this synonym pair
                        continue;
                    }

                    double rateLowPatternLLR = (double)(cntLowPatternLLR) / (double)(totalPatternLLR);
                    sb.Append(rateLowPatternLLR);
                    sb.Append("\t");
                    sb.Append(totalPatternLLR);
                    //sb.Append("\t");

                    //foreach (KeyValuePair<double, List<string>> subpair in sPatternDict.Reverse())
                    //{
                    //    foreach (string item in subpair.Value)
                    //    {
                    //        sb.Append(item);
                    //        sb.Append("\t");
                    //        sb.Append(subpair.Key);
                    //        sb.Append("\t");
                    //    }
                    //}

                    if (sortedResult.ContainsKey(llr) == false)
                    {
                        sortedResult.Add(llr, new List<string>());
                    }
                    sortedResult[llr].Add(sb.ToString().Trim());

                }
            }

            foreach (KeyValuePair<double, List<string>> pair in sortedResult.Reverse())
            {
                foreach (string item in pair.Value)
                {
                    try
                    {
                        sw.WriteLine(item);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Invalidated line: {0}", item);
                    }
                }
            }
            sw.Close();
        }
    }
}
