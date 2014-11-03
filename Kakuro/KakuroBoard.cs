using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace Kakuro
{
    public interface IStatusUpdater
    {
        void UpdateStatus();
    }

    public partial class KakuroBoard : Form, IStatusUpdater
    {
        /// <summary>
        /// Create an empty Kakuro board with the specified dimensions
        /// </summary>
        public KakuroBoard(int nRows, int nCols)
        {
            InitializeComponent();

            InitializeArrays(nRows,nCols);

            UpdateTextBoxes();
        }

        /// <summary>
        /// Load a Kakuro board from a stream
        /// </summary>
        /// <param name="sr"></param>
        public KakuroBoard(TextReader sr)
        {
            InitializeComponent();

            string s;

            // read number of rows and cols
            int nRows, nCols;
            s = sr.ReadLine();
            nRows = int.Parse(s);
            s = sr.ReadLine();
            nCols = int.Parse(s);

            InitializeArrays(nRows, nCols);

            // read contents
            for (int i = 0; i < nRows; i++)
            {
                s = sr.ReadLine();
                string[] sa = s.Split('\t');
                for (int j = 0; j < nCols; j++)
                {
                    displayed[i, j].Text = sa[j];
                }
            }
        }

        private void InitializeArrays(int nRows, int nCols)
        {
            m_nRows = nRows;
            m_nCols = nCols;

            board = new Board(nRows, nCols);
            displayed = new TextBox[nRows, nCols];

            for (int i = 0; i < nCols; i++)
            {
                for (int j = 0; j < nRows; j++)
                {
                    board[j, i] = new Element();
                    displayed[j, i] = new TextBox();
                    displayed[j, i].Parent = this;
                    displayed[j, i].Location = new Point(i * ColumnSpacing + LeftMargin, j * RowSpacing + TopMargin);
                    displayed[j, i].Size = new Size(ColumnWidth, RowHeight);
                    displayed[j, i].Visible = true;
                    displayed[j, i].TextChanged += new EventHandler(KakuroBoard_TextChanged);
                }
            }

            this.Size = new Size(nCols * ColumnSpacing + LeftMargin + RightMargin, nRows * RowSpacing + TopMargin + BottomMargin);

            int xPosSolve = (this.Size.Width - buttonSolve.Size.Width - buttonSave.Size.Width - ColumnSpacing) / 2;
            buttonSolve.Location = new Point(xPosSolve, nRows * RowSpacing + TopMargin);
            buttonSave.Location = new Point(xPosSolve + buttonSolve.Size.Width + ColumnSpacing, nRows * RowSpacing + TopMargin);
        }

        void KakuroBoard_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string s = tb.Text;
            int nBackslashPos = s.IndexOf('\\');

            // determine which row,col we are in
            int nCol = (tb.Location.X - LeftMargin) / ColumnSpacing;
            int nRow = (tb.Location.Y - TopMargin) / RowSpacing;

            // if the textbox contains a backslash, we need to convert it
            // to a sum-type element
            if (nBackslashPos != -1)
            {
                // parse the string
                int valDown = Element.Illegal, valRight = Element.Illegal;
                try
                {
                    if (nBackslashPos == 0)
                        valDown = Element.Unused;
                    else
                        valDown = int.Parse(s.Substring(0, nBackslashPos));
                }
                catch (FormatException) { }
                try
                {
                    if (nBackslashPos == s.Length - 1)
                        valRight = Element.Unused;
                    else
                        valRight = int.Parse(s.Substring(nBackslashPos + 1));
                }
                catch (FormatException) { }
                if (valDown != Element.Illegal && valRight != Element.Illegal)
                {
                    // we have a legal sum-type element
                    board[nRow, nCol].SetSum(valDown, valRight);
                    tb.BackColor = Color.Black;
                    tb.ForeColor = Color.White;
                }
                else
                {
                    board[nRow, nCol].SetValue(Element.Unknown);
                    tb.BackColor = Color.Red;
                    tb.ForeColor = Color.Black;
                }
            }
            else
            {
                // not a sum-type element
                if (tb.Text.Length > 0)
                {
                    int val = Element.Illegal;
                    try
                    {
                        val = int.Parse(tb.Text);
                    }
                    catch (FormatException) { }
                    if (val >= 1 && val <= 9)
                    {
                        board[nRow, nCol].SetValue(val);
                        tb.BackColor = Color.LightGreen;
                        tb.ForeColor = Color.Black;
                    }
                    else
                    {
                        board[nRow, nCol].SetValue(Element.Unknown);
                        tb.BackColor = Color.Red;
                        tb.ForeColor = Color.Black;
                    }
                }
                else
                {
                    board[nRow, nCol].SetValue(Element.Unknown);
                    tb.BackColor = Color.White;
                    tb.ForeColor = Color.Black;
                }
            }
        }

        // display constants
        private const int ColumnSpacing = 40;
        private const int RowSpacing = 30;
        private const int ColumnWidth = 35;
        private const int RowHeight = 25;
        private const int LeftMargin = 12;
        private const int TopMargin = 12;
        private const int RightMargin = 12;
        private const int BottomMargin = 60;

        private Board board;
        private TextBox[,] displayed;

        int m_nRows, m_nCols;

        private void buttonSolve_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            DateTime dtStart = DateTime.Now;
            bool success = board.Solve(this, 5);
            DateTime dtEnd = DateTime.Now;
            Cursor = Cursors.Default;

            UpdateTextBoxes();

            if (success)
                MessageBox.Show("Completed successfully. Time elapsed: "+(dtEnd-dtStart).ToString());
            else
                MessageBox.Show("Failed to solve.");
        }

        void IStatusUpdater.UpdateStatus()
        {
            UpdateTextBoxes();
            this.Refresh();
        }

        public void UpdateTextBoxes()
        {
            for (int i = 0; i < m_nRows; i++)
                for (int j = 0; j < m_nCols; j++)
                    displayed[i, j].Text = board[i, j].ToString();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Kakuro board files (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Stream stream = dlg.OpenFile();
                StreamWriter sw = new StreamWriter(stream);

                sw.WriteLine(m_nRows.ToString());
                sw.WriteLine(m_nCols.ToString());

                for (int row = 0; row < m_nRows; row++)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int col = 0; col < m_nCols; col++)
                    {
                        sb.Append(displayed[row, col].Text);
                        sb.Append('\t');
                    }
                    sw.WriteLine(sb);
                }

                sw.Close();
                stream.Close();
            }
        }
    }
}