using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;

namespace QueryTermSynonymAnalyzer
{
    public class SynContextSet
    {
        public double llr;
        public Dictionary<string, int> featureSpace;

        public SynContextSet()
        {
            llr = 0.0;
            featureSpace = new Dictionary<string, int>();
        }
    }

    public class QueryTermSynonymAnalyzer
    {
        Dictionary<string, Dictionary<string, SynContextSet>> termpair2featureSet = new Dictionary<string, Dictionary<string, SynContextSet>>();
        WordSeg.WordSeg wordseg;
        WordSeg.Tokens wbTokens;


        public QueryTermSynonymAnalyzer(string strLexicalFileName, string strCorpusFileName)
        {
            InitializeWordSeg(strLexicalFileName);
            LoadFeatureSet(strCorpusFileName);
        }

        private List<string> GetTermSynonym(string strTerm, string strPreContext, string strPostContext)
        {
            if (termpair2featureSet.ContainsKey(strTerm) == false)
            {
                return new List<string>();
            }

            Dictionary<string, SynContextSet> syn2feature = termpair2featureSet[strTerm];
            //Generate feature set
            Dictionary<string, int> featureDict = new Dictionary<string, int>();
            if (strPreContext.Length > 0)
            {
                wbTokens.Clear();
                wordseg.Segment(strPreContext, wbTokens, false);
                int offset = wbTokens.tokenList.Count;
                foreach (Token token in wbTokens.tokenList)
                {
                    string strFeature = token.strTerm; // +"_B"; // +offset.ToString();
                    if (featureDict.ContainsKey(strFeature) == false)
                    {
                        featureDict.Add(strFeature, 1);
                    }
                    else
                    {
                        featureDict[strFeature]++;
                    }
                    offset--;
                }
            }

            if (strPostContext.Length > 0)
            {
                wbTokens.Clear();
                wordseg.Segment(strPostContext, wbTokens, false);
                for (int j = 0; j < wbTokens.tokenList.Count; j++)
                {
                    string strFeature = wbTokens.tokenList[j].strTerm; // +"_P"; // +(j + 1).ToString();
                    if (featureDict.ContainsKey(strFeature) == false)
                    {
                        featureDict.Add(strFeature, 1);
                    }
                    else
                    {
                        featureDict[strFeature]++;
                    }
                }
            }

            SortedDictionary<double, List<string>> sdict = new SortedDictionary<double, List<string>>();
            foreach (KeyValuePair<string, SynContextSet> pair in syn2feature)
            {
                string strSynTerm = pair.Key;
                double synLLR = pair.Value.llr;
                Dictionary<string, int> featureSpace = pair.Value.featureSpace;

                int matchCnt = 0;
                foreach (KeyValuePair<string, int> subpair in featureDict)
                {
                    if (featureSpace.ContainsKey(subpair.Key) == true)
                    {
                        matchCnt++;
                    }
                }
                double matchRate = (double)matchCnt / (double)featureDict.Count;
                if (matchRate > 0 || synLLR > 10.0)
                {
                    if (sdict.ContainsKey(matchRate) == false)
                    {
                        sdict.Add(matchRate, new List<string>());
                    }
                    sdict[matchRate].Add(strSynTerm + "_" + synLLR.ToString());
                }
            }

            List<string> rstList = new List<string>();
            foreach (KeyValuePair<double, List<string>> pair in sdict.Reverse())
            {
                foreach (string item in pair.Value)
                {
                    rstList.Add(item + "_" + pair.Key);
                }
            }

            return rstList;
        }

        public Dictionary<string, List<string>> GetSynonym(string strQuery, int begin, int len)
        {
            Dictionary<string, List<string>> rstDict = new Dictionary<string, List<string>>();
            string strPreContext = strQuery.Substring(0, begin);
            string strPostContext = strQuery.Substring(begin + len);
            string strCoreTerm = strQuery.Substring(begin, len);

            List<string> rstList = GetTermSynonym(strCoreTerm, strPreContext, strPostContext);
            if (rstList.Count > 1)
            {
                rstDict.Add(strCoreTerm, rstList);
                return rstDict;
            }

            WordSeg.Tokens wbTerms;
            wbTerms = wordseg.CreateTokens(1024);
            wordseg.Segment(strCoreTerm, wbTerms, false);

            for (int i = 0; i < wbTerms.tokenList.Count; i++)
            {
                string strTerm = wbTerms.tokenList[i].strTerm;
                if (termpair2featureSet.ContainsKey(strTerm) == false)
                {
                    continue;
                }

                strPreContext = strQuery.Substring(0, begin);
                strPostContext = strQuery.Substring(begin + len);

                for (int j = 0; j < i; j++)
                {
                    strPreContext += wbTerms.tokenList[j].strTerm;
                }

                for (int j = i + 1; j < wbTerms.tokenList.Count; j++)
                {
                    strPostContext = wbTerms.tokenList[j].strTerm + strPostContext;
                }

                rstList = GetTermSynonym(strTerm, strPreContext, strPostContext);
                if (rstDict.ContainsKey(strTerm) == false)
                {
                    rstDict.Add(strTerm, rstList);
                }
            }

            return rstDict;
        }


        public void InitializeWordSeg(string strLexicalFileName)
        {
            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(strLexicalFileName, true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);
        }

        public void LoadFeatureSet(string strCorpusFileName)
        {
            StreamReader sr = new StreamReader(strCorpusFileName, Encoding.UTF8);
            string strLine = "";
            while ((strLine = sr.ReadLine()) != null)
            {
                string[] items = strLine.Split('\t');
                if (items.Length % 2 == 0)
                {
                    Console.WriteLine("{0} is invalidated, skip it.", strLine);
                    continue;
                }
                double llr = double.Parse(items[2]);
                if (llr < 100.0)
                {
                    continue;
                }

                Dictionary<string, int> featureDict = new Dictionary<string, int>();
                string[] sep = new string[1];
                sep[0] = "[X]";
                for (int i = 3; i < items.Length; i += 2)
                {
                    string strContext = items[i];
                    double score = double.Parse(items[i + 1]);
                    int pos = strContext.IndexOf("[X]");
                    string strPreContext = strContext.Substring(0, pos);
                    string strPostContext = strContext.Substring(pos + 3);

                    if (strPreContext.Length > 0)
                    {
                        wbTokens.Clear();
                        wordseg.Segment(strPreContext, wbTokens, false);
                        int offset = wbTokens.tokenList.Count;
                        foreach (Token token in wbTokens.tokenList)
                        {
                            string strFeature = token.strTerm; // +"_B"; // +offset.ToString();
                            if (featureDict.ContainsKey(strFeature) == false)
                            {
                                featureDict.Add(strFeature, 1);
                            }
                            else
                            {
                                featureDict[strFeature]++;
                            }
                            offset--;
                        }
                    }

                    if (strPostContext.Length > 0)
                    {
                        wbTokens.Clear();
                        wordseg.Segment(strPostContext, wbTokens, false);
                        for (int j = 0; j < wbTokens.tokenList.Count; j++)
                        {
                            string strFeature = wbTokens.tokenList[j].strTerm; // +"_P"; // +(j + 1).ToString();
                            if (featureDict.ContainsKey(strFeature) == false)
                            {
                                featureDict.Add(strFeature, 1);
                            }
                            else
                            {
                                featureDict[strFeature]++;
                            }
                        }
                    }
                }

                SynContextSet synConSet = new SynContextSet();
                synConSet.featureSpace = featureDict;
                synConSet.llr = llr;

                if (termpair2featureSet.ContainsKey(items[0]) == false)
                {
                    termpair2featureSet.Add(items[0], new Dictionary<string, SynContextSet>());
                }
                if (termpair2featureSet[items[0]].ContainsKey(items[1]) == false)
                {
                    termpair2featureSet[items[0]].Add(items[1], synConSet);
                }

                if (termpair2featureSet.ContainsKey(items[1]) == false)
                {
                    termpair2featureSet.Add(items[1], new Dictionary<string, SynContextSet>());
                }
                if (termpair2featureSet[items[1]].ContainsKey(items[0]) == false)
                {
                    termpair2featureSet[items[1]].Add(items[0], synConSet);
                }

            }
            sr.Close();
        }
    }
}
