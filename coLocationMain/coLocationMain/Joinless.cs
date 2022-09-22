using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coLocationMain
{
    class Joinless
    {
        #region 废弃代码
        [Obsolete("用dictionary一步到位不需要再提取类型")]
        private static List<string> GetTypes(List<SpatialPoint> spatialPoints)
        {
            List<string> types = new List<string>();
            for (int i = 0; i < spatialPoints.Count; i++)
            {
                //如果不包含指定类型
                if (types.Contains(spatialPoints[i].spPointType) == false)
                {
                    types.Add(spatialPoints[i].spPointType);
                }
            }
            return types;
        }
        #endregion

        #region 全局变量
        //将每种特征的个数存储下来
        public static Dictionary<string, int> FeatureCounts = new Dictionary<string, int>();
        #endregion

        #region 辅助方法
        //获取第一步的候选模式：从k阶到k+1阶
        public static HashSet<string> GetCandidateCo(HashSet<string> key)
        {
            HashSet<string> patterns = new HashSet<string>();
            foreach (var featureOut in key)
            {
                foreach (var featureIn in key)
                {
                    if ((featureIn.Last()>featureOut.Last())&& featureIn.Substring(0,featureIn.Length-1)== featureOut.Substring(0, featureOut.Length - 1))
                    {
                        patterns.Add(featureOut + featureIn.Last());
                    }
                }
            }
            return patterns;
        }
        //测试函数，测试模式生成效果
        public static List<HashSet<string>> test1(Dictionary<string, List<StarNeighbor>> inData)
        {
            HashSet<string> basic = new HashSet<string>(inData.Keys);
            int k = basic.Count - 1;
            List<HashSet<string>> result = new List<HashSet<string>>();
            result.Add(basic);
            while (true)
            {
                HashSet<string> temp = new HashSet<string>();
                temp = GetCandidateCo(result.Last());

                if (temp.Count == 0) break;
                result.Add(temp);

            }
            return result;
        }
        
        //筛选：生成星型实例：星型实例指模式中心实例和其他实例相邻，但非中心实例不确定是否相邻的模式
        public static Dictionary<SpatialPoint, Dictionary<string, List<SpatialPoint>>> GenStar(string pattern, List<StarNeighbor> starNeighbor)
        {
            Dictionary<SpatialPoint, Dictionary<string, List<SpatialPoint>>> centerIns = new Dictionary<SpatialPoint, Dictionary<string, List<SpatialPoint>>>();
            foreach (var item in starNeighbor)
            {
                bool hasCoarseNeighbor = true;
                Dictionary<string, List<SpatialPoint>> aCenterIns = new Dictionary<string, List<SpatialPoint>>();
                foreach (var feature in pattern)
                {
                    //如果邻域包含模式中实例，则保存，如模式 CD ，有 D 则保存 D 的所有实例
                    if (item.NeighborByFeature.ContainsKey(feature.ToString()))
                    {
                        List<SpatialPoint> tempIns = item.NeighborByFeature[feature.ToString()];
                        aCenterIns.Add(feature.ToString(), tempIns);
                    }
                    //如果邻域中不包含模式中的所有实例，则退出重来，如CDE,有 D 无 E 则退出
                    else
                    {
                        hasCoarseNeighbor = false;
                        break;
                    }
                }
                if (hasCoarseNeighbor)
                {
                    centerIns.Add(item.Instance, aCenterIns);
                }
            }
            return centerIns;
        }

        //辅助：第二步中间步骤展示：粗筛星型实例
        public static List<RankPatternIns> Step2a(Dictionary<string, List<StarNeighbor>> starNeighbors)
        {
            //定义最终的候选模式数组 int为阶数 hashset<string>是指定阶的模式
            Dictionary<int, HashSet<string>> coModel = new Dictionary<int, HashSet<string>>();
            //定义每一阶的候选模式
            Dictionary<int, HashSet<string>> canModel = new Dictionary<int, HashSet<string>>();
            //定义每一阶的候选模式实例

            //一阶模式的频繁模式P1
            HashSet<string> basicFeature = new HashSet<string>(starNeighbors.Keys);
            coModel.Add(1, basicFeature);
            //当前处理阶数k
            int k = 2;
            List<RankPatternIns> StarInsk = new List<RankPatternIns>();
            while (coModel[k - 1].Count != 0)
            {
                //从k-1阶模式中生成k阶模式的候选模式
                var currentCanModel = GetCandidateCo(coModel[k - 1]);
                //当前阶的星型实例
                foreach (var feature in basicFeature)
                {
                    foreach (var item in currentCanModel)
                    {
                        if (item[0].ToString() == feature)
                        {
                            //根据传入模式生成候选实例
                            var patternIns = GenStar(item.Substring(1), starNeighbors[feature]);
                            var tempRPI = new RankPatternIns(k, item, patternIns);
                            StarInsk.Add(tempRPI);
                        }
                    }
                }
                if (k == 2)
                {
                }


                HashSet<string> temp = new HashSet<string>();
                foreach (var item in StarInsk)
                {
                    if (item.rank == k)
                    {
                        temp.Add(item.pattern);
                    }
                }
                coModel.Add(k, temp);
                k++;
            }
            return StarInsk;


        }

        #endregion

        //粗略筛选，如果模式的参与度不满足阈值，则可以不考虑模式是否属于团实例
        public static void CoarseFilter(List<RankPatternIns> StarIns, Dictionary<string, List<StarNeighbor>> basicData,double thresholdPI)
        {
            List<RankPatternIns> removeList = new List<RankPatternIns>();
            //对每个模式的实例进行处理
            foreach (var patternGroup in StarIns)
            {
                //分别计算pattern中每个特征的参与度
                Dictionary<string, double> PIList = new Dictionary<string, double>();
                var patternArray = patternGroup.pattern.ToCharArray();
                //第一项的参与度
                PIList.Add(patternArray[0].ToString(), Convert.ToDouble( patternGroup.ins.Count) / Convert.ToDouble( FeatureCounts[patternArray[0].ToString()]));
                for (int i = 1; i < patternArray.Length; i++)
                {
                    //模式特征实例数
                    HashSet<string> ACenterFeatureInsCountList = new HashSet<string>();
                    foreach (var ins in patternGroup.ins)
                    {
                        var ACenterFeatureIns = ins.Value[patternArray[i].ToString()];
                        foreach (var item in ACenterFeatureIns)
                        {
                            ACenterFeatureInsCountList.Add(item.spPointID);
                        }
                    }
                    PIList.Add(patternArray[i].ToString(),Convert.ToDouble( ACenterFeatureInsCountList.Count) / Convert.ToDouble( FeatureCounts[patternArray[i].ToString()]));
                }
                var minPr=PIList.Min(d => d.Value);
                if (minPr<thresholdPI)
                {
                    removeList.Add(patternGroup);
                }
                //获得每个特征的参与度后，计算最小参与度，比较判断是否删除当前模式
            }
            foreach (var item in removeList)
            {
                StarIns.Remove(item);
            }
            return;
        }

        //根据传入的模式初始化当前团模式
        public static Dictionary<string, HashSet<int>> InitCurCliqueIns(string pattern)
        {
            Dictionary<string, HashSet<int>> curCliqueIns = new Dictionary<string, HashSet<int>>();
            char[] features=pattern.ToCharArray();
            foreach (var item in features)
            {
                curCliqueIns.Add(item.ToString(), new HashSet<int>());
            }
            return curCliqueIns;
        }


        //获取团实例 删除非团结构的实例
        public static void GenClique(List<RankPatternIns> StarIns, Dictionary<string, List<StarNeighbor>> basicData, Dictionary<int, Dictionary<string, HashSet<string>>> kNearTable)
        {
            foreach (var pattern in StarIns)
            {
                //对于每一个pattern 生成一个存满足团条件的模式
                Dictionary<string, HashSet<int>> curCliqueIns = InitCurCliqueIns(pattern.pattern);
                string currentPattern = pattern.pattern.Substring(1);
                //如果除中心外是二阶模式 直接比较
                if (currentPattern.Length == 2)
                {
                    //将固定结构数组列表转换为键值对组
                    HashSet<string> compareModel = kNearTable[2][currentPattern];
                    Dictionary<string, HashSet<string>> tempList = new Dictionary<string, HashSet<string>>();
                    foreach (var item in compareModel)
                    {
                        string key = item.Split(',')[0];
                        string value= item.Split(',')[1];
                        if (tempList.ContainsKey(key)== false)
                        {
                            var tempHashset = new HashSet<string>();
                            tempHashset.Add(value);
                            tempList.Add(key,tempHashset);
                        }
                        else
                        {
                            tempList[key].Add(value);
                        }
                    }
                    Dictionary<string, HashSet<string>> nearTable3 = new Dictionary<string, HashSet<string>>();
                    HashSet<string> nearSet3 = new HashSet<string>();
                    //从邻域中把点编号取出来
                    foreach (var center in pattern.ins)
                    {
                        List<HashSet<string>> neighbors = new List<HashSet<string>>();
                        foreach (var neighbor in center.Value)
                        {
                            HashSet<string> tempHash = new HashSet<string>();
                            foreach (var item in neighbor.Value)
                            {
                                tempHash.Add(item.spPointID);
                            }
                            neighbors.Add(tempHash);
                        }

                        //和已有序列templist对比 有则保存没有则不保存
                        foreach (var neighbor in neighbors[0])
                        {
                            foreach (var neighbor2 in neighbors[1])
                            {
                                if (tempList.ContainsKey(neighbor))
                                {
                                    if (tempList[neighbor].Contains(neighbor2))
                                    {
                                        string cliqueGroup = center.Key.spPointID + ',' + neighbor + ',' + neighbor2;
                                        nearSet3.Add(cliqueGroup);
                                    }
                                }
                                else break;
                            }
                        }
                    }
                    //将相邻关系保存下来
                    nearTable3.Add(pattern.pattern, nearSet3);
                }
                //如果除中心外是三阶模式及以上 最好进入二阶模式的比较方法
                else
                {
                    int x = 0;
                }
            }

        }


        //第一步，用传入的点生成一个星型邻近表
        //输出结果为：4层列表
        //特征A 实例A.1 邻域特征B 邻域特征实例B.1
        //                        邻域特征实例B.2
        //              邻域特征C 
        //      实例A.2 
        //特征B      
        public static Dictionary<string, List<StarNeighbor>> Step1(Dictionary<string, List<SpatialPoint>> InputList, double threshold)
        {
            //星型邻近表
            var StarNeighborList = new Dictionary<string, List<StarNeighbor>>();
            //key为当前处理的特征
            foreach (var key in InputList.Keys)
            {
                //某个特征的所有实例邻域
                List<StarNeighbor> featureNeighbors = new List<StarNeighbor>();
                for (int i = 0; i < InputList[key].Count; i++)
                {
                    //currentInstance是当前特征的当前实例
                    SpatialPoint currentInstance = InputList[key][i];
                    //当前实例的当前特征的邻域对象也按照特征类型进行分类组织
                    var Dic = new Dictionary<string, List<SpatialPoint>>();
                    //指定要查找的特征
                    foreach (var feature in InputList.Keys)
                    {
                        //只有类型靠后的才可能出现在星型邻域中
                        if (feature.CompareTo(key) > 0)
                        {
                            //寻找所有邻近的实例存入列表
                            List<SpatialPoint> tempList = InputList[feature].FindAll(l => l.isNear(currentInstance, threshold));
                            //特征类型和邻近实例存入邻域对象
                            //只有有邻近实例才存入列表
                            if (tempList.Count>0)
                            {
                                Dic.Add(feature, tempList);
                            }
                        }
                    }
                    //当前实例存入当前特征集中
                    featureNeighbors.Add(new StarNeighbor(currentInstance, Dic));
                }
                if (featureNeighbors.Count != 0)
                {
                    //当前特征存入星型临近表
                    StarNeighborList.Add(key, featureNeighbors);
                }
            }
            //填充featurecount数据
            if (StarNeighborList.Count != 0)
            {
                foreach (var item in StarNeighborList.Keys)
                {
                    FeatureCounts.Add(item, StarNeighborList[item].Count);
                }
                return StarNeighborList;
            }
            else
            {
                return null;
            }
        }
        







        //第二步 获取频繁模式
        public static void Step2(Dictionary<string, List<StarNeighbor>> starNeighbors)
        {
            //参与度阈值初定0.5
            double thresholdPI = 0.5;
            //定义最终的候选模式数组 int为阶数 hashset<string>是指定阶的模式
            Dictionary<int, HashSet<string>> coModel = new Dictionary<int, HashSet<string>>();
            //定义每一阶的候选模式
            Dictionary<int, HashSet<string>> canModel = new Dictionary<int, HashSet<string>>();
            //定义每一阶的候选模式实例

            //一阶模式的频繁模式P1
            HashSet<string> basicFeature = new HashSet<string>(starNeighbors.Keys);
            coModel.Add(1, basicFeature);
            //当前处理阶数k
            int k = 2;

            //定义包含每一阶数据的星型实例
            List<RankPatternIns> StarInsk = new List<RankPatternIns>();
            //定义包含每一阶数据的团实例
            List<RankPatternIns> CliqueInsk = new List<RankPatternIns>();

            //直接将邻近关系保存下来
            Dictionary<int, Dictionary<string, HashSet<string>>> kNearTable = new Dictionary<int, Dictionary<string, HashSet<string>>>();

            while (coModel[k - 1].Count != 0)
            {
                //从k-1阶模式中生成k阶模式的候选模式
                var currentCanModel = GetCandidateCo(coModel[k - 1]);
                //当前阶的星型实例
                foreach (var feature in basicFeature)
                {
                    foreach (var item in currentCanModel)
                    {
                        if (item[0].ToString() == feature)
                        {
                            //根据传入模式生成候选实例
                            var patternIns = GenStar(item.Substring(1), starNeighbors[feature]);
                            var tempRPI = new RankPatternIns(k, item, patternIns);
                            StarInsk.Add(tempRPI);
                        }
                    }
                }





                //当前阶的团实例
                if (k == 2)
                {
                    Dictionary<string, HashSet<string>> tempD = new Dictionary<string, HashSet<string>>();
                    foreach (var item in StarInsk)
                    {
                        HashSet<string> nearItems = new HashSet<string>();
                        foreach (var ins in item.ins)
                        {
                            //因为是二元，所以邻域对象肯定只有一类
                            foreach (var neighbor in ins.Value.First().Value)
                            {
                                string temps = ins.Key.spPointID +','+ neighbor.spPointID;
                                nearItems.Add(temps);
                            }

                        }
                        tempD.Add(item.pattern, nearItems);
                        CliqueInsk.Add(item);
                    }
                    kNearTable.Add(2, tempD);
                }

                else
                {
                    //获取当前阶的星型实例
                    var kStarIns = new List<RankPatternIns>();
                    foreach (var item in StarInsk)
                    {
                        if (item.rank==k)
                        {
                            kStarIns.Add(item);
                        }
                    }
                    //粗略筛选
                    CoarseFilter(kStarIns, starNeighbors, thresholdPI);
                    GenClique(kStarIns,starNeighbors,kNearTable);
                    CliqueInsk = kStarIns;

                }



                //循环终止
                HashSet<string> temp = new HashSet<string>();
                foreach (var item in CliqueInsk)
                {
                        temp.Add(item.pattern);
                }
                coModel.Add(k, temp);

                k++;
            }
        }

    }
}
