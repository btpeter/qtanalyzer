using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using QueryTermWeightAnalyzer;

namespace QueryTermWeightAnalyzerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("QueryTermWeightAnalyzerConsole.exe [configuration file name] [input file name] [output file name]");
                return;
            }

            QueryTermWeightAnalyzer.QueryTermWeightAnalyzer analyzer = new QueryTermWeightAnalyzer.QueryTermWeightAnalyzer();
            analyzer.Initialize(args[0]);

            StreamReader sr = new StreamReader(args[1]);
            StreamWriter sw = new StreamWriter(args[2]);
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                List<Token> tknList;
                tknList = analyzer.Analyze(strLine);

                string strOutput = "";
                foreach (Token token in tknList)
                {
                    strOutput += token.strTerm + "[RANK_" + token.rankId.ToString() + "] ";
                }
                sw.WriteLine(strOutput.Trim());
            }
            sr.Close();
            sw.Close();
        }
    }
}
