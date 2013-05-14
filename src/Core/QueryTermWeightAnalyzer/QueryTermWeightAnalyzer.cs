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
        public string strTerm;
        public int rankId;
    }

    public class QueryTermWeightAnalyzer
    {
        const string KEY_LEXICAL_DICT_FILE_NAME = "LexicalDictFileName";
        const string KEY_MODEL_FILE_NAME = "ModelFileName";

        WordSeg.WordSeg wordseg;
        WordSeg.Tokens wbTokens;
        bool bUseCRFModel = false;

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
                    items[0] != KEY_MODEL_FILE_NAME.ToLower())
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
                ModelFeatureGenerator featureGenerator = new ModelFeatureGenerator();
                featureGenerator.Initialize(confDict[KEY_LEXICAL_DICT_FILE_NAME.ToLower()]);
                wordseg.LoadModelFile(confDict[KEY_MODEL_FILE_NAME.ToLower()], featureGenerator);
            }
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);

            return true;

        }

      

        private string GetTermRankTag(List<string> strTagList)
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

        private string ExtractCoreTerms(List<Token> tknList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Token tkn in tknList)
            {
                if (tkn.rankId < 2)
                {
                    sb.Append(tkn.strTerm);
                }
            }
            return sb.ToString();
        }

        private bool HasOptTerms(List<Token> tknList)
        {
            foreach (Token tkn in tknList)
            {
                if (tkn.rankId >= 2)
                {
                    return true;
                }
            }
            return false;
        }

        private void NormalizeTermWeight(List<Token> tknList)
        {
            //Check the number of token with rank0, if there is no such token, 
            //Adjust token's rank id
            int rank0Cnt = 0, rank1Cnt = 1;
            foreach (Token tkn in tknList)
            {
                if (tkn.rankId == 0)
                {
                    rank0Cnt++;
                }
                else if (tkn.rankId == 1)
                {
                    rank1Cnt++;
                }
            }
            if (rank0Cnt == 0 && rank1Cnt == 0)
            {
                //If all token's term rank are rank2, rewrite them as rank0
                foreach (Token tkn in tknList)
                {
                    tkn.rankId = 0;
                }
            }
            else if (rank0Cnt == 0)
            {
                //If no token's term ran is rank0, increase all token's term rank level
                foreach (Token tkn in tknList)
                {
                    tkn.rankId--;
                }
            }
        }

        private List<Token> LabelString(string strText)
        {
            List<Token> tknList = new List<Token>();
            wordseg.Segment(strText, wbTokens, bUseCRFModel);
            for (int i = 0; i < wbTokens.tokenList.Count; i++)
            {
                Token tkn = new Token();
                tkn.offset = wbTokens.tokenList[i].offset;
                tkn.strTerm = wbTokens.tokenList[i].strTerm;
                string strTag = GetTermRankTag(wbTokens.tokenList[i].strTagList);
                if (strTag == "RANK_0")
                {
                    tkn.rankId = 0;
                }
                else if (strTag == "RANK_1")
                {
                    tkn.rankId = 1;
                }
                else
                {
                    tkn.rankId = 2;
                }

                tknList.Add(tkn);
            }
            return tknList;
        }

        private bool MergeTermWeight(List<Token> subTknList, List<Token> tknList)
        {
            if (HasOptTerms(subTknList) == false)
            {
                //No need to merge
                return false;
            }

            //Check whether the token boundary between tknList and subTknList are the same.
            //If not, do not merge them
            int i = 0, j = 0;
            while (j < tknList.Count)
            {
                if (tknList[j].rankId >= 2)
                {
                    j++;
                    continue;
                }

                if (tknList[j].strTerm != subTknList[i].strTerm)
                {
                    return false;
                }
                i++;
                j++;
            }

            i = 0;
            j = 0;
            while (j < tknList.Count)
            {
                if (tknList[j].rankId >= 2)
                {
                    tknList[j].rankId++;
                    j++;
                    continue;
                }

                tknList[j].rankId = subTknList[i].rankId;
                i++;
                j++;
            }

            return true;

        }

        public List<Token> Analyze(string strText)
        {
            List<Token> tknList = LabelString(strText);
            NormalizeTermWeight(tknList);

            if (HasOptTerms(tknList) == false)
            {
                //No optional term, return directly
                return tknList;
            }

            while (true)
            {
                string strCoreTerms = ExtractCoreTerms(tknList);
                List<Token> tmpTknList = LabelString(strCoreTerms);
                NormalizeTermWeight(tmpTknList);

                if (MergeTermWeight(tmpTknList, tknList) == false)
                {
                    //If no new result need to be mereged into result, end the process
                    break;
                }
            }

            return tknList;
        }

    }
}
