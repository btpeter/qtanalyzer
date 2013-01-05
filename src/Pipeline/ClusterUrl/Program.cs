using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterUrl
{
    class UrlEntity
    {
        public string strUrl;
        public List<QueryItem> featureSet;
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
            Dictionary<string, int> query2Id = new Dictionary<string, int>();
            Dictionary<int, string> id2query = new Dictionary<int,string>();

            List<UrlEntity> entityList = new List<UrlEntity>();

            Console.WriteLine("Loading url set and building feature set...");
            int maxLine = 0;
            int minFreq = int.Parse(args[2]);

            while (sr.EndOfStream == false)
            {
                //Query \t Url \t Freq
                string strLine = sr.ReadLine();
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

                if (entityList.Count == 0)
                {
                    UrlEntity entity = new UrlEntity();
                    entity.strUrl = strUrl;
                    entity.featureSet = new List<QueryItem>();
                    entity.featureSet.Add(qi);
                    entityList.Add(entity);
                }
                else if (entityList[entityList.Count - 1].strUrl == strUrl)
                {
                    entityList[entityList.Count - 1].featureSet.Add(qi);
                }
                else
                {
                    UrlEntity entity = new UrlEntity();
                    entity.strUrl = strUrl;
                    entity.featureSet = new List<QueryItem>();
                    entity.featureSet.Add(qi);
                    entityList.Add(entity);
                }

            }
            sr.Close();

            Console.WriteLine("Clustring urls...");
            //entityList = Clustring(entityList);
            entityList = RemoveUrlsWithLowQualityFeature(entityList);
            entityList = ClusterUrlsWithSameFeatures(entityList);


            Console.WriteLine("Saving query clustered_id freq file ordered by clustered id...");
            StreamWriter sw = new StreamWriter(args[1]);
            foreach (UrlEntity item in entityList)
            {
                foreach (QueryItem qi in item.featureSet)
                {
                    sw.WriteLine("{0}\t{1}\t{2}", id2query[qi.qid], item.strUrl, qi.weight);
                }
            }
            
            sw.Close();
        }

        private static double GetSimilairyScore(List<QueryItem> qi1, List<QueryItem> qi2)
        {
            int i = 0, j = 0;
            int joinCnt = 0;
            int qi1Size = qi1.Count;
            int qi2Size = qi2.Count;
            while (i < qi1Size && j < qi2Size)
            {
                if (qi1[i].qid < qi2[j].qid)
                {
                    i++;
                }
                else if (qi1[i].qid > qi2[j].qid)
                {
                    j++;
                }
                else
                {
                    i++; j++;
                    joinCnt++;
                }
            }

            if (joinCnt == 0)
            {
                return 0.0;
            }

            return (double)joinCnt / (double)(qi1Size + qi2Size - joinCnt);
        }

        private static List<QueryItem> MergeQueryItem(List<QueryItem> qiList)
        {
            Dictionary<int, int> qid2weight = new Dictionary<int, int>();
            foreach (QueryItem item in qiList)
            {
                if (qid2weight.ContainsKey(item.qid) == false)
                {
                    qid2weight.Add(item.qid, 0);
                }
                qid2weight[item.qid] += item.weight;
            }

            List<QueryItem> rst = new List<QueryItem>();
            foreach (KeyValuePair<int, int> pair in qid2weight)
            {
                QueryItem qi = new QueryItem();
                qi.qid = pair.Key;
                qi.weight = pair.Value;

                rst.Add(qi);
            }

            rst.Sort((x, y) => x.qid - y.qid);

            return rst;
        }

        private static List<UrlEntity> ClusterUrlsWithSameFeatures(List<UrlEntity> entityList)
        {
            Dictionary<string, UrlEntity> key2entity = new Dictionary<string, UrlEntity>();
            foreach (UrlEntity item in entityList)
            {
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
                            return null;
                        }
                        key2entity[sb.ToString()].featureSet[i].weight += item.featureSet[i].weight;
                    }
                }
            }

            List<UrlEntity> rst = new List<UrlEntity>();
            foreach (KeyValuePair<string, UrlEntity> pair in key2entity)
            {
                rst.Add(pair.Value);
            }

            rst.Sort((x, y) => x.featureSet.Count - y.featureSet.Count);

            return rst;
        }

        private static List<UrlEntity> RemoveUrlsWithLowQualityFeature(List<UrlEntity> entityList)
        {
            List<UrlEntity> entityList2 = new List<UrlEntity>();
            foreach (UrlEntity item in entityList)
            {
                if (item.featureSet.Count <= 1 || (item.strUrl.EndsWith("/") == true && item.featureSet.Count > 5))
                {
                    continue;
                }

                item.featureSet.Sort((x, y) => x.qid - y.qid);
                entityList2.Add(item);
            }
            return entityList2;
        }

        private static List<UrlEntity> Clustring(List<UrlEntity> entityList)
        {
            entityList = RemoveUrlsWithLowQualityFeature(entityList);
            entityList = ClusterUrlsWithSameFeatures(entityList);

            bool bMergeEntity = false;
            int iterCnt = 0;
            do
            {
                iterCnt++;
                bMergeEntity = false;
                List<UrlEntity> newEntityList = new List<UrlEntity>();

                for (int i = 0; i < entityList.Count; i++)
                {
                    if (i % 10 == 0)
                    {
                        Console.WriteLine("Iter {0}, {1}/{2} processed...", iterCnt, i, entityList.Count);
                    }

                    double maxSim = 0.0;
                    int bestIndex = -1;
                    int lastFeaSize = 0;
                    int iFeaSize = entityList[i].featureSet.Count;
                    List<QueryItem> iQueryItemList = entityList[i].featureSet;
                    for (int j = i + 1; j < entityList.Count; j++)
                    {
                        List<QueryItem> jQueryItemList = entityList[j].featureSet;

                        if (iFeaSize == jQueryItemList.Count && iFeaSize <= 3)
                        {
                            continue;
                        }


                        if (lastFeaSize != jQueryItemList.Count)
                        {
                            lastFeaSize = jQueryItemList.Count;
                            //pre-judge
                            double r1 = (double)iFeaSize / (double)lastFeaSize;
                            if (r1 < 0.8)
                            {
                                break;
                            }
                        }

                        double sim = GetSimilairyScore(iQueryItemList, jQueryItemList);
                        if (sim >= maxSim)
                        {
                            bestIndex = j;
                            maxSim = sim;
                        }
                        if (sim == 1.0)
                        {
                            break;
                        }
                    }
                    if (maxSim >= 0.8)
                    {
                        UrlEntity entity = new UrlEntity();
                        entity.strUrl = entityList[i].strUrl;
                        entity.featureSet = new List<QueryItem>(entityList[i].featureSet);

                        entity.featureSet.AddRange(entityList[bestIndex].featureSet);
                        entity.featureSet = MergeQueryItem(entity.featureSet);

                        entityList.RemoveAt(bestIndex);
                        bMergeEntity = true;

                        newEntityList.Add(entity);
                    }
                    else
                    {
                        newEntityList.Add(entityList[i]);
                    }
                }

                Console.WriteLine("Iter {0} : Merge entity from {1} to {2}", iterCnt, entityList.Count, newEntityList.Count);
                entityList = newEntityList;
                entityList = ClusterUrlsWithSameFeatures(entityList);

            } while (bMergeEntity == true);

            return entityList;

        }
    }
}
