using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class WordFormationFeature : IFeature
    {
        public string GetName()
        {
            return "WordFormation";
        }

        public string GetValue(FeatureContext context)
        {
            return context.tknList[context.index].rankId.ToString();
        }
    }
}
