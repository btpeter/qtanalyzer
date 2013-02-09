using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdvUtils;

namespace ClusterUrl
{
    class UrlEntity : IComparable<UrlEntity>
    {
        public string strUrl;
        public List<QueryItem> featureSet;


        public int CompareTo(UrlEntity other)
        {
            return featureSet.Count - other.featureSet.Count;
        }
    }

    class QueryItem
    {
        public int qid;
        public int weight;
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("ClusterUrl [Query Url Freq file name] [Query ClusterId Freq file name] [Min Query Url Frequency]");
                return;
            }

            StreamReader sr = new StreamReader(args[0]);
            int maxQueryId = 0;
            BigDictionary<string, int> query2Id = new BigDictionary<string, int>();
            BigDictionary<int, string> id2query = new BigDictionary<int,string>();

            VarBigArray<UrlEntity> entityList = new VarBigArray<UrlEntity>(1024);
            long entityListSize = 0;

            Console.WriteLine("Loading url set and building feature set...");
            int maxLine = 0;
            int minFreq = int.Parse(args[2]);
            string strLine = null;
            while ((strLine = sr.ReadLine()) != null)
            {
                //Query \t Url \t Freq
                string[] items = strLine.Split('\t');
                items[0] = items[0].ToLower().Trim();
                items[1] = items[1].ToLower().Trim();

                string strUrl = items[1];
                int weight = int.Parse(items[2]);
                if (weight < minFreq)
                {
                    continue;
                }

                if (maxLine % 100000 == 0)
                {
                    Console.WriteLine("Line No. {0} Max Query Id: {1}", maxLine, maxQueryId);
                }
                maxLine++;

                if (query2Id.ContainsKey(items[0]) == false)
                {
                    query2Id.Add(items[0], maxQueryId);
                    id2query.Add(maxQueryId, items[0]);
                    maxQueryId++;
                }

                

                QueryItem qi = new QueryItem();
                qi.qid = query2Id[items[0]];
                qi.weight = weight;

                if (entityListSize == 0)
                {
                    UrlEntity entity = new UrlEntity();
                    entity.strUrl = strUrl;
                    entity.featureSet = new List<QueryItem>();
                    entity.featureSet.Add(qi);
                    entityList[entityListSize] = entity;
                    entityListSize++;
                }
                else if (entityList[entityListSize - 1].strUrl == strUrl)
                {
                    entityList[entityListSize - 1].featureSet.Add(qi);
                }
                else
                {
                    UrlEntity entity = new UrlEntity();
                    entity.strUrl = strUrl;
                    entity.featureSet = new List<QueryItem>();
                    entity.featureSet.Add(qi);
                    entityList[entityListSize] = entity;
                    entityListSize++;
                }

            }
            sr.Close();

            Console.WriteLine("Clustring urls...");
            entityList = RemoveUrlsWithLowQualityFeature(entityList, ref entityListSize);
            entityList = ClusterUrlsWithSameFeatures(entityList, ref entityListSize);


            Console.WriteLine("Saving query clustered_id freq file ordered by clustered id...");
            StreamWriter sw = new StreamWriter(args[1]);

            for (long i = 0;i < entityListSize;i++)
            {
                UrlEntity item = entityList[i];
                foreach (QueryItem qi in item.featureSet)
                {
                    sw.WriteLine("{0}\t{1}\t{2}", id2query[qi.qid], item.strUrl, qi.weight);
                }
            }
            
            sw.Close();
        }

        //If two urls have the same cliked query set, these two urls should be merged as one url
        private static VarBigArray<UrlEntity> ClusterUrlsWithSameFeatures(VarBigArray<UrlEntity> entityList, ref long entityListSize)
        {
            BigDictionary<string, UrlEntity> key2entity = new BigDictionary<string, UrlEntity>();
            for (long k = 0;k < entityListSize;k++)
            {
                UrlEntity item = entityList[k];
                //Generate qid-key
                StringBuilder sb = new StringBuilder();
                foreach (QueryItem qi in item.featureSet)
                {
                    sb.Append(qi.qid.ToString());
                    sb.Append("_");
                }

                //Try to match
                if (key2entity.ContainsKey(sb.ToString()) == false)
                {
                    key2entity.Add(sb.ToString(), item);
                }
                else
                {
                    for (int i = 0; i < item.featureSet.Count; i++)
                    {
                        if (key2entity[sb.ToString()].featureSet[i].qid != item.featureSet[i].qid)
                        {
                            Console.WriteLine("Key is inconsistent!");
                            entityListSize = 0;
                            return null;
                        }
                        key2entity[sb.ToString()].featureSet[i].weight += item.featureSet[i].weight;
                    }
                }
            }

            VarBigArray<UrlEntity> rst = new VarBigArray<UrlEntity>(1024);
            long rstSize = 0;
            foreach (KeyValuePair<string, UrlEntity> pair in key2entity)
            {
                rst[rstSize] = pair.Value;
                rstSize++;
            }
            rst.Sort(0, rstSize);
            entityListSize = rstSize;
            return rst;
        }

        //If the url is clicked by only one query, OR 
        //(the url is a directionary, not a page, and it is cliked by more than 5 unique queries),
        //the url should be dropped.
        private static VarBigArray<UrlEntity> RemoveUrlsWithLowQualityFeature(VarBigArray<UrlEntity> entityList, ref long entityListSize)
        {
            VarBigArray<UrlEntity> entityList2 = new VarBigArray<UrlEntity>(1024);
            long entityList2Size = 0;

            for (long i = 0;i < entityListSize;i++)
            {
                UrlEntity item = entityList[i];
                //If too small or many unique query point to a special query, ignore this url
                if (item.featureSet.Count <= 1 || item.featureSet.Count > 500)
                {
                    continue;
                }

                item.featureSet.Sort((x, y) => x.qid - y.qid);
                entityList2[entityList2Size] = item;
                entityList2Size++;
            }
            entityListSize = entityList2Size;

            return entityList2;
        }
    }
}
