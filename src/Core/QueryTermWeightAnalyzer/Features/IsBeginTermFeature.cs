using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class IsBeginTermFeature : IFeature
    {
        public string GetName()
        {
            return "IsBeginTerm";
        }

        public float GetValue(FeatureContext context)
        {
            if (context.index == 0)
            {
                return 1;
            }

            return 0;
        }
    }
}
