using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class TermRank4PercentFeature : IFeature
    {
        public string GetName()
        {
            return "TermRank4Percent";
        }

        public string GetValue(FeatureContext context)
        {
            string strTerm = context.tknList[context.index].strTerm;
            int idx = context.unigram_da.SearchByPerfectMatch(strTerm);
            if (idx >= 0)
            {
                return context.unigramList[idx].pRank4.ToString();
            }

            return "0";
        }
    }
}
