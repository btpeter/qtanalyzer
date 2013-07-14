using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class IsPunctFeature : IFeature
    {
        public string GetName()
        {
            return "IsPunct";
        }

        public string GetValue(FeatureContext context)
        {
            int idx = context.index;
            string strTerm = context.tknList[idx].strTerm;

            if (context.setPunct.Contains(strTerm) == true)
            {
                return "1.0";
            }

            return "0.0";
        }
    }
}
