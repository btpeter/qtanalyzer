using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryTermWeightAnalyzer.Features
{
    //class PMIInRightFeature : IFeature
    //{
    //    public string GetName()
    //    {
    //        return "PMIInRight";
    //    }

    //    public string GetValue(FeatureContext context)
    //    {
    //        if (context.index < context.tknList.Count - 1)
    //        {
    //            string strTerm1 = context.tknList[context.index].strTerm;
    //            string strTerm2 = context.tknList[context.index + 1].strTerm;
    //            string strBigram = strTerm1 + " " + strTerm2;
    //            int idx = context.bigram_da.SearchByPerfectMatch(strBigram);
    //            if (idx >= 0)
    //            {
    //                return context.bigramList[idx].mi.ToString();
    //            }
    //        }
    //        return "0";
    //    }
    //}
}
