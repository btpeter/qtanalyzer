using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ConvertRawTrainingCorpusToCRFSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("ConvertRawTrainingCorpusToCRFSharp.exe [input filename] [output filename]");
                return;
            }

            StreamReader sr = new StreamReader(args[0]);
            StreamWriter sw = new StreamWriter(args[1]);

            Dictionary<string, int> tag2num = new Dictionary<string, int>();
            while (sr.EndOfStream == false)
            {
                string strLine = sr.ReadLine();
                string[] items = strLine.Split();
                foreach (string item in items)
                {
                    int pos = item.IndexOf('[');
                    string strTerm = item.Substring(0, pos);
                    string strTag = item.Substring(pos + 1, item.Length - pos - 2);
                    sw.WriteLine("{0}\tS_{1}", strTerm, strTag);

                    if (tag2num.ContainsKey(strTag) == false)
                    {
                        tag2num.Add(strTag, 0);
                    }
                    tag2num[strTag]++;
                }
                sw.WriteLine();
            }

            foreach (KeyValuePair<string, int> pair in tag2num)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

            sr.Close();
            sw.Close();
        }
    }
}
