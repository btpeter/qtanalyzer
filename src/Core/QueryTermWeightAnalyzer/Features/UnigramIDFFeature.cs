using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class UnigramIDFFeature : IFeature
    {
        public string GetName()
        {
            return "UnigramIDF";
        }

        public string GetValue(FeatureContext context)
        {
            string strTerm = context.tknList[context.index].strTerm;
            int idx = context.unigram_da.SearchByPerfectMatch(strTerm);
            if (idx >= 0)
            {
                return context.unigramList[idx].idf.ToString();
            }

            return context.maxIDF.ToString();
        }
    }
}
