using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    class LanguageModelFeature : IFeature
    {
        string IFeature.GetName()
        {
            return "LanguageModel";
        }

        string IFeature.GetValue(FeatureContext context)
        {
            //Generate raw text token list
            StringBuilder sb = new StringBuilder();
            foreach (Token token in context.tknList)
            {
                sb.Append(token.strTerm);
                sb.Append(" ");
            }

            LMDecoder.LMResult lmResultRaw = context.lmDecoder.GetSentProb(sb.ToString().Trim(), 4);

            //Generate text token list without center term 
            sb.Clear();
            for (int i = 0; i < context.tknList.Count; i++)
            {
                if (i != context.index)
                {
                    sb.Append(context.tknList[i].strTerm);
                    sb.Append(" ");
                }
            }

            LMDecoder.LMResult lmResultNoTerm = context.lmDecoder.GetSentProb(sb.ToString().Trim(), 4);

            double ratio = lmResultNoTerm.perplexity / lmResultRaw.perplexity;

            return ratio.ToString();
        }
    }
}
