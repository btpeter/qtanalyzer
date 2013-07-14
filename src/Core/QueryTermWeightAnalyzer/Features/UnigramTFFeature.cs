using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class UnigramTFFeature : IFeature
    {
        public string GetName()
        {
            return "UnigramTF";
        }

        public string GetValue(FeatureContext context)
        {
            string strTerm = context.tknList[context.index].strTerm;
            int idx = context.unigram_da.SearchByPerfectMatch(strTerm);
            if (idx >= 0)
            {
                return context.unigramList[idx].freq.ToString();
            }

            return "0";
        }
    }
}
