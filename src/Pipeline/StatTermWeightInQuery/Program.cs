using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;

namespace StatTermWeightInQuery
{
    class QueryItem
    {
        public int freq;
        public string strQuery;
        public Dictionary<string, double> token2weight;
    }

    class Program
    {
        public static SortedDictionary<int, List<string>> freq2TermWeightList = new SortedDictionary<int, List<string>>();
        public const int MIN_QUERY_NUM_FOR_EACH_URL = 2;
        public static int MIN_QUERY_URL_PAIR_FREQUENCY = 2;
        public static Dictionary<string, List<QueryItem>> query2Item;
        private static StreamWriter sw;
        public static Dictionary<string, List<string>> synonymDict;
        public static Tokens tokens;
        public static WordSeg.WordSeg wordseg;

        //Check whether aList is sub set of bList.
        private static bool AsubOfB(List<string> aList, List<string> bList, ref List<string> rstTokenList)
        {
            rstTokenList.Clear();
            for (int i = 0; i < aList.Count; i++)
            {
                if (!bList.Contains(aList[i]))
                {
                    //Check synonym terms
                    List<string> synonymPair = GetSynonymPair(aList[i]);
                    bool flag = false;
                    if (synonymPair != null)
                    {
                        foreach (string str in synonymPair)
                        {
                            if (bList.Contains(str))
                            {
                                rstTokenList.Add(str);
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        continue;
                    }
                    return false;
                }
                rstTokenList.Add(aList[i]);
            }
            return true;
        }


        public static List<string> GetSynonymPair(string str)
        {
            if (synonymDict.ContainsKey(str))
            {
                return synonymDict[str];
            }
            return null;
        }

        private static void LoadSynonymPairs(string strSynonymDict)
        {
            synonymDict = new Dictionary<string, List<string>>();
            StreamReader reader = new StreamReader(strSynonymDict);
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if ((!strArray[0].Contains(" ") && !strArray[1].Contains(" ")) && (!strArray[0].Contains(strArray[1]) && !strArray[1].Contains(strArray[0])))
                {
                    if (!synonymDict.ContainsKey(strArray[0]))
                    {
                        synonymDict.Add(strArray[0], new List<string>());
                    }
                    if (!synonymDict.ContainsKey(strArray[1]))
                    {
                        synonymDict.Add(strArray[1], new List<string>());
                    }
                    synonymDict[strArray[0]].Add(strArray[1]);
                    synonymDict[strArray[1]].Add(strArray[0]);
                }
            }
            reader.Close();
        }

        private static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("StatTermWeightInQuery [Query_Freq_Url Pair FileName] [Query_Term_Weight FileName] [Min Query Url Pair Frequency]");
            }
            else
            {
                MIN_QUERY_URL_PAIR_FREQUENCY = int.Parse(args[2]);
                Console.WriteLine("Initializing wordbreaker...");
                wordseg = new WordSeg.WordSeg();
                wordseg.LoadLexicalDict("ChineseDictionary.txt", true);
                tokens = wordseg.CreateTokens(0x400);
                Console.WriteLine("Start to process...");
                LoadSynonymPairs("SynonymDictionary.txt");
                query2Item = new Dictionary<string, List<QueryItem>>();
                StreamReader reader = new StreamReader(args[0]);
                sw = new StreamWriter(args[1], false, Encoding.UTF8);
                string lastUrl = "";
                List<QueryItem> qiList = new List<QueryItem>();
                while (!reader.EndOfStream)
                {
                    string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                    QueryItem item = new QueryItem
                    {
                        strQuery = strArray[0],
                        freq = int.Parse(strArray[2])
                    };
                    if (item.freq >= MIN_QUERY_URL_PAIR_FREQUENCY)
                    {
                        item.token2weight = new Dictionary<string, double>();
                        string strUrl = strArray[1];
                        if (!query2Item.ContainsKey(item.strQuery))
                        {
                            query2Item.Add(item.strQuery, new List<QueryItem>());
                        }
                        query2Item[item.strQuery].Add(item);
                        if ((lastUrl.Length > 0) && (strUrl != lastUrl))
                        {
                            if (qiList.Count >= 2)
                            {
                                StatTermWeightInQuery(qiList);
                            }
                            qiList = new List<QueryItem>();
                        }
                        qiList.Add(item);
                        lastUrl = strUrl;
                    }
                }
                if (qiList.Count >= 2)
                {
                    StatTermWeightInQuery(qiList);
                }
                MergeQueryWeight();
                reader.Close();
                sw.Close();
            }
        }

        //A query may have relationship with more than one clicked-url. So it is necessary to
        //merge (query, clicked-url1, term weights1), (query, clicked-url2, term weights2) ... (query, clicked-urlN, term weightN) 
        //into a(query, term weight).
        public static void MergeQueryWeight()
        {
            foreach (KeyValuePair<string, List<QueryItem>> pair in query2Item)
            {
                if (pair.Value.Count != 0)
                {
                    Dictionary<string, double> term2weight = new Dictionary<string, double>();
                    int iTotalFreq = 0;
                    foreach (QueryItem item in pair.Value)
                    {
                        iTotalFreq += item.freq;
                    }
                    foreach (QueryItem item2 in pair.Value)
                    {
                        double queryInUrlPercent = ((double)item2.freq) / ((double)iTotalFreq);
                        foreach (KeyValuePair<string, double> pair2 in item2.token2weight)
                        {
                            if (!term2weight.ContainsKey(pair2.Key))
                            {
                                term2weight.Add(pair2.Key, 0.0);
                            }
                            term2weight[pair2.Key] = term2weight[pair2.Key] + (queryInUrlPercent * pair2.Value);
                        }
                    }

                    if (term2weight.Count > 0)
                    {
                        wordseg.Segment(pair.Key, tokens, false);
                        string strOutput = pair.Key + "\t" + iTotalFreq.ToString() + "\t";
                        bool bInComplete = false;
                        foreach (Token token in tokens.tokenList)
                        {
                            if (token.strTerm.Length > 0)
                            {
                                if (term2weight.ContainsKey(token.strTerm) == false)
                                {
                                    bInComplete = true;
                                    break;
                                }
                                strOutput = strOutput + token.strTerm + "[" + term2weight[token.strTerm].ToString("0.##") + "]\t";
                            }
                        }
                        if (bInComplete == false)
                        {
                            sw.WriteLine(strOutput);
                        }
                        else
                        {
                            Console.WriteLine("Incomplete query : {0}", pair.Key);
                        }
                    }
                }
            }
        }

        private static void StatTermWeightInQuery(List<QueryItem> qiList)
        {
            new Dictionary<string, List<string>>();
            List<List<string>> list = new List<List<string>>();
            foreach (QueryItem item in qiList)
            {
                wordseg.Segment(item.strQuery, tokens, false);
                List<string> strTknList = new List<string>();
                foreach (Token tkn in tokens.tokenList)
                {
                    //ignore null string
                    if (tkn.strTerm.Length > 0)
                    {
                        strTknList.Add(tkn.strTerm);
                    }
                }
                list.Add(strTknList);
            }
            for (int i = 0; i < qiList.Count; i++)
            {
                Dictionary<string, int> term2Freq = new Dictionary<string, int>();
                foreach (string item in list[i])
                {
                    if (!term2Freq.ContainsKey(item))
                    {
                        term2Freq.Add(item, qiList[i].freq);
                    }
                }

                //Check all qiList[i]'s sub-query and statistic frequency
                for (int j = 0; j < qiList.Count; j++)
                {
                    if (i != j)
                    {
                        List<string> rstTokenList = new List<string>();
                        if (AsubOfB(list[j], list[i], ref rstTokenList))
                        {
                            HashSet<string> set = new HashSet<string>();
                            foreach (string item in rstTokenList)
                            {
                                if (!set.Contains(item))
                                {
                                    set.Add(item);
                                    term2Freq[item] += qiList[j].freq;
                                }
                            }
                        }
                    }
                }

                //Get term weight
                int maxFreq = 0;
                foreach (KeyValuePair<string, int> pair in term2Freq)
                {
                    if (maxFreq < pair.Value)
                    {
                        maxFreq = pair.Value;
                    }
                }
                foreach (string item in list[i])
                {
                    double fWeight = ((double)term2Freq[item]) / ((double)maxFreq);
                    if (!qiList[i].token2weight.ContainsKey(item))
                    {
                        qiList[i].token2weight.Add(item, fWeight);
                    }
                }
            }
        }
    }
}


