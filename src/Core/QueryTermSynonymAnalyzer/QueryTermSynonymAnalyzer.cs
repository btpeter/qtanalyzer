using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;
using LMDecoder;

namespace QueryTermSynonymAnalyzer
{
    public class SynContextSet
    {
        public double llr;
        public string strTerm;
    }

    public class SynResult
    {
        public string strTerm;
        public double lmScore;
        public double lmScore_rnn;
        public double llr;
    }

    public class QueryTermSynonymAnalyzer
    {
        //Synonym dictionary
        Dictionary<string, List<SynContextSet>> synPair;

        //Language model based on RNN
        RNNDecoder lmDecoder_rnn;

        //Language model based on ngram
        int lmOrder;
        KNDecoder lmDecoder;

        //Word breaker
        WordSeg.WordSeg wordseg;
        WordSeg.Tokens wbTokens;

        public QueryTermSynonymAnalyzer(string strLexicalFileName, string strSynFileName, string strLMFileName)
        {
            InitializeWordSeg(strLexicalFileName);
            LoadSynonym(strSynFileName);
            LoadLanguageModel(strLMFileName);
        }

        private void LoadSynonym(string strSynFileName)
        {
            StreamReader sr = new StreamReader(strSynFileName);
            string strLine = null;
            synPair = new Dictionary<string, List<SynContextSet>>();
            while ((strLine = sr.ReadLine()) != null)
            {
                string[] items = strLine.Split('\t');
                string strTerm1 = items[0];
                string strTerm2 = items[1];
                double llr = float.Parse(items[2]);
                //if (llr < 1000.0)
                //{
                //    continue;
                //}

                SynContextSet synCtx = new SynContextSet();
                synCtx.strTerm = strTerm2;
                synCtx.llr = llr;
                if (synPair.ContainsKey(strTerm1) == false)
                {
                    synPair.Add(strTerm1, new List<SynContextSet>());
                }

                if (synPair[strTerm1].Count < 10)
                {
                    synPair[strTerm1].Add(synCtx);
                }

                synCtx = new SynContextSet();
                synCtx.strTerm = strTerm1;
                synCtx.llr = llr;
                if (synPair.ContainsKey(strTerm2) == false)
                {
                    synPair.Add(strTerm2, new List<SynContextSet>());
                }

                if (synPair[strTerm2].Count < 10)
                {
                    synPair[strTerm2].Add(synCtx);
                }
            }
            sr.Close();

        }

        private void LoadLanguageModelNGram(string strLMFileName)
        {
            lmDecoder = new LMDecoder.KNDecoder();
            lmDecoder.LoadLM(strLMFileName);
            lmOrder = 4;
        }

        private void LoadLanguageModelRNN(string strLMFileName)
        {
            float regularization = 0.0000001f;
            float dynamic = 0;
            lmDecoder_rnn = new RNNDecoder();
            lmDecoder_rnn.setLambda(0.5);
            lmDecoder_rnn.setRegularization(regularization);
            lmDecoder_rnn.setDynamic(dynamic);
            lmDecoder_rnn.LoadLM(strLMFileName);
        }

        private void LoadLanguageModel(string strLMFileName)
        {
            //Load language model based on ngram
            LoadLanguageModelNGram(strLMFileName);

            //Load language model based on RNN
            LoadLanguageModelRNN("chsSentLM");
        }

        public static int CompareLMResult(SynResult l, SynResult r)
        {
            //return l.lmScore_rnn.CompareTo(r.lmScore_rnn);
            return l.lmScore.CompareTo(r.lmScore);
        }

        public List<SynResult> GetSynonym(string strQuery, int begin, int len)
        {
            List<SynResult> synRstList = new List<SynResult>();

            //Get candidate synonym term
            string strTerm = strQuery.Substring(begin, len);
           
            //Word breaking strQuery
            string strWBQuery = WordSegment(strQuery);
            //Get sentence probability by language model

            //Call RNN language model
            RnnLMResult lmResult_rnn = lmDecoder_rnn.GetSentProb(strWBQuery);
            //Call Ngram language model
            LMDecoder.LMResult lmResult = lmDecoder.GetSentProb(strWBQuery, lmOrder);


            SynResult synRst = new SynResult();
            synRst.strTerm = strTerm;
            synRst.lmScore = (int)(lmResult.perplexity / 1.0);
            synRst.lmScore_rnn = (int)(lmResult_rnn.perplexity / 1.0);
            synRst.llr = -1.0;
            synRstList.Add(synRst);

            if (synPair.ContainsKey(strTerm) == false)
            {
                return synRstList;
            }

            string strLCtx = strQuery.Substring(0, begin);
            string strRCtx = strQuery.Substring(begin + len);
            foreach (SynContextSet ctx in synPair[strTerm])
            {
                //Replace the term with its synonym term
                string strText = strLCtx + ctx.strTerm + strRCtx;
                //Word breaking strQuery
                strWBQuery = WordSegment(strText);
                //Get sentence probability by language model
                //Call RNN language model
                lmResult_rnn = lmDecoder_rnn.GetSentProb(strWBQuery);
                //Call ngram language model
                lmResult = lmDecoder.GetSentProb(strWBQuery, lmOrder);


                synRst = new SynResult();
                synRst.strTerm = ctx.strTerm;
                synRst.lmScore = (int)(lmResult.perplexity / 1.0);
                synRst.lmScore_rnn = (int)(lmResult_rnn.perplexity / 1.0);
                synRst.llr = ctx.llr;
                synRstList.Add(synRst);

            }

            synRstList.Sort(CompareLMResult);

            return synRstList;
        }


        private void InitializeWordSeg(string strLexicalFileName)
        {
            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(strLexicalFileName, true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens();
        }

        private string WordSegment(string strText)
        {
            //Segment text by lexical dictionary
            wordseg.Segment(strText, wbTokens, false);
            StringBuilder sb = new StringBuilder();
            //Parse each broken token
            for (int i = 0; i < wbTokens.tokenList.Count; i++)
            {
                string strTerm = wbTokens.tokenList[i].strTerm.Trim();
                if (strTerm.Length > 0)
                {
                    sb.Append(strTerm);
                    sb.Append(" ");
                }
            }

            return sb.ToString().Trim();
        }
        

    }
}
