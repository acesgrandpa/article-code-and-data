using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace coLocationMain
{
    /// <summary>
    /// 空间点：包括点号，点类型，点类型+点号，坐标
    /// </summary>
    class SpatialPoint
    {
        public string spPointID;
        public string spPointType;
        public string spPointKey;
        public double spPointX;
        public double spPointY;
        public SpatialPoint(string id, string type, double x, double y)
        {
            spPointID = id;
            spPointType = type;
            spPointX = x;
            spPointY = y;
            spPointKey = spPointType + '.' + spPointID;
        }
        //判断是否和别的点相近
        public bool isNear(SpatialPoint bsp, double threshold)
        {
            return Math.Sqrt(Math.Pow((this.spPointX - bsp.spPointX), 2) + Math.Pow((this.spPointY - bsp.spPointY), 2)) < threshold;
        }
    }

    /// <summary>
    /// 星型领域:
    /// </summary>
    class StarNeighbor
    {
        //星型领域的中心实例
        public SpatialPoint Instance;

        //根据类型分类的邻居
        public Dictionary<string, List<SpatialPoint>> NeighborByFeature;

        //new对象
        public StarNeighbor(SpatialPoint ins, Dictionary<string, List<SpatialPoint>> dic)
        {
            this.Instance = ins;
            this.NeighborByFeature = dic;
        }
    }

    /// <summary>
    /// 存储阶数 模式 模式对应实例的结构
    /// </summary>
    class RankPatternIns
    {
        public int rank;
        public string pattern;
        public Dictionary<SpatialPoint, Dictionary<string, List<SpatialPoint>>> ins;
        public RankPatternIns() { }
        public RankPatternIns(int r,string p, Dictionary<SpatialPoint, Dictionary<string, List<SpatialPoint>>> i)
        {
            rank = r;
            pattern = p;
            ins = i;
        }
    }

    class FileReader
    {
        [Obsolete("用list提取数据没有用dictionary提取来的简单")]
        private static List<SpatialPoint> GetSpatialPoints(string fileName)
        {
            List<SpatialPoint> spatialPoints = new List<SpatialPoint>();
            try
            {
                using (StreamReader sR = new StreamReader(fileName))
                {
                    string tS = sR.ReadLine();
                    while (sR.EndOfStream is false)
                    {
                        SpatialPoint tSpPoint = new SpatialPoint(tS.Split(',')[0], tS.Split(',')[1], Convert.ToDouble(tS.Split(',')[2]), Convert.ToDouble(tS.Split(',')[3]));
                        spatialPoints.Add(tSpPoint);
                        tS = sR.ReadLine();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return spatialPoints;
        }

        public static Dictionary<string, List<SpatialPoint>> GetFeatures(string fileName)
        {
            return new List<SpatialPoint>(
                from record in File.ReadAllLines(fileName)
                let temp = record.Split(',')
                select new SpatialPoint(temp[0], temp[1], Convert.ToDouble(temp[2]), Convert.ToDouble(temp[3]))
                )
                .GroupBy(list => list.spPointType)
                .OrderBy(list => list.Key)
                .ToDictionary(list => list.Key, list => list.ToList());

        }

        //从特征分类集合中取出所有点
        public static List<SpatialPoint> GetAllpts(Dictionary<string, List<SpatialPoint>> features)
        {
            List<SpatialPoint> allPts = new List<SpatialPoint>();
            foreach (var item in features)
            {
                allPts.AddRange(item.Value);
            }
            return allPts;
        }
    }

    class Drawer
    {
        public static Image DrawFromSpPs2Img(List<SpatialPoint> spPoints,double alpha)
        {
            int width = 600;
            int height = 600;
            //double alpha = 50;
            //double alpha = 0.04;
            Bitmap image = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);
            //绘制坐标轴
            //说明:屏幕坐标系相比普通坐标系,Y轴反号，X轴正常，所以Y轴值整体为绘图区高度height-实际
            //此外：边框，线宽会占位，所以无线宽影响值-2，有线宽影响值-3 坐标轴受线宽影响，点受不受均可
            //alpha为等比缩放系数，这是根据样例点的数据变更的
            Pen pBlack = new Pen(Color.Black, 3);
            Brush bRed = new SolidBrush(Color.FromArgb(255, Color.Red));
            Brush bBlue = new SolidBrush(Color.FromArgb(255, Color.Blue));
            Brush bGreen = new SolidBrush(Color.FromArgb(255, Color.Green));
            Point pointO = new Point(0, height - 3 - 0);
            Point pointX = new Point(width - 2, height - 3 - 0);
            Point pointY = new Point(0, height - 3 - height - 3);
            Font font = new Font("verdana", 8);
            g.DrawLine(pBlack, pointO, pointX);
            g.DrawLine(pBlack, pointO, pointY);
            for (int i = 0; i < spPoints.Count; i++)
            {
                if (spPoints[i].spPointType == "C")
                {
                    g.FillEllipse(bRed, Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2), 5, 5);
                    //标记名称
                    if (spPoints.Count<100)
                    {
                        g.DrawString(spPoints[i].spPointKey, font, bRed, Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2));
                    }
                }
                else if (spPoints[i].spPointType == "D")
                //else if (spPoints[i].spPointType == "R")
                {
                    g.FillEllipse(bBlue, Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2), 5, 5);
                    if (spPoints.Count < 100)
                    {
                        g.DrawString(spPoints[i].spPointKey, font, bBlue, Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2));
                    }
                }
                //else if (spPoints[i].spPointType == "S")
                else if (spPoints[i].spPointType == "E")
                {
                    g.FillEllipse(bGreen, Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2), 5, 5);
                    if (spPoints.Count < 100)
                    {
                        g.DrawString(spPoints[i].spPointKey, font, bGreen, Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2));
                    }
                }
                else if (spPoints[i].spPointType == "F")
                {
                    g.FillEllipse(new SolidBrush(Color.FromArgb(255, Color.Black)), Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2), 5, 5);
                    if (spPoints.Count < 100)
                    {
                        g.DrawString(spPoints[i].spPointKey, font, new SolidBrush(Color.FromArgb(255, Color.Black)), Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2));
                    }
                }
                else if (spPoints[i].spPointType == "M")
                {
                    g.FillEllipse(new SolidBrush(Color.FromArgb(255, Color.Purple)), Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2), 5, 5);
                    if (spPoints.Count < 100)
                    {
                        g.DrawString(spPoints[i].spPointKey, font, new SolidBrush(Color.FromArgb(255, Color.Purple)), Convert.ToSingle(spPoints[i].spPointX * alpha - 2), Convert.ToSingle(height - spPoints[i].spPointY * alpha - 2));
                    }
                }


            }
            Image img = image;
            return img;

        }
    }

    class MyMethods
    {
        //基础函数
        static double Sqrt(double x)
        {
            return Math.Sqrt(x);
        }
        static double Pow(double x, double n)
        {
            return Math.Pow(x, n);
        }

        //根据点坐标计算点距离
        public static double GetDistance(SpatialPoint spPoint1, SpatialPoint spPoint2)
        {
            return Sqrt(Pow((spPoint1.spPointX - spPoint2.spPointX), 2) + Pow((spPoint1.spPointY - spPoint2.spPointY), 2));
        }
    }
}
