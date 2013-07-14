using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WordSeg;
using AdvUtils;

namespace BuildLanguageModel
{
    class Program
    {
        static WordSeg.WordSeg wordseg;
        static WordSeg.Tokens wbTokens;

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("BuildLanguageModel.exe [lexical dictionary] [input file] [output file]");
                return;
            }

            wordseg = new WordSeg.WordSeg();
            //Load lexical dictionary
            wordseg.LoadLexicalDict(args[0], true);
            //Initialize word breaker's token instance
            wbTokens = wordseg.CreateTokens(1024);

            StreamReader sr = new StreamReader(args[1], Encoding.UTF8);
            string strLine = null;
            BigDictionary<string, int> bigram = new BigDictionary<string, int>();
            BigDictionary<string, int> unigram = new BigDictionary<string, int>();
            BigDictionary<string, int> unigramDF = new BigDictionary<string, int>();
            long queryCnt = 0;
            long docCnt = 0;
            while ((strLine = sr.ReadLine()) != null)
            {
                string[] items = strLine.Split('\t');
                string strQuery = items[0].ToLower().Trim();
                int freq = int.Parse(items[2]);
                queryCnt += freq;
                docCnt++;
                HashSet<string> setTerm = new HashSet<string>();

                wordseg.Segment(strQuery, wbTokens, false);
                for (int i = 0; i < wbTokens.tokenList.Count; i++)
                {
                    string strTerm = wbTokens.tokenList[i].strTerm.Trim().Replace(" ", "");
                    if (strTerm.Length == 0)
                    {
                        continue;
                    }

                    if (unigram.ContainsKey(strTerm) == false)
                    {
                        unigram.Add(strTerm, freq);
                    }
                    else
                    {
                        unigram[strTerm] += freq;
                    }

                    if (setTerm.Contains(strTerm) == false)
                    {
                        setTerm.Add(strTerm);
                        if (unigramDF.ContainsKey(strTerm) == false)
                        {
                            unigramDF.Add(strTerm, 1);
                        }
                        else
                        {
                            unigramDF[strTerm]++;
                        }
                    }

                    if (i < wbTokens.tokenList.Count - 1)
                    {
                        string strTerm2 = wbTokens.tokenList[i + 1].strTerm;
                        string strBigram = strTerm + " " + strTerm2;
                        if (bigram.ContainsKey(strBigram) == false)
                        {
                            bigram.Add(strBigram, freq);
                        }
                        else
                        {
                            bigram[strBigram] += freq;
                        }
                    }
                }
            }
            sr.Close();

            //Save unigram data
            string strUnigramFileName = args[2] + ".uni";
            StreamWriter sw_unigramData = new StreamWriter(strUnigramFileName, false, Encoding.UTF8, 102400);
       
            //Build double array trie-tree key and value pairs
            SortedDictionary<string, int> unigramDict_da = new SortedDictionary<string, int>(StringComparer.Ordinal);
            int da_unigram_cnt = 0;
            foreach (KeyValuePair<string, int> pair in unigram)
            {
                if (unigramDF[pair.Key] > 1)
                {
                    double idf = (double)docCnt / (double)(unigramDF[pair.Key]);
                    idf = Math.Log(idf, 2.0);
                    sw_unigramData.WriteLine("{0}\t{1}", pair.Value, idf);
                    unigramDict_da.Add(pair.Key, da_unigram_cnt);
                    da_unigram_cnt++;
                }
            }
            sw_unigramData.Close();

            DoubleArrayTrieBuilder da_unigram = new DoubleArrayTrieBuilder(4);
            da_unigram.build(unigramDict_da);
            da_unigram.save(strUnigramFileName + ".da");


            //Calculate MI
            BigDictionary<string, double> miDict = new BigDictionary<string, double>();
            double minMI = 10000000.0;
            foreach (KeyValuePair<string, int> pair in bigram)
            {
                string[] terms = pair.Key.Split(' ');

                if (terms.Length != 2)
                {
                    Console.WriteLine("Invalidated bigram: {0}", pair.Key);
                    continue;
                }
                if (unigram.ContainsKey(terms[0]) == false)
                {
                    Console.WriteLine("{0} has no unigram freq in Line {1}", terms[0], pair.Key);
                    continue;
                }
                if (unigram.ContainsKey(terms[1]) == false)
                {
                    Console.WriteLine("{0} has no unigram freq in Line {1}", terms[1], pair.Key);
                    continue;
                }

                long freq1 = unigram[terms[0]];
                long freq2 = unigram[terms[1]];
                long freqBigram = pair.Value;
                double mi = (double)(queryCnt * freqBigram) / (double)(freq1 * freq2);
                mi = Math.Log(mi, 2.0);
                if (mi < minMI)
                {
                    minMI = mi;
                }

                miDict.Add(pair.Key, mi);
            }

            //Save bigram data
            string strBigramFileName = args[2] + ".bi";
            StreamWriter sw_bigramData = new StreamWriter(strBigramFileName, false, Encoding.UTF8, 102400);
            SortedDictionary<string, int> bigramDict_da = new SortedDictionary<string, int>(StringComparer.Ordinal);
            int da_bigram_cnt = 0;
            foreach (KeyValuePair<string, int> pair in bigram)
            {
                if (pair.Value > 2)
                {
                    double mi = minMI;
                    if (miDict.ContainsKey(pair.Key) == true)
                    {
                        mi = miDict[pair.Key];
                    }
                    sw_bigramData.WriteLine("{0}\t{1}", pair.Value, mi);

                    bigramDict_da.Add(pair.Key, da_bigram_cnt);
                    da_bigram_cnt++;
                }
            }
            sw_bigramData.Close();


            DoubleArrayTrieBuilder da_bigram = new DoubleArrayTrieBuilder(4);
            da_bigram.build(bigramDict_da);
            da_bigram.save(strBigramFileName + ".da");

            Console.WriteLine("Total query: {0}", queryCnt);

        }
    }
}
