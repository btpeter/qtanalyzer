using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class TermOffsetFeature : IFeature
    {
        public string GetName()
        {
            return "TermOffset";
        }

        public float GetValue(FeatureContext context)
        {
            return context.index;
        }
    }
}
