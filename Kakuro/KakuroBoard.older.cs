using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Kakuro
{
    public interface IStatusUpdater
    {
        void UpdateStatus();
    }

    public partial class KakuroBoard : Form, IStatusUpdater
    {
        public KakuroBoard(int nRows, int nCols)
        {
            InitializeComponent();

            m_nRows = nRows;
            m_nCols = nCols;

            board = new Board(nRows, nCols);
            displayed = new TextBox[nRows, nCols];

            for (int i = 0; i < nCols; i++)
            {
                for (int j = 0; j < nRows; j++)
                {
                    board[j, i] = new Element();
                    displayed[j,i] = new TextBox();
                    displayed[j, i].Parent = this;
                    displayed[j, i].Location = new Point(i * ColumnSpacing + LeftMargin, j * RowSpacing + TopMargin);
                    displayed[j, i].Size = new Size(ColumnWidth, RowHeight);
                    displayed[j, i].Visible = true;
                    displayed[j, i].TextChanged += new EventHandler(KakuroBoard_TextChanged);
                }
            }
            UpdateTextBoxes();

            this.Size = new Size(nCols * ColumnSpacing + LeftMargin + RightMargin, nRows * RowSpacing + TopMargin + BottomMargin);

            buttonSolve.Location = new Point((this.Size.Width - buttonSolve.Size.Width) / 2, nRows * RowSpacing + TopMargin);
        }

        void KakuroBoard_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string s = tb.Text;
            int nBackslashPos = s.IndexOf('\\');

            // if the textbox contains a backslash, we need to convert it
            // to a sum-type element
            if (nBackslashPos != -1)
            {
                // determine which row,col we are in
                int nCol = (tb.Location.X - LeftMargin) / ColumnSpacing;
                int nRow = (tb.Location.Y - TopMargin) / RowSpacing;

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
                    board[nRow, nCol] = new Element(valDown, valRight);
                    tb.BackColor = Color.Black;
                    tb.ForeColor = Color.White;
                }
                else
                {
                    tb.BackColor = Color.Red;
                    tb.ForeColor = Color.Black;
                }
            }
            else
            {
                // not a sum-type element
                if (tb.Text.Length > 0)
                {
                    tb.BackColor = Color.Red;
                    tb.ForeColor = Color.Black;
                }
                else
                {
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
            bool success = board.Solve(this, 5);
            Cursor = Cursors.Default;

            UpdateTextBoxes();

            if (success)
                MessageBox.Show("Completed successfully.");
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
    }

    public class Board
    {
        public Board(int nRows, int nCols)
        {
            m_nRows = nRows;
            m_nCols = nCols;

            m_board = new Element[nRows, nCols];
        }

        public Element this[int row, int col] 
        {
            get { return m_board[row,col]; }
            set { m_board[row, col] = value; }
        }

        private IStatusUpdater m_updater;
        private System.DateTime m_dtNextUpdate;
        private int m_nUpdateSeconds;

        public bool Solve(IStatusUpdater updater, int nSeconds)
        {
            m_updater = updater;
            m_dtNextUpdate = System.DateTime.Now.AddSeconds(nSeconds);
            m_nUpdateSeconds = nSeconds;
            return SolveAt(0, 0);
        }

        private bool SolveAt(int nRow, int nCol)
        {
            if (System.DateTime.Now > m_dtNextUpdate)
            {
                m_updater.UpdateStatus();
                m_dtNextUpdate = System.DateTime.Now.AddSeconds(m_nUpdateSeconds);
            }

            if (nCol == m_nCols)
            {
                nCol = 0;
                nRow++;
            }

            if (nRow == m_nRows && nCol == 0)
            {
                // complete!
                return true;
            }

            // if we are in a sum-type element, then advance to next element
            if (!this[nRow, nCol].HasValue)
            {
                return SolveAt(nRow, nCol+1);
            }

            // we are in a value-type element. see if we can make it work.
            // first, find information about the current column and row.
            int nColStart, nColEnd, nColSum; //NB: nColStart is the ROW in which the current column starts!
            int nRowStart, nRowEnd, nRowSum; //NB: nRowStart is the COLUMN in which the current row starts!

            // find the row in which the current column starts
            for(nColStart=nRow; this[nColStart-1,nCol].HasValue; nColStart--) 
                ;
            // find the row in which the current column ends
            for(nColEnd=nRow; nColEnd<m_nRows-1 && this[nColEnd+1,nCol].HasValue; nColEnd++)
                ;
            // find the column in which the current row starts
            for(nRowStart=nCol; this[nRow,nRowStart-1].HasValue; nRowStart--) 
                ;
            // find the column in which the current row ends
            for(nRowEnd=nCol; nRowEnd<m_nCols-1 && this[nRow,nRowEnd+1].HasValue; nRowEnd++)
                ;
            // find the expected sums for the current row and column
            nColSum = this[nColStart-1,nCol].SumDown;
            nRowSum = this[nRow,nRowStart-1].SumRight;
            Debug.Assert(nColSum != Element.Unused);
            Debug.Assert(nRowSum != Element.Unused);

            // find illegal values for the current element. 
            // these are stored in the bitarray illegalValues: 
            // value i is illegal iff bit i is 1
            int illegalValues = 0;

            // stage 1: must not appear in the current column
            // (we also use this pass to calculate the column sum so far)
            int colsum = 0;
            for (int i = nColStart; i < nRow; i++)
            {
                int valHere = this[i, nCol].Value;
                illegalValues |= (1 << valHere);
                colsum += valHere;
            }

            // stage 2: must not appear in the current row
            // (we also use this pass to calculate the row sum so far)
            int rowsum = 0;
            for (int i = nRowStart; i < nCol; i++)
            {
                int valHere = this[nRow, i].Value;
                illegalValues |= (1 << valHere);
                rowsum += valHere;
            }

            int curValue=0;
            // if we are in the last element of the column/row, we must behave differently,
            // since there is only one possible value here
            if (nCol == nRowEnd)
            {
                curValue = nRowSum - rowsum;
                if (curValue < 1 || curValue > 9)
                    return false;
            }
            if (nRow == nColEnd)
            {
                int required = nColSum - colsum;
                if (required < 1 || required > 9)
                    return false;
                if (curValue == 0)
                    curValue = required;
                else
                {
                    if (curValue != required)
                    {
                        // contradiction between row and column requirements; we failed
                        return false;
                    }
                    else
                    {
                        // row and column requirements agree; continue
                    }
                }
            }

            if (curValue != 0)
            {
                // we have a row or column requirement; see if the required value is legal
                if ((illegalValues & (1 << curValue)) == 0)
                {
                    // digit doesn't yet appear
                    if (colsum + curValue <= nColSum)
                    {
                        // digit doesn't exceed expected column sum
                        if (rowsum + curValue <= nRowSum)
                        {
                            // digit doesn't exceed expected row sum

                            // set value in current cell
                            this[nRow, nCol].Value = curValue;

                            // recurse
                            if (SolveAt(nRow, nCol + 1))
                            {
                                // success!
                                return true;
                            }
                            else
                            {
                                // this doesn't work; we failed
                            }
                        }
                    }
                }
            }
            else 
            {
                // no row or column requirements; check all legal possibilities

                for (curValue = 9; curValue >= 1; curValue--)
                {
                    // see if this value is legal
                    if ((illegalValues & (1 << curValue)) != 0)
                        continue; // digit already appears in current row or column
                    if (colsum + curValue > nColSum)
                        continue; // exceeded expected column sum
                    if (rowsum + curValue > nRowSum)
                        continue; // exceeded expected row sum

                    // set value in current cell
                    this[nRow, nCol].Value = curValue;

                    // recurse
                    if (SolveAt(nRow, nCol + 1))
                    {
                        // success!
                        return true;
                    }
                    else
                    {
                        // that didn't work, try the next one
                    }
                }
            }

            // nothing worked;
            // let's clear the field so the higher recursion level can try again
            this[nRow, nCol].Value = Element.Unknown;
            return false;

        }

        public int Rows { get { return m_nRows; } }
        public int Cols { get { return m_nCols; } }

        private int m_nRows, m_nCols;
        private Element[,] m_board;
    }

    /// <summary>
    /// A single element in a Kakuro board.
    /// May be either a value element, or a sum element.
    /// Value elements contain a digit (1-9). 
    /// Sum elements contain either a sumDown, or a sumRight, or both.
    /// </summary>
    public class Element
    {
        /// <summary>
        /// Indicates that this member is not used in the current element type
        /// (for example, SumDown in an element containing a digit value)
        /// </summary>
        public const int Unused = -1;

        /// <summary>
        /// Indicates that the element contains an unknown value
        /// </summary>
        public const int Unknown = 0;

        /// <summary>
        /// An illegal value - should not appear when solving
        /// </summary>
        public const int Illegal = -100;

        /// <summary>
        /// default constructor creates an element with an unknown value
        /// </summary>
        public Element()
        {
            m_nValue = Unknown;
            m_nSumDown = Unused;
            m_nSumRight = Unused;
        }

        /// <summary>
        /// create an element with a certain value
        /// </summary>
        public Element(int val)
        {
            m_nValue = val;
            m_nSumDown = Unused;
            m_nSumRight = Unused;
        }

        /// <summary>
        /// create an element with one or two sums
        /// </summary>
        /// <param name="sumdown">Sum in downwards direction 
        /// (use Element.Unknown if irrelevant)</param>
        /// <param name="sumright">Sum to the right
        /// (use Element.Unknown if irrelevant)</param>
        public Element(int sumdown, int sumright)
        {
            m_nValue = Unused;
            m_nSumDown = sumdown;
            m_nSumRight = sumright;
        }

        public override string ToString()
        {
            if (HasValue)
            {
                if(Value==Unknown)
                    return "";
                else 
                    return Value.ToString();
            }
            else
            {
                return string.Format("{0}\\{1}",
                    HasSumDown ? SumDown.ToString() : "",
                    HasSumRight ? SumRight.ToString() : "");
            }
        }

        public int SumDown { get { return m_nSumDown; } }
        public int SumRight { get { return m_nSumRight; } }
        public int Value 
        { 
            get { return m_nValue; }
            set
            {
                if (m_nValue == Unused)
                    throw new InvalidOperationException("This cell cannot contain digit values");
                m_nValue = value;
            }
        }

        public bool HasValue { get { return (m_nValue != Unused); } }
        public bool HasSumDown { get { return (m_nSumDown != Unused); } }
        public bool HasSumRight { get { return (m_nSumRight != Unused); } }

        private int m_nValue;
        private int m_nSumDown;
        private int m_nSumRight;
    }
}