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
            if (context.term2rankDist.ContainsKey(strTerm) == true)
            {
                return context.term2rankDist[strTerm].pRank4.ToString();
            }

            return "0";
        }
    }
}
