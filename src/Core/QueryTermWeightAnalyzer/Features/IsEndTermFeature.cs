using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class IsEndTermFeature : IFeature
    {
        public string GetName()
        {
            return "IsEndTerm";
        }

        public string GetValue(FeatureContext context)
        {
            if (context.index + 1 == context.tknList.Count)
            {
                return "1";
            }
            return "0";
        }
    }
}
