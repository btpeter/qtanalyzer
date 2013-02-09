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

                if (tknList.Count > 0)
                {
                    string strOutput = tknList[0].strTerm;
                    string strTag = tknList[0].strTag;
                    for (int i = 1; i < tknList.Count; i++)
                    {
                        if (tknList[i].strTag != strTag)
                        {
                            strOutput += "[" + strTag + "] ";
                            strTag = tknList[i].strTag;
                        }
                        strOutput += tknList[i].strTerm;
                    }
                    strOutput += "[" + strTag + "] ";

                    sw.WriteLine(strOutput.Trim());
                }
            }
            sr.Close();
            sw.Close();
        }
    }
}
