using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace coLocationMain
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        #region 全局数据
        string fileName = @"E:\Code\CS\Colocation\coLocationMain\test5small.txt";
        //找相邻的阈值
        double threshold = 2;
        //实例集合
        Dictionary<string, List<SpatialPoint>> cases;
        //全点集合
        List<SpatialPoint> allPts;
        //星型实例集合
        Dictionary<string, List<StarNeighbor>> StarNeighbors;
        #endregion

        /// <summary>
        /// 从文件到按特征组织实例
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            cases = FileReader.GetFeatures(fileName);
            allPts = FileReader.GetAllpts(cases);
            foreach (var key in cases.Keys)
            {
                richTextBox1.AppendText("====类型：= " + key + " =====\n");
                for (int i = 0; i < cases[key].Count; i++)
                {
                    richTextBox1.AppendText(cases[key][i].spPointKey + "  " + cases[key][i].spPointX.ToString() + "  " + cases[key][i].spPointY.ToString() + "\n");
                }
            }
            //绘图
            //缩放比：模拟数据用50,真实数据用0.04
            double alpha = 50;
            //double alpha = 0.04;
            pictureBox1.Image = Drawer.DrawFromSpPs2Img(allPts,alpha);
        }

        /// <summary>
        /// 从按特征组织实例到星型邻域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            var starNeighbor = Joinless.Step1(cases, threshold);
            foreach (var key in starNeighbor.Keys)
            {
                richTextBox1.AppendText("=======特征类型: " + key + " =========\n");
                richTextBox1.AppendText("==实例==||  邻域实例===============\n");
                foreach (var star in starNeighbor[key])
                {
                    richTextBox1.AppendText(star.Instance.spPointKey + "     ||  ");
                    foreach (var neighborfeature in star.NeighborByFeature.Keys)
                    {
                        foreach (var item in star.NeighborByFeature[neighborfeature])
                        {
                            richTextBox1.AppendText("  " + item.spPointKey + "  ");
                        }
                    }
                    richTextBox1.AppendText("\n");
                }
            }
            StarNeighbors = starNeighbor;
        }


        //综合
        private void button3_Click(object sender, EventArgs e)
        {
            button1_Click(sender,e);
            button2_Click(sender,e);
        }

        //提取所有候选模式
        private void button4_Click(object sender, EventArgs e)
        {
            button3_Click(sender, e);
            richTextBox1.Clear();
            var candiCos = Joinless.test1(StarNeighbors);
            int k = 1;
            foreach (var kmodel in candiCos)
            {
                richTextBox1.AppendText(String.Format("========={0}阶模式========\n", k));
                foreach (var co in kmodel)
                {
                    richTextBox1.AppendText(":      "+co + "\n");
                }
                k++;
            }
        }

        //测试
        private void button5_Click(object sender, EventArgs e)
        {
            fileName = @"E:\Code\CS\Colocation\coLocationMain\test5small.txt";
            button1_Click(sender,e);
            button2_Click(sender, e);
            var result = Joinless.Step2a(StarNeighbors);
            richTextBox1.Clear();
            int k = 0;
            foreach (var item in result)
            {
                if (k!=item.rank)
                {
                    k = item.rank;
                    richTextBox1.AppendText(string.Format("=========={0}阶==========\n", k));
                }
                richTextBox1.AppendText(string.Format("========= 模式 {0}===========\n", item.pattern));
                foreach (var ins in item.ins)
                {
                    richTextBox1.AppendText("=== "+ins.Key.spPointKey + ":\n");
                    foreach (var features in ins.Value.Values)
                    {
                        foreach (var feature in features)
                        {
                            richTextBox1.AppendText(feature.spPointKey + " , ");
                        }
                        richTextBox1.AppendText("\n");
                    }
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            fileName = @"E:\Code\CS\Colocation\coLocationMain\test5small.txt";
            button1_Click(sender, e);
            button2_Click(sender, e);
            Joinless.Step2(StarNeighbors);
        }
    }
}
