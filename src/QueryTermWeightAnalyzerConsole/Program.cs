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

            if (File.Exists(args[0]) == false)
            {
                Console.WriteLine("Configuration file is not existed: {0}", args[0]);
                return;
            }

            if (File.Exists(args[1]) == false)
            {
                Console.WriteLine("Input file is not existed: {0}", args[1]);
                return;
            }

            QueryTermWeightAnalyzer.QueryTermWeightAnalyzer analyzer = new QueryTermWeightAnalyzer.QueryTermWeightAnalyzer();
            if (analyzer.Initialize(args[0]) == false)
            {
                Console.WriteLine("Initialize the analyzer failed.");
                return;
            }

            StreamReader sr = new StreamReader(args[1]);
            StreamWriter sw = new StreamWriter(args[2]);
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();

                List<Token> tknList;
                tknList = analyzer.Analyze(strLine);
                if (tknList == null)
                {
                    //Analyze term weight is failed.
                    Console.WriteLine("Failed to analyze {0}", strLine);
                    continue;
                }

                string strOutput = "";
                foreach (Token token in tknList)
                {
                    strOutput += token.strTerm + "[RANK_" + token.rankId.ToString() + ", "+ token.rankingscore.ToString("0.00") +"] ";
                }
                sw.WriteLine(strOutput.Trim());
            }
            sr.Close();
            sw.Close();
        }
    }
}
