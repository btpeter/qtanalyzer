using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;

namespace QueryTermWeightAnalyzer
{
    public class Token
    {
        public int offset;
        public int length;
        public string strTerm;
        public string strTag;
    }

    public class QueryTermWeightAnalyzer
    {
        const string KEY_LEXICAL_DICT_FILE_NAME = "LexicalDictFileName";
        const string KEY_MODEL_FILE_NAME = "ModelFileName";
        const string KEY_UNIGRAM_FILE_NAME = "UnigramFileName";
        const string KEY_BIGRAM_FILE_NAME = "BigramFileName";

        WordSeg.WordSeg wordseg;
        WordSeg.Tokens wbTokens;
        bool bUseCRFModel = false;
        Dictionary<string, int> unigram, bigram;

        private Dictionary<string, int> LoadNGram(string strFileName)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            StreamReader sr = new StreamReader(strFileName);
            int LineCnt = 0;
            HashSet<string> setWrongTerm = new HashSet<string>();
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split('\t');
                LineCnt++;

                if (setWrongTerm.Contains(items[0]) == false)
                {
                    try
                    {
                        dict.Add(items[0], int.Parse(items[1]));
                    }
                    catch (System.Exception err)
                    {
                        dict.Remove(items[0]);
                        setWrongTerm.Add(items[0]);
                    }
                }
            }
            sr.Close();

            return dict;
        }

        private Dictionary<string, string> LoadConfFile(string strConfFileName)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(strConfFileName);
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split('=');

                items[0] = items[0].ToLower().Trim();
                items[1] = items[1].ToLower().Trim();

                if (items[0] != KEY_LEXICAL_DICT_FILE_NAME.ToLower() &&
                    items[0] != KEY_MODEL_FILE_NAME.ToLower() &&
                    items[0] != KEY_UNIGRAM_FILE_NAME.ToLower() &&
                    items[0] != KEY_BIGRAM_FILE_NAME.ToLower())
                {
                    throw new Exception("Invalidated configuration item");

                }
                dict.Add(items[0], items[1]);
            }

            sr.Close();

            return dict;
        }


        public bool Initialize(string strConfFileName)
        {
            Dictionary<string, string> confDict;
            confDict = LoadConfFile(strConfFileName);

            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(confDict[KEY_LEXICAL_DICT_FILE_NAME.ToLower()], true);
            if (confDict[KEY_MODEL_FILE_NAME.ToLower()] == null || confDict[KEY_MODEL_FILE_NAME.ToLower()].Length == 0)
            {
                bUseCRFModel = false;
            }
            else
            {
                bUseCRFModel = true;
                wordseg.LoadModelFile(confDict[KEY_MODEL_FILE_NAME.ToLower()], null);
            }
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);

            unigram = LoadNGram(confDict[KEY_UNIGRAM_FILE_NAME.ToLower()]);
            bigram = LoadNGram(confDict[KEY_BIGRAM_FILE_NAME.ToLower()]);
            return true;

        }

        private double LMProb(string term1, string term2)
        {
            string strBiTerm = term1 + " " + term2;
            int bigramFreq;
            int unigramFreq;
            if (bigram.ContainsKey(strBiTerm) == false)
            {
                bigramFreq = 0;
            }
            else
            {
                bigramFreq = bigram[strBiTerm];
            }

            if (unigram.ContainsKey(term1) == false)
            {
                unigramFreq = 0;
            }
            else
            {
                unigramFreq = unigram[term1];
            }

            if (unigramFreq == 0)
            {
                return 0.0;
            }

            return (double)(bigramFreq) / (double)(unigramFreq);
        }

        public string GetTermRankTag(List<string> strTagList)
        {
            foreach (string item in strTagList)
            {
                if (item == "RANK_0" || item == "RANK_1" || item == "RANK_2")
                {
                    return item;
                }
            }
            return null;
        }

        public string GetTermOrderTag(List<string> strTagList)
        {
            foreach (string item in strTagList)
            {
                if (item == ">" || item == "=" || item == "<" || item == "E")
                {
                    return item;
                }
            }
            return null;
        }

        public void ConvertOrderToRank(List<Token> tokenList)
        {
            int maxRank = -10;
            int minRank = 10;
            int currRank = 0;
            foreach (Token token in tokenList)
            {
                if (token.strTag == ">")
                {
                    currRank--;
                    if (currRank > maxRank)
                    {
                        maxRank = currRank;
                    }
                }
                else if (token.strTag == "<")
                {
                    currRank++;
                    if (currRank < minRank)
                    {
                        minRank = currRank;
                    }
                }
            }

            currRank = 0;
            foreach (Token token in tokenList)
            {
                token.strTag = "RANK_" + Math.Abs(currRank - maxRank).ToString();
                if (token.strTag == ">")
                {
                    currRank--;
                }
                else if (token.strTag == "<")
                {
                    currRank++;
                }
            }
        }


        public List<Token> LabelString(string strText)
        {
            List<Token> tknList = new List<Token>();
            wordseg.Segment(strText, wbTokens, bUseCRFModel);
            for (int i = 0; i < wbTokens.tokenList.Count; i++)
            {
                Token tkn = new Token();
                tkn.offset = wbTokens.tokenList[i].offset;
                tkn.length = wbTokens.tokenList[i].len;
                tkn.strTerm = wbTokens.tokenList[i].strTerm;
                tkn.strTag = GetTermOrderTag(wbTokens.tokenList[i].strTagList);

                tknList.Add(tkn);
            }
            return tknList;
        }


        public List<Token> Analyze(string strText)
        {
            List<Token> tknList = LabelString(strText);
            for (int i = 0; i < tknList.Count; i++)
            {
                double probLeftTerm = 0.0, probRightTerm = 0.0;
                string strLeftTermWeight = "", strRightTermWeight = "";

                if (i > 0)
                {
                    probLeftTerm = LMProb(tknList[i].strTerm, tknList[i - 1].strTerm);
                    strLeftTermWeight = tknList[i - 1].strTag;
                }

                if (i < tknList.Count - 1)
                {
                    probRightTerm = LMProb(tknList[i].strTerm, tknList[i + 1].strTerm);
                    strRightTermWeight = tknList[i + 1].strTag;
                }

                if (probLeftTerm > probRightTerm)
                {
                    if (probLeftTerm > 0.8)
                    {
                        tknList[i].strTag = tknList[i].strTag + "|" + strLeftTermWeight;
                    }
                }
                else
                {
                    if (probRightTerm > 0.8)
                    {
                        tknList[i].strTag = tknList[i].strTag + "|" + strRightTermWeight;
                    }
                }

                if (tknList[i].strTag.Contains("RANK_0") == true)
                {
                    tknList[i].strTag = "RANK_0";
                }
                else if (tknList[i].strTag.Contains("RANK_1") == true)
                {
                    tknList[i].strTag = "RANK_1";
                }
                else
                {
                    tknList[i].strTag = "RANK_2";
                }
            }

            return tknList;
        }

    }
}
