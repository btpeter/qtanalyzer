using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    interface IFeature
    {
        string GetName();
        float GetValue(FeatureContext context);
    }
}
