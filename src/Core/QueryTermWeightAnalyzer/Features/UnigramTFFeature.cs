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
            if (context.unigramDict.ContainsKey(strTerm) == true)
            {
                return context.unigramDict[strTerm].freq.ToString();
            }

            return "0";
        }
    }
}
