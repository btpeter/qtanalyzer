using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdvUtils;

namespace QueryTermWeightAnalyzer.Features
{
    class FeatureContext
    {
        public int index;
        public List<Token> tknList;
        public BigDictionary<string, Unigram> unigramDict;
        public BigDictionary<string, Bigram> bigramDict;
        public HashSet<string> setPunct;
        public double maxIDF;
    }
}
