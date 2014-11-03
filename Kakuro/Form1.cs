using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Kakuro
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int nRows, nCols;
            try
            {
                nRows = int.Parse(textBox1.Text);
                nCols = int.Parse(textBox2.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invlaid number of rows or columns specified.");
                return;
            }

            KakuroBoard board = new KakuroBoard(nRows, nCols);
            board.Show();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Kakuro board files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = openFileDialog1.FileName;
            }
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            Stream stream = File.OpenRead(textBox3.Text);
            StreamReader sr = new StreamReader(stream);

            KakuroBoard board = new KakuroBoard(sr);

            sr.Close();
            stream.Close();

            board.Show();
        }
    }
}