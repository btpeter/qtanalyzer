using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WordSeg;
using CRFSharpWrapper;
using QueryTermWeightAnalyzer.Features;

namespace QueryTermWeightAnalyzer
{
    public class Instance
    {
        public WordSeg.Tokens wbTokens;
        public SegDecoderTagger crf_tag;
        public crf_out crf_out;
        public float[] ftrList;
        public FeatureContext context;
    }
}
