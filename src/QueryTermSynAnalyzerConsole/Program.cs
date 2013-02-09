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
            QueryTermSynonymAnalyzer.QueryTermSynonymAnalyzer qts = new QueryTermSynonymAnalyzer.QueryTermSynonymAnalyzer("LexicalDict.txt", "syn_zh_cn.txt");

            while (true)
            {
                string str = "顺丰物流电话";
                Dictionary<string, List<string>> rstDict = qts.GetSynonym(str, 3, 2);
            }

        }
    }
}
