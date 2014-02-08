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
        public DoubleArrayTrieSearch unigram_da;
   //     public DoubleArrayTrieSearch bigram_da;
        public List<Unigram> unigramList;
      //  public List<Bigram> bigramList;
        public HashSet<string> setPunct;
        public double maxIDF;
    }
}
