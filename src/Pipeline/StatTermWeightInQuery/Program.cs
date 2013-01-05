using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;

namespace StatTermWeightInQuery
{
    class Token
    {
        public string strTerm;
        public bool bDuplicate;
        public string strHash;
        public double fWeight;

        public Token()
        {
            bDuplicate = false;
        }

    }

    class QueryItem
    {
        //public int clusterFreq;
        public int freq;
        public string strQuery;
        public List<Token> tokenList;

        public QueryItem()
        {
            tokenList = null;
        }
    }

    class Program
    {
        public static SortedDictionary<int, List<string>> freq2TermWeightList = new SortedDictionary<int, List<string>>();
        public static int MIN_QUERY_URL_PAIR_FREQUENCY = 2;
        public static int MIN_CLUSTER_FREQUENCY = 4;
        public static Dictionary<string, List<QueryItem>> query2Item;
        private static StreamWriter sw;
        private static StreamWriter sw_dup;
        public static Tokens tokens;
        public static WordSeg.WordSeg wordseg;


        private static string ShowTokenList(List<Token> tokenList)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0;i < tokenList.Count;i++)
            {
                sb.Append(tokenList[i].strTerm);
            }

            return sb.ToString();
        }

        //Check whether aList is sub set of bList.
        private static bool AsubOfB(List<Token> aList, List<Token> bList, List<Token> joinBList)
        {
            string[] sep;
            sep = new string[1];
            sep[0] = "$_M_$";

            joinBList.Clear();
            for (int i = 0; i < aList.Count; i++)
            {
                bool bMatched = false;
                int j = 0;
                int MaxSim = 0;
                Token bestToken = null;
                Dictionary<int, int> maxSim2Cnt = new Dictionary<int, int>();

                for (j = 0; j < bList.Count; j++)
                {
                    if (aList[i].strTerm == bList[j].strTerm)
                    {
                        if (aList[i].bDuplicate == false && bList[j].bDuplicate == false)
                        {
                            MaxSim = 10;
                            bestToken = bList[j];
                            bMatched = true;
                            break;
                        }
                        else
                        {
                            string[] arrHashA = aList[i].strHash.Split(sep,100, StringSplitOptions.None);
                            string[] arrHashB = bList[j].strHash.Split(sep, 100, StringSplitOptions.None);

                            int sim = 0;
                            if (arrHashA[0] == arrHashB[0])
                            {
                                if (arrHashA[0].Contains("$B$") == true || arrHashA[0].Contains("$E$") == true)
                                {
                                    sim++;
                                }
                                else
                                {
                                    sim += 2;
                                }
                            }
                            if (arrHashA[1] == arrHashB[1])
                            {
                                if (arrHashA[1].Contains("$B$") == true || arrHashA[1].Contains("$E$") == true)
                                {
                                    sim++;
                                }
                                else
                                {
                                    sim += 2;
                                }
                            }
                            if (sim > MaxSim)
                            {
                                bestToken = bList[j];
                                MaxSim = sim;
                                bMatched = true;
                            }

                            if (maxSim2Cnt.ContainsKey(sim) == false)
                            {
                                maxSim2Cnt.Add(sim, 0);
                            }
                            maxSim2Cnt[sim]++;
                        }
                    }
                }

                foreach (KeyValuePair<int, int> pair in maxSim2Cnt)
                {
                    if (pair.Value > 1)
                    {
                        return false;
                    }
                }

                if (bMatched == true)
                {
                    joinBList.Add(bestToken);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }


        private static bool AsubOfB_2(List<Token> aList, List<Token> bList, List<Token> joinBList)
        {
            string[] sep;
            sep = new string[1];
            sep[0] = "$_M_$";

            joinBList.Clear();
            for (int i = 0; i < aList.Count; i++)
            {
                bool bMatched = false;
                int j = 0;
                int MaxSim = 0;
                Token bestToken = null;
                Dictionary<int, int> maxSim2Cnt = new Dictionary<int, int>();

                for (j = 0; j < bList.Count; j++)
                {
                    if (aList[i].strTerm == bList[j].strTerm)
                    {
                        if (aList[i].bDuplicate == false && bList[j].bDuplicate == false)
                        {
                            MaxSim = 10;
                            bestToken = aList[i];
                            bMatched = true;
                            break;
                        }
                        else
                        {
                            string[] arrHashA = aList[i].strHash.Split(sep, 100, StringSplitOptions.None);
                            string[] arrHashB = bList[j].strHash.Split(sep, 100, StringSplitOptions.None);

                            int sim = 0;
                            if (arrHashA[0] == arrHashB[0])
                            {
                                if (arrHashA[0].Contains("$B$") == true || arrHashA[0].Contains("$E$") == true)
                                {
                                    sim++;
                                }
                                else
                                {
                                    sim += 2;
                                }
                            }
                            if (arrHashA[1] == arrHashB[1])
                            {
                                if (arrHashA[1].Contains("$B$") == true || arrHashA[1].Contains("$E$") == true)
                                {
                                    sim++;
                                }
                                else
                                {
                                    sim += 2;
                                }
                            }
                            if (sim > MaxSim)
                            {
                                bestToken = aList[i];
                                MaxSim = sim;
                                bMatched = true;
                            }

                            if (maxSim2Cnt.ContainsKey(sim) == false)
                            {
                                maxSim2Cnt.Add(sim, 0);
                            }
                            maxSim2Cnt[sim]++;
                        }
                    }
                }

                foreach (KeyValuePair<int, int> pair in maxSim2Cnt)
                {
                    if (pair.Value > 1)
                    {
                        return false;
                    }
                }

                if (bMatched == true)
                {
                    joinBList.Add(bestToken);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static void AddQueryList(List<QueryItem> qiList)
        {
            foreach (QueryItem item in qiList)
            {
                if (query2Item.ContainsKey(item.strQuery) == false)
                {
                    query2Item.Add(item.strQuery, new List<QueryItem>());
                }
                query2Item[item.strQuery].Add(item);
            }
        }

        private static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("StatTermWeightInQuery [Query_ClusterId_Freq FileName] [Query_Term_Weight FileName] [Min Query_ClusterId Frequency] [Min Cluster Frequency]");
            }
            else
            {
                MIN_QUERY_URL_PAIR_FREQUENCY = int.Parse(args[2]);
                MIN_CLUSTER_FREQUENCY = int.Parse(args[3]);
                Console.WriteLine("Initializing wordbreaker...");
                wordseg = new WordSeg.WordSeg();
                wordseg.LoadLexicalDict("ChineseDictionary.txt", true);
                tokens = wordseg.CreateTokens(0x400);
                Console.WriteLine("Start to process...");
                query2Item = new Dictionary<string, List<QueryItem>>();
                StreamReader reader = new StreamReader(args[0]);
                sw = new StreamWriter(args[1], false, Encoding.UTF8);
                sw_dup = new StreamWriter(args[1] + ".dup", false, Encoding.UTF8);

                string lastUrl = "";
                List<QueryItem> qiList = new List<QueryItem>();
                while (!reader.EndOfStream)
                {
                    string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                    QueryItem item = new QueryItem
                    {
                        strQuery = strArray[0].Replace(" ", ""),
                        freq = int.Parse(strArray[2])
                    };
                    if (item.freq >= MIN_QUERY_URL_PAIR_FREQUENCY)
                    {
                        string strUrl = strArray[1];
                        if ((lastUrl.Length > 0) && (strUrl != lastUrl))
                        {
                            if (qiList.Count >= 2)
                            {
                                StatTermWeightInQuery(qiList);
                                AddQueryList(qiList);
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
                    AddQueryList(qiList);
                }
                MergeQueryWeight();
                reader.Close();
                sw.Close();
                sw_dup.Close();
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
                    if (pair.Value[0].tokenList == null)
                    {
                        //This query is invlidated, ignore it.
                        Console.WriteLine("Invalidated Query Item at index 0");
                        continue;
                    }

                    int iTotalFreq = 0;
                    foreach (QueryItem item in pair.Value)
                    {
                        if (item.tokenList != null)
                        {
                            iTotalFreq += item.freq;
                        }
                        else
                        {
                            Console.WriteLine("Invalidated Query Item");
                        }
                    }

                    QueryItem rstQueryItem = new QueryItem();
                    rstQueryItem.tokenList = new List<Token>();
                    for (int i = 0; i < pair.Value[0].tokenList.Count; i++)
                    {
                        Token tkn = new Token();
                        tkn.strTerm = pair.Value[0].tokenList[i].strTerm;
                        tkn.strHash = pair.Value[0].tokenList[i].strHash;
                        tkn.fWeight = 0.0;

                        rstQueryItem.tokenList.Add(tkn);
                    }

                    bool bIgnore = false;
                    for (int i = 0; i < rstQueryItem.tokenList.Count; i++)
                    {
                        for (int j = 0; j < pair.Value.Count; j++)
                        {
                            if (pair.Value[j].tokenList == null)
                            {
                                Console.WriteLine("Invalidated Query Item");
                                continue;
                            }

                            if (rstQueryItem.tokenList[i].strTerm != pair.Value[j].tokenList[i].strTerm)
                            {
                                Console.WriteLine("Query with different clicked url is inconsistent");
                                bIgnore = true;
                                break;
                            }
                            rstQueryItem.tokenList[i].fWeight += (((double)pair.Value[j].freq) / ((double)iTotalFreq)) * pair.Value[j].tokenList[i].fWeight;
                        }

                        if (bIgnore == true)
                        {
                            break;
                        }
                    }
                    if (bIgnore == true)
                    {
                        continue;
                    }

                    string strOutput = pair.Key + "\t" + iTotalFreq.ToString() + "\t";
                    for (int i = 0; i < rstQueryItem.tokenList.Count; i++)
                    {
                        strOutput = strOutput + rstQueryItem.tokenList[i].strTerm + "[" + rstQueryItem.tokenList[i].fWeight.ToString("0.##") + "]\t";
                    }
                    sw.WriteLine(strOutput);
                }
            }
        }

        //public static void MergeQueryWeight()
        //{
        //    foreach (KeyValuePair<string, List<QueryItem>> pair in query2Item)
        //    {
        //        if (pair.Value[0].tokenList == null)
        //        {
        //            //This query is invlidated, ignore it.
        //            Console.WriteLine("Invalidated Query Item at index 0");
        //            continue;
        //        }

        //        int maxFreq = 0;
        //        int maxClusterFreq = 0;
        //        int iTotalFreq = 0;
        //        QueryItem bestItem = null;
        //        foreach (QueryItem item in pair.Value)
        //        {
        //            iTotalFreq += item.freq;

        //            if (item.freq > maxFreq)
        //            {
        //                maxFreq = item.freq;
        //                maxClusterFreq = item.clusterFreq;
        //                bestItem = item;
        //            }
        //            else if (item.freq == maxFreq && item.clusterFreq == maxClusterFreq)
        //            {
        //                maxFreq = item.freq;
        //                maxClusterFreq = item.clusterFreq;
        //                bestItem = item;
        //            }
        //        }
        //        if (bestItem == null)
        //        {
        //            continue;
        //        }

        //        string strOutput = pair.Key + "\t" + maxFreq + "\t" + iTotalFreq.ToString() + "\t" + maxClusterFreq.ToString() +"\t";
        //        for (int i = 0; i < bestItem.tokenList.Count; i++)
        //        {
        //            strOutput = strOutput + bestItem.tokenList[i].strTerm + "[" + bestItem.tokenList[i].fWeight.ToString("0.##") + "]\t";
        //        }
        //        sw.WriteLine(strOutput);
        //    }
        //}

        private static string GenerateHash(List<WordSeg.Token> wbTokenList, int index)
        {
            string leftRst = "";
            if (index > 0)
            {
                leftRst = wbTokenList[index - 1].strTerm + wbTokenList[index].strTerm;
            }
            else
            {
                leftRst = "$B$" + wbTokenList[index].strTerm;
            }

            string rightRst = "";
            if (index < wbTokenList.Count - 1)
            {
                rightRst = wbTokenList[index].strTerm + wbTokenList[index + 1].strTerm;
            }
            else
            {
                rightRst = wbTokenList[index].strTerm + "$E$";
            }


            return leftRst + "$_M_$" + rightRst;
        }

        private static void StatTermWeightInQuery(List<QueryItem> qiList)
        {
            List<QueryItem> tmp_qiList = new List<QueryItem>();
            foreach (QueryItem item in qiList)
            {
                wordseg.Segment(item.strQuery, tokens, false);
                List<Token> tknList = new List<Token>();
                bool bIgnored = false;
                for (int i = 0;i < tokens.tokenList.Count;i++)
                {
                    WordSeg.Token wbTkn = tokens.tokenList[i];
                    if (wbTkn.strTerm.Length > 0)
                    {
                        //ignore null string
                        Token token = new Token();
                        token.strTerm = wbTkn.strTerm;
                        token.strHash = GenerateHash(tokens.tokenList, i);

                        //check duplicate terms in a query
                        for (int j = 0; j < tknList.Count; j++)
                        {
                            if (tknList[j].strTerm == token.strTerm)
                            {
                                //found it
                                tknList[j].bDuplicate = true;
                                token.bDuplicate = true;
                                if (tknList[j].strHash == token.strHash)
                                {
                                    sw_dup.WriteLine(item.strQuery);
                                    bIgnored = true;
                                    break;
                                }
                            }
                        }

                        if (bIgnored == true)
                        {
                            break;
                        }

                        tknList.Add(token);
                    }
                }

                if (bIgnored == false)
                {
                    item.tokenList = tknList;
                    tmp_qiList.Add(item);
                }
            }

            qiList.Clear();
            for (int i = 0;i < tmp_qiList.Count;i++)
            {
                Dictionary<Token, int> termHash2Freq = new Dictionary<Token, int>();
                foreach (Token item in tmp_qiList[i].tokenList)
                {
                    termHash2Freq.Add(item, tmp_qiList[i].freq);
                }

                //Check all qiList[i]'s sub-query and statistic frequency
                for (int j = 0; j < tmp_qiList.Count; j++)
                {
                    if (i != j)
                    {
                        List<Token> joinBList = new List<Token>();
                        if (AsubOfB(tmp_qiList[j].tokenList, tmp_qiList[i].tokenList, joinBList) == true)
                        {
                            foreach (Token item in joinBList)
                            {
                                termHash2Freq[item] += tmp_qiList[j].freq;
                            }
                        }
                        else if (AsubOfB_2(tmp_qiList[i].tokenList, tmp_qiList[j].tokenList, joinBList) == true)
                        {
                            foreach (Token item in joinBList)
                            {
                                termHash2Freq[item] += tmp_qiList[j].freq;
                            }
                        }
                    }
                }


                //Get term weight
                int maxFreq = 0;
                foreach (KeyValuePair<Token, int> pair in termHash2Freq)
                {
                    if (maxFreq < pair.Value)
                    {
                        maxFreq = pair.Value;
                    }
                }

                if (maxFreq < MIN_CLUSTER_FREQUENCY)
                {
                    continue;
                }
                
                foreach (Token item in tmp_qiList[i].tokenList)
                {
                    double fWeight = ((double)termHash2Freq[item]) / ((double)maxFreq);
                    item.fWeight = fWeight;
                }

                //tmp_qiList[i].clusterFreq = maxFreq;
                qiList.Add(tmp_qiList[i]);
            }
        }
    }
}


