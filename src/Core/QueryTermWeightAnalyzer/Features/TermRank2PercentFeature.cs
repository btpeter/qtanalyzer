using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class TermRank2PercentFeature : IFeature
    {
        public string GetName()
        {
            return "TermRank2Percent";
        }

        public string GetValue(FeatureContext context)
        {
            string strTerm = context.tknList[context.index].strTerm;
            if (context.term2rankDist.ContainsKey(strTerm) == true)
            {
                return context.term2rankDist[strTerm].pRank2.ToString();
            }

            return "0";
        }
    }
}
