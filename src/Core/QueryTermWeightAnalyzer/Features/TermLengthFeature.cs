using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class TermLengthFeature : IFeature
    {
        public string GetName()
        {
            return "TermLength";
        }

        public float GetValue(FeatureContext context)
        {
            return context.tknList[context.index].strTerm.Length;
        }
    }
}
