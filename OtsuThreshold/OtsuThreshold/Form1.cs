using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OtsuThreshold
{
    public partial class Form1 : Form
    {
        private Otsu ot = new Otsu();
        private Bitmap org;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = Bitmap.FromFile(openFileDialog1.FileName);
                org = (Bitmap)pictureBox1.Image.Clone();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image.Save(saveFileDialog1.FileName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bitmap temp = (Bitmap)org.Clone();
            ot.Convert2GrayScaleFast(temp);
            int otsuThreshold= ot.getOtsuThreshold((Bitmap)temp);
            ot.threshold(temp,otsuThreshold);
            textBox1.Text = otsuThreshold.ToString();
            pictureBox1.Image = temp;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            org = (Bitmap)pictureBox1.Image.Clone();
        }
    }
}
