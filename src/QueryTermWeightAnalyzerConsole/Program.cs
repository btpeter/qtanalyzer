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
            if (args.Length != 3 && args.Length != 1)
            {
                Console.WriteLine("QueryTermWeightAnalyzerConsole.exe [configuration file name] <input file name> <output file name>");
                Console.WriteLine("     [configuration file name] : a specified file name contains configuration items for analyzing");
                Console.WriteLine("     <input/output file name> : input/output file name contains input query for analyzing and output result");
                Console.WriteLine("Examples:");
                Console.WriteLine("     QueryTermWeightAnalyzerConsole.exe qt_analyzer.ini input.txt output.txt");
                Console.WriteLine("         Load queries from input.txt file, analyze and save result into output.txt file");
                Console.WriteLine("     QueryTermWeightAnalyzerConsole.exe qt_analyzer.ini");
                Console.WriteLine("         Load queries from console, analyze and output result to console");
                return;
            }

            if (File.Exists(args[0]) == false)
            {
                Console.WriteLine("Configuration file is not existed: {0}", args[0]);
                return;
            }

            StreamReader sr = null;
            StreamWriter sw = null;
            
            if (args.Length == 3)
            {
                if (File.Exists(args[1]) == false)
                {
                    Console.WriteLine("Input file {0} is not existed.", args[1]);
                    return;
                }
                sr = new StreamReader(args[1], Encoding.UTF8);
                sw = new StreamWriter(args[2], false, Encoding.UTF8);
            }
            else if (args.Length != 1)
            {
                Console.WriteLine("Invalidated parameters.");
                return;
            }

            Console.WriteLine("Start to initialize query term weight analyzer...");
            QueryTermWeightAnalyzer.QueryTermWeightAnalyzer analyzer = new QueryTermWeightAnalyzer.QueryTermWeightAnalyzer();
            if (analyzer.Initialize(args[0]) == false)
            {
                Console.WriteLine("Initialize the analyzer failed.");
                return;
            }
            Console.WriteLine("Done.");

            //Create working instance for each thread
            Instance instance = analyzer.CreateInstance();

            while (true)
            {
                string strLine = null;
                if (sr != null)
                {
                    strLine = sr.ReadLine();
                }
                else
                {
                    strLine = Console.ReadLine();
                }

                if (strLine == null || strLine.ToLower() == "quit")
                {
                    break;
                }

                List<Token> tknList;
                string[] columns = strLine.Split('\t');

                tknList = analyzer.Analyze(instance, columns[0]);
                if (tknList == null)
                {
                    //Analyze term weight is failed.
                    Console.WriteLine("Failed to analyze {0}", columns[0]);
                    continue;
                }

                string strOutput = "";
                foreach (Token token in tknList)
                {
                    strOutput += token.strTerm + "[RANK_" + token.rankId.ToString() + ", "+ token.rankingscore.ToString("0.00") +"] ";
                }

                if (sw != null)
                {
                    sw.WriteLine(strOutput.Trim());
                }
                else
                {
                    Console.WriteLine(strOutput.Trim());
                }
            }

            if (sr != null)
            {
                sr.Close();
            }

            if (sw != null)
            {
                sw.Close();
            }
        }
    }
}
