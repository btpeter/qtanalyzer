using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QueryTermSynonymAnalyzer;

namespace QueryTermSynAnalyzerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            QueryTermSynonymAnalyzer.QueryTermSynonymAnalyzer qts = new QueryTermSynonymAnalyzer.QueryTermSynonymAnalyzer("LexicalDict_zh-cn_empty.txt", "term_syn_pattern.txt", "model_zh-cn_ngram_5");

            Console.WriteLine("Ready...");
            while (true)
            {
                string strQuery = Console.ReadLine();
                string strSynTerm = Console.ReadLine();

                int pos = strQuery.IndexOf(strSynTerm);
                if (pos < 0)
                {
                    Console.WriteLine("Invalidated term");
                    continue;
                }

                List<SynResult> rstDict = qts.GetSynonym(strQuery, pos, strSynTerm.Length);

                foreach (SynResult rst in rstDict)
                {
                    Console.WriteLine("{0}\t{1}\t{2}\t{3}", rst.strTerm, rst.lmScore, rst.lmScore_rnn, rst.llr);
                }
            }

        }
    }
}
