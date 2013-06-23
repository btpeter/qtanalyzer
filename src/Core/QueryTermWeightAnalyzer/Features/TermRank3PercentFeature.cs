using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class TermRank3PercentFeature : IFeature
    {
        public string GetName()
        {
            return "TermRank3Percent";
        }

        public string GetValue(FeatureContext context)
        {
            string strTerm = context.tknList[context.index].strTerm;
            if (context.unigramDict.ContainsKey(strTerm) == true)
            {
                return context.unigramDict[strTerm].pRank3.ToString();
            }

            return "0";
        }
    }
}
