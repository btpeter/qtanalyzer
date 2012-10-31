using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WordSeg;

namespace BuildNGram
{
    class Program
    {
        static WordSeg.WordSeg wordseg;
        static WordSeg.Tokens wbTokens;
      
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("BuildNGram [Lexical dictionary file name] [Query term weight score file name] [Unigram file name] [Bigram file name]");
                return;
            }

            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(args[0], true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);


            StreamReader sr = new StreamReader(args[1], Encoding.UTF8);
            StreamWriter swUnigram = new StreamWriter(args[2], false, Encoding.UTF8);
            StreamWriter swBigram = new StreamWriter(args[3], false, Encoding.UTF8);

            Dictionary<string, int> unigram = new Dictionary<string, int>();
            Dictionary<string, int> term2min = new Dictionary<string, int>();
            Dictionary<string, int> bigram = new Dictionary<string, int>();

            //Generate Unigram and Bigram data
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split('\t');
                string strQuery = items[0].Replace(" ", "");
                int queryFreq = int.Parse(items[1]);

                wordseg.Segment(strQuery, wbTokens, false);

                try
                {
                    for (int i = 0; i < wbTokens.tokenList.Count; i++)
                    {
                        if (unigram.ContainsKey(wbTokens.tokenList[i].strTerm) == false)
                        {
                            unigram.Add(wbTokens.tokenList[i].strTerm, 0);
                        }
                        unigram[wbTokens.tokenList[i].strTerm] += queryFreq;


                        if (i < wbTokens.tokenList.Count - 1)
                        {
                            string strBigram;
                            strBigram = wbTokens.tokenList[i].strTerm + " " + wbTokens.tokenList[i + 1].strTerm;
                            if (bigram.ContainsKey(strBigram) == false)
                            {
                                bigram.Add(strBigram, 0);
                            }
                            bigram[strBigram] += queryFreq;

                            strBigram = wbTokens.tokenList[i + 1].strTerm + " " + wbTokens.tokenList[i].strTerm;
                            if (bigram.ContainsKey(strBigram) == false)
                            {
                                bigram.Add(strBigram, 0);
                            }
                            bigram[strBigram] += queryFreq;
                        }
                    }

                }
                catch (Exception err)
                {
                    Console.WriteLine("Invalidated sentence: {0}", strLine);
                    Console.WriteLine("Message: {0}", err.Message);
                    Console.WriteLine("Call stack: {0}", err.StackTrace);
                }
            }

            foreach (KeyValuePair<string, int> pair in unigram)
            {
                swUnigram.WriteLine("{0}\t{1}", pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, int> pair in bigram)
            {
                swBigram.WriteLine("{0}\t{1}", pair.Key, pair.Value);
            }

            sr.Close();
            swUnigram.Close();
            swBigram.Close();
        }
    }
}
