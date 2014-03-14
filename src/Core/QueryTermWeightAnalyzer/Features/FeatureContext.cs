using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdvUtils;
using LMDecoder;

namespace QueryTermWeightAnalyzer.Features
{
    public class FeatureContext
    {
        public int index;
        public List<Token> tknList;
        public Dictionary<string, RankXDist> term2rankDist;
        public HashSet<string> setPunct;
        public KNDecoder lmDecoder;
    }
}
