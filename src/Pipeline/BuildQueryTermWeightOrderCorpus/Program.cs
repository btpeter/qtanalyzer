using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;

namespace BuildQueryTermWeightOrderCorpus
{
    class Program
    {
        static WordSeg.WordSeg wordseg;
        static WordSeg.Tokens wbTokens;


        static void MergeTokens(List<string> termList, List<double> weightList, out List<string> mTermList, out List<double> mWeightList)
        {
            mTermList = new List<string>();
            mWeightList = new List<double>();

            string strTokens = termList[0];
            double fWeight = weightList[0];
            for (int i = 1; i < termList.Count; i++)
            {
                if (weightList[i] == fWeight)
                {
                    strTokens = strTokens + termList[i];
                }
                else
                {
                    wbTokens.Clear();
                    wordseg.Segment(strTokens, wbTokens, false);
                    foreach (WordSeg.Token token in wbTokens.tokenList)
                    {
                        mTermList.Add(token.strTerm);
                        mWeightList.Add(fWeight);
                    }
                    strTokens = termList[i];
                    fWeight = weightList[i];
                }
            }

            wbTokens.Clear();
            wordseg.Segment(strTokens, wbTokens, false);
            foreach (WordSeg.Token token in wbTokens.tokenList)
            {
                mTermList.Add(token.strTerm);
                mWeightList.Add(fWeight);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("BuildQueryTermWeightOrderCorpus [wordseg lexical dict file] [input file name] [output file name]");
                return;
            }

            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(args[0], true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);

            StreamReader sr = new StreamReader(args[1]);
            StreamWriter sw = new StreamWriter(args[2]);

            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine().Trim();
                string[] items = strLine.Split('\t');
                if (items.Length < 3)
                {
                    continue;
                }

                int freq = int.Parse(items[1]);
                List<string> termList = new List<string>();
                List<double> weightList = new List<double>();
                HashSet<double> setScore = new HashSet<double>();
                double maxWeight = -1.0;
                double minWeight = 2.0;

                for (int i = 2; i < items.Length; i++)
                {
                    int pos = items[i].LastIndexOf('[');
                    string strTerm = items[i].Substring(0, pos).Trim();
                    if (strTerm.Length == 0)
                    {
                        maxWeight = minWeight = -1.0;
                        break;
                    }

                    double fWeight = double.Parse(items[i].Substring(pos + 1, items[i].Length - pos - 2));
                    if (fWeight >= maxWeight)
                    {
                        maxWeight = fWeight;
                    }
                    if (fWeight <= minWeight)
                    {
                        minWeight = fWeight;
                    }
                    setScore.Add(fWeight);

                    termList.Add(strTerm);
                    weightList.Add(fWeight);
                }

                if (setScore.Count == 0)
                {
                    continue;
                }

                List<double> rankedScoreList = new List<double>();
                foreach (double item in setScore)
                {
                    rankedScoreList.Add(item);
                }
                rankedScoreList.Sort();
                double minGap = 2.0;
                double lastScore = rankedScoreList[0];
                for (int i = 1; i < rankedScoreList.Count; i++)
                {
                    if (Math.Abs(lastScore - rankedScoreList[i]) < minGap)
                    {
                        minGap = Math.Abs(lastScore - rankedScoreList[i]);
                    }
                    lastScore = rankedScoreList[i];
                }

                if (minGap > 1.0 || minGap < 0.05 || termList.Count == 0 || termList.Count != weightList.Count)
                {
                    continue;
                }

                List<string> mTermList;
                List<double> mWeightList;
                MergeTokens(termList, weightList, out mTermList, out mWeightList);
                termList = mTermList;
                weightList = mWeightList;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < termList.Count - 1; i++)
                {
                    sb.Append(termList[i]);
                    sb.Append("[");
                    if (weightList[i] > weightList[i + 1])
                    {
                        sb.Append(">");
                    }
                    else if (weightList[i] < weightList[i + 1])
                    {
                        sb.Append("<");
                    }
                    else
                    {
                        sb.Append("=");
                    }
                    sb.Append("] ");
                }

                sb.Append(termList[termList.Count - 1]);
                sb.Append("[=]");

                sw.WriteLine(sb.ToString());

                //int logFreq = (int)Math.Log10(freq);
                //if (logFreq == 0)
                //{
                //    logFreq++;
                //}
                //while (logFreq > 0)
                //{
                //    sw.WriteLine(sb.ToString());
                //    logFreq--;
                //}
            }
            sr.Close();
            sw.Close();

        }
    }
}
