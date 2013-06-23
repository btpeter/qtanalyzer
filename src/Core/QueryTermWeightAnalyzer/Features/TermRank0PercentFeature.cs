using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class TermRank0PercentFeature : IFeature
    {
        public string GetName()
        {
            return "TermRank0Percent";
        }

        public string GetValue(FeatureContext context)
        {
            string strTerm = context.tknList[context.index].strTerm;
            if (context.unigramDict.ContainsKey(strTerm) == true)
            {
                return context.unigramDict[strTerm].pRank0.ToString();
            }

            return "0";
        }
    }
}
