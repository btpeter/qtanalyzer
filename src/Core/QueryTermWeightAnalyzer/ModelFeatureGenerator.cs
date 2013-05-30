using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WordSeg;

namespace QueryTermWeightAnalyzer
{
    class ModelFeatureGenerator : CRFSharp.IGenerateFeature
    {
        WordSeg.WordSeg wordseg;
        WordSeg.Tokens wbTokens;

        public List<List<string>> GenerateFeature(string strText)
        {
            List<List<string>> strFeatureList = new List<List<string>>();
            wordseg.Segment(strText, wbTokens, false);
            for (int i = 0; i < wbTokens.tokenList.Count; i++)
            {
                List<string> strList = new List<string>();
                strList.Add(wbTokens.tokenList[i].strTerm);
                strFeatureList.Add(strList);
            }
            return strFeatureList;
        }

        public bool Initialize()
        {
            throw new NotImplementedException();
        }

        public bool Initialize(string strLexicalFileName)
        {
            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(strLexicalFileName, true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);

            return true;
        }
    }
}
