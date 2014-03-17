using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AdvUtils;
using WordSeg;
using CRFSharp;
using CRFSharpWrapper;
using QueryTermWeightAnalyzer.Features;
using StochasticGradientBoost;

namespace QueryTermWeightAnalyzer
{
    public class QueryTermWeightAnalyzer
    {
        //Key items in configuration file
        const string KEY_LEXICAL_DICT_FILE_NAME = "LexicalDictFileName";
        const string KEY_MODEL_FILE_NAME = "ModelFileName";
        const string KEY_RANKPERCENT_FILE_NAME = "RankPercentFileName";
        const string KEY_RANKMODEL_FILE_NAME = "RankingModelFileName";
        const string KEY_ACTIVEFEATURE_FILE_NAME = "ActiveFeatureFileName";
        const string KEY_PUNCT_DICT_FILE_NAME = "PunctDictFileName";
        const string KEY_NORMALIZED_TERM_FILE_NAME = "NormalizedTermFileName";
        const string KEY_RUN_RANKER_MODEL = "RunRankerModel";
        const string KEY_LANGUAGE_MODEL_FILE_NAME = "LanguageModelFileName";
  
        //Run ranker model
        bool bRunRankerModel = true;

        //word breaker
        WordSeg.WordSeg wordseg;


        //Feature set for ranker
        //CRF model
        CRFSharpWrapper.Decoder crf;
        CRFSharpFeatureGenerator featureGenerator;

        //Language models
        LMDecoder.KNDecoder lmDecoder;
        Dictionary<string, RankXDist> term2rankDist;
        HashSet<string> setPunct;

        //feature list
        List<IFeature> featureList;

        //Ranking model
        NNModel modelMSN;

        //Term normalizing mapping
        Dictionary<string, string> termNormDict;

        private void LoadLanguageModel(string strLMFileName)
        {
            lmDecoder = new LMDecoder.KNDecoder();
            lmDecoder.LoadLM(strLMFileName);
        }

        private void LoadPunctDict(string strFileName)
        {
            setPunct = new HashSet<string>(File.ReadAllLines(strFileName));
            if (setPunct == null)
            {
                Console.WriteLine("Failed to load punct dictionary from {0}", strFileName);
            }
        }

        private bool LoadRankModel(string strModelFileName, string strFtrFileName)
        {
            if (strModelFileName == null || strModelFileName.Length == 0)
            {
                return false;
            }
            if (strFtrFileName == null || strFtrFileName.Length == 0)
            {
                return false;
            }

            modelMSN = NNModel.Create(strModelFileName);
            if (modelMSN == null)
            {
                Console.WriteLine("Failed to load model: " + strModelFileName);
                return false;
            }

            string[] activeFeatureNames = null;
            //read and process only a subset of activated features as specified in the activeFeatureFile
            activeFeatureNames = File.ReadAllLines(strFtrFileName);
            if (modelMSN.SetFeatureNames(activeFeatureNames) == false)
            {
                Console.WriteLine("Failed to set feature set");
                return false;
            }

            return true;
        }

        private void LoadRankPercent(string strFileName)
        {
            StreamReader sr = new StreamReader(strFileName);
            term2rankDist = new Dictionary<string, RankXDist>();
            while (sr.EndOfStream == false)
            {
                //Format:
                //term \t rank0 % \t rank1 % \t rank2 % \t rank3 % \t rank4 %
                string strLine = sr.ReadLine();
                string[] items = strLine.Split('\t');
                string strTerm = items[0];

                if (items[1] == "NaN")
                {
                    items[1] = "0.0";
                }
                if (items[2] == "NaN")
                {
                    items[2] = "0.0";
                }
                if (items[3] == "NaN")
                {
                    items[3] = "0.0";
                }
                if (items[4] == "NaN")
                {
                    items[4] = "0.0";
                }
                if (items[5] == "NaN")
                {
                    items[5] = "0.0";
                }

                RankXDist dist = new RankXDist();
                dist.pRank0 = double.Parse(items[1]);
                dist.pRank1 = double.Parse(items[2]);
                dist.pRank2 = double.Parse(items[3]);
                dist.pRank3 = double.Parse(items[4]);
                dist.pRank4 = double.Parse(items[5]);

                term2rankDist.Add(strTerm, dist);

            }
            sr.Close();
        }

        //Initialize feature list
        private List<IFeature> InitFeatureList()
        {
            List<IFeature> featureList = new List<IFeature>();
            featureList.Add(new WordFormationFeature());
            featureList.Add(new LanguageModelFeature());
            featureList.Add(new TermLengthFeature());
            featureList.Add(new TermOffsetFeature());
            featureList.Add(new IsBeginTermFeature());
            featureList.Add(new IsEndTermFeature());
            featureList.Add(new TermRank0PercentFeature());
            featureList.Add(new TermRank1PercentFeature());
            featureList.Add(new TermRank2PercentFeature());
            featureList.Add(new TermRank3PercentFeature());
            featureList.Add(new TermRank4PercentFeature());
            featureList.Add(new IsPunctFeature());

            return featureList;
        }

      
        //Load ini file items
        private Dictionary<string, string> LoadConfFile(string strConfFileName)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(strConfFileName);
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine().Trim().ToLower();
                if (strLine.Length == 0)
                {
                    //Ignore empty line
                    continue;
                }

                //Split key and vlaue pair
                string[] items = strLine.Split('=');
                items[0] = items[0].ToLower().Trim();
                items[1] = items[1].ToLower().Trim();

                if (items[0] != KEY_LEXICAL_DICT_FILE_NAME.ToLower() &&
                    items[0] != KEY_MODEL_FILE_NAME.ToLower() &&
                    items[0] != KEY_RANKPERCENT_FILE_NAME.ToLower() &&
                    items[0] != KEY_RANKMODEL_FILE_NAME.ToLower() &&
                    items[0] != KEY_ACTIVEFEATURE_FILE_NAME.ToLower() &&
                    items[0] != KEY_PUNCT_DICT_FILE_NAME.ToLower() &&
                    items[0] != KEY_NORMALIZED_TERM_FILE_NAME.ToLower() &&
                    items[0] != KEY_RUN_RANKER_MODEL.ToLower() &&
                    items[0] != KEY_LANGUAGE_MODEL_FILE_NAME.ToLower())
                {
                    Console.WriteLine("{0} is invalidated item", strLine);
                    return null;
                }
                dict.Add(items[0], items[1]);
            }

            sr.Close();

            return dict;
        }

        //Load mapping file for term normalizing
        private void LoadNormalizedMappingFile(string strFileName)
        {
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

        //Create worker instance for each thread
        //If the analyzer runs in multip-thread environment, the work instance should be created in each thread separatedly
        public Instance CreateInstance()
        {
            Instance instance = new Instance();

            instance.crf_tag = crf.CreateTagger();
            instance.crf_tag.set_nbest(1);
            instance.crf_tag.set_vlevel(0);
            //Initialize word breaker's token instance
            instance.wbTokens = wordseg.CreateTokens();
            instance.crf_seg_out = new crf_seg_out[1];
            instance.crf_seg_out[0] = new crf_seg_out();
            instance.ftrList = new float[featureList.Count];

            instance.context = new FeatureContext();
            instance.context.term2rankDist = term2rankDist;
            instance.context.setPunct = setPunct;
            instance.context.lmDecoder = lmDecoder;

            return instance;
        }

        public bool Initialize(string strConfFileName)
        {
            //Load configuration file
            Dictionary<string, string> confDict;
            confDict = LoadConfFile(strConfFileName);
            if (confDict == null)
            {
                return false;
            }

            //Check required item
            if (confDict.ContainsKey(KEY_LEXICAL_DICT_FILE_NAME.ToLower()) == false)
            {
                Console.WriteLine("Failed to find key {0}", KEY_LEXICAL_DICT_FILE_NAME);
                return false;
            }

            if (confDict.ContainsKey(KEY_MODEL_FILE_NAME.ToLower()) == false)
            {
                Console.WriteLine("Failed to find key {0}", KEY_MODEL_FILE_NAME);
                return false;
            }

            //Load temr normalizing mapping file
            LoadNormalizedMappingFile(confDict[KEY_NORMALIZED_TERM_FILE_NAME.ToLower()]);

            //Load CRF model for word formation
            crf = new CRFSharpWrapper.Decoder();
            string strModelFileName = confDict[KEY_MODEL_FILE_NAME.ToLower()];
            crf.LoadModel(strModelFileName);
            featureGenerator = new CRFSharpFeatureGenerator();

            //Load lexical dictionary
            wordseg = new WordSeg.WordSeg();
            wordseg.LoadLexicalDict(confDict[KEY_LEXICAL_DICT_FILE_NAME.ToLower()], true);

            if (confDict.ContainsKey(KEY_RUN_RANKER_MODEL.ToLower()) == true)
            {
                bRunRankerModel = bool.Parse(confDict[KEY_RUN_RANKER_MODEL.ToLower()]);
            }
            if (bRunRankerModel == false)
            {
                return true;
            }

            //Load punct dict
            if (confDict.ContainsKey(KEY_PUNCT_DICT_FILE_NAME.ToLower()) == true)
            {
                LoadPunctDict(confDict[KEY_PUNCT_DICT_FILE_NAME.ToLower()]);
            }
            else
            {
                Console.WriteLine("Failed to find key {0}", KEY_PUNCT_DICT_FILE_NAME);
                return false;
            }


            //Load language model
            if (confDict.ContainsKey(KEY_LANGUAGE_MODEL_FILE_NAME.ToLower()) == true)
            {
                LoadLanguageModel(confDict[KEY_LANGUAGE_MODEL_FILE_NAME.ToLower()]);
            }
            else
            {
                Console.WriteLine("Failed to find key {0}", KEY_LANGUAGE_MODEL_FILE_NAME);
                return false;
            }

            //Load term important level percent data
            if (confDict.ContainsKey(KEY_RANKPERCENT_FILE_NAME.ToLower()) == true)
            {
                LoadRankPercent(confDict[KEY_RANKPERCENT_FILE_NAME.ToLower()]);
            }
            else
            {
                Console.WriteLine("Failed to find key {0}", KEY_RANKPERCENT_FILE_NAME);
                return false;
            }


            //Initialize feature set for ranking
            featureList = InitFeatureList();

            if (confDict.ContainsKey(KEY_RANKMODEL_FILE_NAME.ToLower()) == false)
            {
                Console.WriteLine("Failed to find key {0}", KEY_RANKMODEL_FILE_NAME);
                return false;
            }

            if (confDict.ContainsKey(KEY_ACTIVEFEATURE_FILE_NAME.ToLower()) == false)
            {
                Console.WriteLine("Failed to find key {0}", KEY_ACTIVEFEATURE_FILE_NAME);
                return false;
            }

            //Load ranking model
            LoadRankModel(confDict[KEY_RANKMODEL_FILE_NAME.ToLower()], confDict[KEY_ACTIVEFEATURE_FILE_NAME.ToLower()]);

            return true;

        }

        //Normalize given term
        private string NormalizeTerm(string strTerm)
        {
            strTerm = strTerm.ToLower().Trim();
            if (termNormDict.ContainsKey(strTerm) == true)
            {
                return termNormDict[strTerm];
            }

            return strTerm;
        }

        //Extract string which is core or normal term
        private List<string> ExtractCoreToken(List<Token> tknList)
        {
            List<string> termList = new List<string>();
            foreach (Token tkn in tknList)
            {
                //By design, the important level of core and normal term is less than 2
                if (tkn.rankId < 2)
                {
                    termList.Add(tkn.strTerm);
                }
            }
            return termList;
        }

        //Check whether token list contain optional term
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

        //Normalize token weight level in the given list
        private void NormalizeTermWeight(List<Token> tknList)
        {
            //Check the number of token with rank0, if there is no such token, 
            //Adjust token's rank id
            int rank0Cnt = 0, rank1Cnt = 0;
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

        //Labeling tokens according its word formation by CRF model
        private static string strRankTagPrefix = "RANK_";
        private List<Token> LabelString(Instance instance, List<string> termList)
        {
            //Extract features from given text
            List<List<string>> sinbuf = featureGenerator.GenerateFeature(termList);

            //Call CRFSharp to predict word formation tags
            int ret = crf.Segment(instance.crf_seg_out, instance.crf_tag, sinbuf);
            //Only use 1st-best result
            crf_seg_out item = instance.crf_seg_out[0];
            if (ret < 0 || item.Count != termList.Count)
            {
                //CRF parsing is failed
                string strMessage = "Failed to parse word formation by model. RetVal: " + ret.ToString() + ", Parsed Token Count: " + item.Count.ToString() + ", Input Token Count: " + termList.Count.ToString();
                Console.WriteLine(strMessage);
                return null;
            }

            //Fill the token list
            List<Token> tknList = new List<Token>();
            for (int j = 0; j < item.Count; j++)
            {
                int offset = item.tokenList[j].offset;
                int len = item.tokenList[j].length;
                string strNE = item.tokenList[j].strTag;

                Token token = new Token();
                token.strTerm = termList[j];
                token.rankId = int.Parse(strNE.Substring(strRankTagPrefix.Length));

                tknList.Add(token);
            }

            return tknList;
        }

        //Call word breaker to break given text
        private List<string> WordBreak(Instance instance, string strText)
        {
            List<string> termList = new List<string>();
            wordseg.Segment(strText, instance.wbTokens, false);
            for (int i = 0; i < instance.wbTokens.tokenList.Count; i++)
            {
                string strTerm = NormalizeTerm(instance.wbTokens.tokenList[i].strTerm);
                if (strTerm.Length == 0)
                {
                    //Ignore empty string
                    continue;
                }
                termList.Add(strTerm);
            }
            return termList;
        }

        //Try to assign subTknList's rank id to tknList 
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
                    //Ignore optional terms, just check core and normal terms
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
                    //Found an optional terms, increase its rank id
                    tknList[j].rankId++;
                    j++;
                    continue;
                }

                //Assign rank id
                tknList[j].rankId = subTknList[i].rankId;
                i++;
                j++;
            }

            return true;

        }

        //Call CRF decoder to predict term important level by word formation
        private List<Token> AnalyzeWordFormationLevel(Instance instance, List<string> termList)
        {
            List<Token> tknList = LabelString(instance, termList);
            if (tknList == null)
            {
                //CRF decoder parse is failed
                return null;
            }

            NormalizeTermWeight(tknList);
            if (HasOptTerms(tknList) == false)
            {
                //No optional term, return directly
                return tknList;
            }

            while (true)
            {
                List<string> coreTknList = ExtractCoreToken(tknList);
                List<Token> tmpTknList = LabelString(instance, coreTknList);
                if (tmpTknList == null)
                {
                    //CRF decoder parse is failed, abort current process
                    break;
                }

                NormalizeTermWeight(tmpTknList);
                if (MergeTermWeight(tmpTknList, tknList) == false)
                {
                    //If no new result need to be mereged into result, end the process
                    break;
                }
            }

            return tknList;
        }

        //Call CRF decoder to predict term important level by word formation
        private List<Token> AnalyzeWordFormationLevel(Instance instance, string strText)
        {
            List<string> termList = WordBreak(instance, strText);
            return AnalyzeWordFormationLevel(instance, termList);
        }

        //Analyze query term important level and score
        public List<Token> Analyze(Instance instance, string strText)
        {
            //Call CRF decoder to analyze important level by word formation
            List<Token> tknList = AnalyzeWordFormationLevel(instance, strText);
            if (tknList == null)
            {
                //Analyze term important level by word formation is failed.
                return null;
            }

            if (bRunRankerModel == false)
            {
                //No need to run ranker
                return tknList;
            }

            //Initialize ranking feature context
            instance.context.tknList = tknList;
            //Calcuate each term's important ranking score
            for (int i = 0; i < tknList.Count; i++)
            {
                //Fill feature context
                instance.context.index = i;

                //Extract features by current context
                for (int j = 0; j < featureList.Count; j++)
                {
                    instance.ftrList[j] = featureList[j].GetValue(instance.context);
                }

                //Predict term's ranking score
                tknList[i].rankingscore = 1.0f - modelMSN.Evaluate(instance.ftrList);
            }

            return tknList;
        }

        #region Feature Extractor

        //List entire all features' name
        public string GetFeatureName()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IFeature feature in featureList)
            {
                sb.Append(feature.GetName());
                sb.Append("\t");
            }
            return sb.ToString().Trim();
        }
        
        //Extract given terms feature set as string format
        public List<string> ExtractFeature(Instance instance, List<string> termList)
        {
            List<Token> tknList = AnalyzeWordFormationLevel(instance, termList);
            if (tknList == null)
            {
                //Analyze term weight by word formation is failed
                return null;
            }

            instance.context.tknList = tknList;
            List<string> rstList = new List<string>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tknList.Count; i++)
            {
                sb.Clear();
                instance.context.index = i;
                foreach (IFeature feature in featureList)
                {
                    string strValue = feature.GetValue(instance.context).ToString();
                    sb.Append(strValue);
                    sb.Append("\t");
                }

                rstList.Add(sb.ToString().Trim());
            }

            return rstList;
        }
        #endregion

    }
}
