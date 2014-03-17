using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;
using WordSeg;

namespace StatTermWeightInQuery
{
    class Token
    {
        public string strTerm;
        public double fWeight;
    }

    class QueryItem
    {
        public int freq;
        public string strQuery;
        public List<Token> tokenList;

        public QueryItem()
        {
            tokenList = null;
        }
    }

    //Sort query item by query string
    class QueryDict : IComparable<QueryDict>
    {
        public string strQuery;
        public QueryItem qi;

        public int CompareTo(QueryDict other)
        {
            return String.Compare(strQuery, other.strQuery);
        }
    }

    class Program
    {
        static int MIN_QUERY_URL_PAIR_FREQUENCY = 2;
        static int MIN_CLUSTER_SIZE = 3;
        static VarBigArray<QueryDict> queryDict = new VarBigArray<QueryDict>(1024000);
        static long queryDictIdx = 0;

        static Tokens tokens;
        static WordSeg.WordSeg wordseg;

        static Dictionary<string, string> termNormDict;

        //Check whether aList is subset of bList.
        private static bool AsubOfB(List<Token> aList, List<Token> bList, List<Token> joinList)
        {
            joinList.Clear();
            //For each token in aList, check if it's also in bList, 
            //If yes, save the token, otherwise, aList is not a subset of bList
            for (int i = 0; i < aList.Count; i++)
            {
                bool bMatched = false;
                for (int j = 0; j < bList.Count; j++)
                {
                    if (aList[i].strTerm == bList[j].strTerm)
                    {
                        joinList.Add(bList[j]);
                        bMatched = true;
                        break;
                    }
                }

                if (bMatched == false)
                {
                    return false;
                }
            }
            return true;
        }


        //Check whether aList is superset of bList.
        private static bool AsuperOfB(List<Token> aList, List<Token> bList, List<Token> joinList)
        {
            joinList.Clear();
            //For each token in aList, check if it's also in bList, 
            //If yes, save the token, otherwise, aList is not a subset of bList
            for (int i = 0; i < bList.Count; i++)
            {
                bool bMatched = false;
                for (int j = 0; j < aList.Count; j++)
                {
                    if (aList[j].strTerm == bList[i].strTerm)
                    {
                        joinList.Add(bList[i]);
                        bMatched = true;
                        break;
                    }
                }

                if (bMatched == false)
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
                if (item.tokenList != null)
                {
                    string strQuery = item.strQuery.ToLower().Trim();
                    QueryDict qd = new QueryDict();
                    qd.strQuery = strQuery;
                    qd.qi = item;
                    queryDict[queryDictIdx] = qd;
                    queryDictIdx++;

                    if (queryDictIdx % 100000 == 0)
                    {
                        Console.WriteLine("{0} query items have been generated.", queryDictIdx);
                    }
                }
            }
        }

        //Initializing word breaker
        private static void InitializeWordBreaker(string strLexicalDictionary)
        {
            wordseg = new WordSeg.WordSeg();
            wordseg.LoadLexicalDict(strLexicalDictionary, true);
            tokens = wordseg.CreateTokens();
        }

        //Load mapping file for term normalizing
        private static void LoadNormalizedMappingFile(string strFileName)
        {
            Console.WriteLine("Loading term normalizing mapping file...");
            termNormDict = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(strFileName);
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split('\t');

                if (termNormDict.ContainsKey(items[1]) == false)
                {
                    termNormDict.Add(items[1], items[0]);
                }
                else if (termNormDict[items[1]] != items[0])
                {
                    Console.WriteLine("Duplicated normalize mapping {0} (mapping to {1} in dictionary)", items[1], termNormDict[items[1]]);
                }
            }
            sr.Close();
        }

        //Normalize given term
        public static string NormalizeTerm(string strTerm)
        {
            strTerm = strTerm.ToLower().Trim();
            if (termNormDict.ContainsKey(strTerm) == true)
            {
                return termNormDict[strTerm];
            }

            return strTerm;
        }

        private static void Main(string[] args)
        {
            if (args.Length != 6)
            {
                Console.WriteLine("StatTermWeightInQuery [input:query_clusterId_freq filename] [output:query_term_weight filename] [input:min query_clusterId frequency] [input:min cluster size] [input:word breaker lexical dictionary] [input:normalize mapping filename]");
                return;
            }

            //Initialize parameters
            MIN_QUERY_URL_PAIR_FREQUENCY = int.Parse(args[2]);
            MIN_CLUSTER_SIZE = int.Parse(args[3]);
            InitializeWordBreaker(args[4]);
            LoadNormalizedMappingFile(args[5]);

            Console.WriteLine("Start to process...");
            StreamReader reader = new StreamReader(args[0]);

            string lastUrl = "";
            List<QueryItem> qiList = new List<QueryItem>();
            string strLine = null;
            while ((strLine = reader.ReadLine()) != null)
            {
                strLine = strLine.ToLower().Trim();
                string[] strArray = strLine.Split(new char[] { '\t' });
                if (strArray.Length < 3)
                {
                    Console.WriteLine("Invalidated line: {0}", strLine);
                    continue;
                }

                QueryItem item = null;
                try
                {
                    //Construct query item instance
                    item = new QueryItem
                    {
                        strQuery = NormalizeQuery(strArray[0]),
                        freq = int.Parse(strArray[2])
                    };
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalidated line: {0}", strLine);
                    continue;
                }

                if (item.freq >= MIN_QUERY_URL_PAIR_FREQUENCY)
                {
                    string strUrl = strArray[1];
                    if ((lastUrl.Length > 0) && (strUrl != lastUrl))
                    {
                        if (qiList.Count >= 2)
                        {
                            //Statistics terms weight in each query-url cluster
                            if (StatTermWeightInQuery(qiList) == true)
                            {
                                AddQueryList(qiList);
                            }
                        }
                        qiList = new List<QueryItem>();
                    }
                    qiList.Add(item);
                    lastUrl = strUrl;
                }
            }

            //Stat the last query-url cluster
            if (qiList.Count >= 2)
            {
                if (StatTermWeightInQuery(qiList) == true)
                {
                    AddQueryList(qiList);
                }
            }

            Console.WriteLine("Merging clusters...");
            MergeQueryWeight(args[1]);
            reader.Close();
        }

        //Merge term weights of one query with different clicked-urls
        private static QueryItem MergeOneQuery(List<QueryItem> qiList)
        {
            //Calcuate query's total clicked freq in sum
            int iTotalFreq = 0;
            foreach (QueryItem item in qiList)
            {
                iTotalFreq += item.freq;
            }

            //Initialize query's token list
            QueryItem rstQueryItem = new QueryItem();
            rstQueryItem.tokenList = new List<Token>();
            for (int i = 0; i < qiList[0].tokenList.Count; i++)
            {
                Token tkn = new Token();
                tkn.strTerm = qiList[0].tokenList[i].strTerm;
                tkn.fWeight = 0.0;

                rstQueryItem.tokenList.Add(tkn);
            }

            bool bIgnore = false;
            for (int i = 0; i < rstQueryItem.tokenList.Count; i++)
            {
                for (int j = 0; j < qiList.Count; j++)
                {
                    if (rstQueryItem.tokenList[i].strTerm != qiList[j].tokenList[i].strTerm)
                    {
                        Console.WriteLine("Query with different clicked url is inconsistent");
                        bIgnore = true;

                        break;
                    }
                    rstQueryItem.tokenList[i].fWeight += (((double)qiList[j].freq) / ((double)iTotalFreq)) * qiList[j].tokenList[i].fWeight;
                }

                if (bIgnore == true)
                {
                    break;
                }
            }

            //Invalidated query set, ignore it
            if (bIgnore == true)
            {
                return null;
            }

            return rstQueryItem;
        }


        //A query may have relationship with more than one clicked-url. So it is necessary to
        //merge (query, clicked-url1, term weights1), (query, clicked-url2, term weights2) ... (query, clicked-urlN, term weightN) 
        //into a(query, term weight).
        public static void MergeQueryWeight(string strSavedFileName)
        {
            StreamWriter sw = new StreamWriter(strSavedFileName, false, Encoding.UTF8);

            Console.WriteLine("Sorting query dictionary...");
            queryDict.Sort(0, queryDictIdx);
            List<QueryItem> qiList = new List<QueryItem>();

            string strQuery = queryDict[0].strQuery;
            qiList.Add(queryDict[0].qi);
            for (long k = 1; k < queryDictIdx; k++)
            {
                if (strQuery != queryDict[k].strQuery)
                {
                    //Merge result
                    QueryItem rstQueryItem = MergeOneQuery(qiList);
                    if (rstQueryItem != null)
                    {
                        //Calcuate query's total clicked freq in sum
                        int iTotalFreq = 0;
                        foreach (QueryItem item in qiList)
                        {
                            iTotalFreq += item.freq;
                        }
                        string strOutput = strQuery + "\t" + iTotalFreq.ToString() + "\t";
                        for (int i = 0; i < rstQueryItem.tokenList.Count; i++)
                        {
                            strOutput = strOutput + rstQueryItem.tokenList[i].strTerm + "[" + rstQueryItem.tokenList[i].fWeight.ToString("0.##") + "]\t";
                        }
                        sw.WriteLine(strOutput);
                    }

                    //Clear current list
                    qiList.Clear();
                }

                strQuery = queryDict[k].strQuery;
                qiList.Add(queryDict[k].qi);
            }

            if (qiList.Count > 0)
            {
                //Merge result
                QueryItem rstQueryItem = MergeOneQuery(qiList);
                //Calcuate query's total clicked freq in sum
                int iTotalFreq = 0;
                foreach (QueryItem item in qiList)
                {
                    iTotalFreq += item.freq;
                }
                string strOutput = strQuery + "\t" + iTotalFreq.ToString() + "\t";
                for (int i = 0; i < rstQueryItem.tokenList.Count; i++)
                {
                    strOutput = strOutput + rstQueryItem.tokenList[i].strTerm + "[" + rstQueryItem.tokenList[i].fWeight.ToString("0.##") + "]\t";
                }
                sw.WriteLine(strOutput);
            }

            sw.Close();
        }

        private static string NormalizeQuery(string strQuery)
        {
            StringBuilder sb = new StringBuilder();
            //Break query string by given dictionary
            wordseg.Segment(strQuery, tokens, false);
            for (int i = 0; i < tokens.tokenList.Count; i++)
            {
                //Normalize term and if the term is empty, ignore it
                WordSeg.Token wbTkn = tokens.tokenList[i];
                string strTerm = NormalizeTerm(wbTkn.strTerm).Trim();
                if (strTerm.Length == 0)
                {
                    continue;
                }

                sb.Append(strTerm);
                sb.Append(" ");
            }

            return sb.ToString().Trim();
        }


        private static bool StatTermWeightInQuery(List<QueryItem> qiList)
        {
            //Check each query item in qiList, and save good items for term weight calucating
            int goodQueryItemCount = 0;
            foreach (QueryItem item in qiList)
            {
                List<Token> tknList = new List<Token>();
                bool bIgnored = false;
                //Break query string by given dictionary
                wordseg.Segment(item.strQuery, tokens, false);
                for (int i = 0; i < tokens.tokenList.Count; i++)
                {
                    //Normalize term and if the term is empty, ignore it
                    WordSeg.Token wbTkn = tokens.tokenList[i];
                    string strTerm = wbTkn.strTerm.Trim();
                    if (strTerm.Length == 0)
                    {
                        continue;
                    }

                    //check duplicate terms in a query
                    //If duplicated terms is found, drop current query
                    for (int j = 0; j < tknList.Count; j++)
                    {
                        if (tknList[j].strTerm == strTerm)
                        {
                            //found the duplicated term
                            bIgnored = true;
                            break;
                        }
                    }

                    if (bIgnored == true)
                    {
                        //Ignore the query with duplicated term
                        break;
                    }

                    //Save the token into the list
                    Token token = new Token();
                    token.strTerm = strTerm;
                    tknList.Add(token);
                }

                if (bIgnored == false)
                {
                    item.tokenList = tknList;
                    goodQueryItemCount++;
                }
                else
                {
                    item.tokenList= null;
                }
            }

            //The flag for checking whether query is sub-query or super-query for all other queries in the cluster
            bool bEntireCluster = false;
            for (int i = 0;i < qiList.Count;i++)
            {
                QueryItem selQueryItem = qiList[i];
                if (selQueryItem.tokenList == null)
                {
                    continue;
                }

                Dictionary<Token, int> termHash2Freq = new Dictionary<Token, int>();
                int totalFreq = selQueryItem.freq;
                int queryInCluster = 1;
                foreach (Token item in selQueryItem.tokenList)
                {
                    termHash2Freq.Add(item, selQueryItem.freq);
                }

                //Check selQueryItem's sub-query and super-query, and statistic frequency
                for (int j = 0; j < qiList.Count; j++)
                {
                    if (i != j && qiList[j].tokenList != null)
                    {
                        List<Token> joinBList = new List<Token>();
                        //Try to find selQueryItem's sub-query
                        if (AsubOfB(qiList[j].tokenList, selQueryItem.tokenList, joinBList) == true)
                        {
                            //Found a sub-query of selQueryItem
                            //Increase the cluster, query and term's frequency
                            queryInCluster++;
                            totalFreq += qiList[j].freq;
                            foreach (Token item in joinBList)
                            {
                                termHash2Freq[item] += qiList[j].freq;
                            }
                        }
                        //Try to find selQueryItem's super-query
                        else if (AsuperOfB(qiList[j].tokenList, selQueryItem.tokenList, joinBList) == true)
                        {
                            //Found a super-query of selQueryItem
                            //Increase the cluster, query and term's frequency
                            queryInCluster++;
                            totalFreq += qiList[j].freq;
                            foreach (Token item in joinBList)
                            {
                                termHash2Freq[item] += qiList[j].freq;
                            }
                        }
                    }
                }

                if (queryInCluster < MIN_CLUSTER_SIZE)
                {
                    //The generated cluster is too small, ignore current query item
                    selQueryItem.tokenList = null;
                    continue;
                }

                if (queryInCluster == goodQueryItemCount && selQueryItem.strQuery.Length >= 2)
                {
                    //All other queries are current query's sub or super set.
                    bEntireCluster = true;
                }

                foreach (Token item in selQueryItem.tokenList)
                {
                    double fWeight = ((double)termHash2Freq[item]) / ((double)totalFreq);
                    item.fWeight = fWeight;
                }
            }

            return bEntireCluster;
        }
    }
}


