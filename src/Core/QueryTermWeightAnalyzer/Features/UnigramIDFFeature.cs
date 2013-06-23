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
            if (context.unigramDict.ContainsKey(strTerm) == true)
            {
                return context.unigramDict[strTerm].idf.ToString();
            }

            return context.maxIDF.ToString();
        }
    }
}
