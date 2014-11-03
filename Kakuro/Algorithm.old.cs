using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace Kakuro
{
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
            get { return m_board[row, col]; }
            set { m_board[row, col] = value; }
        }

        private IStatusUpdater m_updater;
        private System.DateTime m_dtNextUpdate;
        private int m_nUpdateSeconds;
        private ulong m_nIterations;

        public bool Solve(IStatusUpdater updater, int nSeconds)
        {
            m_updater = updater;
            m_dtNextUpdate = System.DateTime.Now.AddSeconds(nSeconds);
            m_nUpdateSeconds = nSeconds;
            m_nIterations = 0;
            //return SolveAt(0, 0);
            bool bSolved = SolveIterate();
            updater.UpdateStatus();
            MessageBox.Show(string.Format("Number of iterations: {0}", m_nIterations));
            return bSolved;
        }

        private bool SolveIterate()
        {
            m_nIterations++;

            if (System.DateTime.Now > m_dtNextUpdate)
            {
                m_updater.UpdateStatus();
                m_dtNextUpdate = System.DateTime.Now.AddSeconds(m_nUpdateSeconds);
            }

            // find out what is the best element to work on
            int nRow, nCol, nOpts, baLegals;
            int nBestRow = -1, nBestCol = -1, nBestOpts = 100, baBestLegals = 0;
            for (nRow = 0; nRow < m_nRows; nRow++)
            {
                nOpts = 100;
                for (nCol = 0; nCol < m_nCols; nCol++)
                {
                    if (this[nRow, nCol].Value != Element.Unknown)
                        continue;
                    baLegals = FindOptions(nRow, nCol, out nOpts);
                    if (nOpts < nBestOpts)
                    {
                        nBestRow = nRow;
                        nBestCol = nCol;
                        nBestOpts = nOpts;
                        baBestLegals = baLegals;
                    }

                    if (nOpts == 0)
                        return false; // found contradiction

                    if (nOpts == 1)
                        break;
                }
                if (nOpts == 1)
                    break;
            }

            // ending condition
            if (nBestOpts == 100)
            {
                // we're done
                return true;
            }

            // work on the best element
            for (int curValue = 9; curValue >= 1; curValue--)
            {
                if ((baBestLegals & (1 << curValue)) == 0)
                    continue; // illegal option

                this[nBestRow, nBestCol].Value = curValue;

                // recurse
                if (SolveIterate())
                    return true;
            }

            // couldn't find any solution
            this[nBestRow, nBestCol].Value = Element.Unknown;
            return false;
        }

        /// <summary>
        /// Find and return all legal options for the specified element.
        /// We assume we are given a value-type element.
        /// </summary>
        /// <param name="nRow">Row of specified element</param>
        /// <param name="nCol">Column of specified element</param>
        /// <param name="nOpts">(out) number of legal options for the element</param>
        /// <returns>A bit array which contains the legal options for the specified element</returns>
        private int FindOptions(int nRow, int nCol, out int nOpts)
        {
            Debug.Assert(this[nRow, nCol].Value == Element.Unknown);

            // first, find information about the current column and row.
            int nColStart, nColEnd, nColSum; //NB: nColStart is the ROW in which the current column starts!
            int nRowStart, nRowEnd, nRowSum; //NB: nRowStart is the COLUMN in which the current row starts!

            // find the row in which the current column starts
            for (nColStart = nRow; this[nColStart - 1, nCol].HasValue; nColStart--)
                ;
            // find the row in which the current column ends
            for (nColEnd = nRow; nColEnd < m_nRows - 1 && this[nColEnd + 1, nCol].HasValue; nColEnd++)
                ;
            // find the column in which the current row starts
            for (nRowStart = nCol; this[nRow, nRowStart - 1].HasValue; nRowStart--)
                ;
            // find the column in which the current row ends
            for (nRowEnd = nCol; nRowEnd < m_nCols - 1 && this[nRow, nRowEnd + 1].HasValue; nRowEnd++)
                ;
            // find the expected sums for the current row and column
            nColSum = this[nColStart - 1, nCol].SumDown;
            nRowSum = this[nRow, nRowStart - 1].SumRight;
            Debug.Assert(nColSum != Element.Unused);
            Debug.Assert(nRowSum != Element.Unused);

            // find illegal values for the current element. 
            // these are stored in the bitarray illegalValues: 
            // value i is illegal iff bit i is 1
            int illegalValues = 0;

            // stage 1: must not appear in the current column
            // (we also use this pass to calculate the column sum so far, and the number
            // of unknown elements in the column)
            int colsum = 0;
            int nColElemsUnknown = 0;
            for (int i = nColStart; i <= nColEnd; i++)
            {
                int valHere = this[i, nCol].Value;
                if (valHere != Element.Unknown)
                {
                    illegalValues |= (1 << valHere);
                    colsum += valHere;
                }
                else
                {
                    nColElemsUnknown++;
                }
            }
            nColElemsUnknown--; // subtract one for the current element, which is always unknown

            // stage 2: must not appear in the current row
            // (we also use this pass to calculate the row sum so far)
            int rowsum = 0;
            int nRowElemsUnknown = 0;
            for (int i = nRowStart; i <= nRowEnd; i++)
            {
                int valHere = this[nRow, i].Value;
                if (valHere != Element.Unknown)
                {
                    illegalValues |= (1 << valHere);
                    rowsum += valHere;
                }
                else
                {
                    nRowElemsUnknown++;
                }
            }
            nRowElemsUnknown--; // subtract one for the current element, which is always unknown

            // stage 3: if we are in the last element of the column/row, 
            // we must behave differently, since there is only one possible value here
            int curValue = 0;
            if (nColElemsUnknown == 0)
            {
                curValue = nColSum - colsum;
                if (curValue < 1 || curValue > 9)
                {
                    // contradiction
                    nOpts = 0;
                    return 0;
                }
            }
            if (nRowElemsUnknown == 0)
            {
                int required = nRowSum - rowsum;
                if (required < 1 || required > 9)
                {
                    // contradiction
                    nOpts = 0;
                    return 0;
                }
                if (nColElemsUnknown != 0)
                    curValue = required;
                else
                {
                    if (curValue != required)
                    {
                        // contradiction between row and column requirements
                        nOpts = 0;
                        return 0;
                    }
                    else
                    {
                        // row and column requirements agree; continue
                    }
                }
            }

            // stage 4: count the number of options
            if (curValue != 0)
            {
                // we have a row or column requirement (i.e., maximum one legal value);
                // see if the required value is legal
                if ((illegalValues & (1 << curValue)) == 0)
                {
                    // digit doesn't yet appear
                    if (colsum + curValue + MinimumSum[nColElemsUnknown] <= nColSum &&
                        colsum + curValue + MaximumSum[nColElemsUnknown] >= nColSum)
                    {
                        // digit doesn't exceed expected column sum
                        if (rowsum + curValue + MinimumSum[nRowElemsUnknown] <= nRowSum &&
                            rowsum + curValue + MaximumSum[nRowElemsUnknown] >= nRowSum)
                        {
                            // digit doesn't exceed expected row sum
                            // we have a single legal value
                            nOpts = 1;
                            return (1 << curValue);
                        }
                    }
                }

                // contradiction
                nOpts = 0;
                return 0;
            }
            else
            {
                // count the number of legal options
                nOpts = 0;
                int retval = 0;

                for (curValue = 9; curValue >= 1; curValue--)
                {
                    // see if the this value is legal
                    if ((illegalValues & (1 << curValue)) == 0)
                    {
                        // digit doesn't yet appear
                        if (colsum + curValue + MinimumSum[nColElemsUnknown] <= nColSum &&
                            colsum + curValue + MaximumSum[nColElemsUnknown] >= nColSum)
                        {
                            // we're still in possible range for the expected column sum
                            if (rowsum + curValue + MinimumSum[nRowElemsUnknown] <= nRowSum &&
                                rowsum + curValue + MaximumSum[nRowElemsUnknown] >= nRowSum)
                            {
                                // we're still in possible range for the expected row sum
                                // this is a legal value
                                nOpts++;
                                retval |= (1 << curValue);
                            }
                        }
                    }
                }
                return retval;
            }
        }

        /// <summary>
        /// MinimumSum[n] : the smallest possible sum of n different positive integers
        /// </summary>
        private static int[] MinimumSum = new int[10] { 0, 1, 1 + 2, 1 + 2 + 3, 1 + 2 + 3 + 4, 1 + 2 + 3 + 4 + 5, 1 + 2 + 3 + 4 + 5 + 6, 1 + 2 + 3 + 4 + 5 + 6 + 7, 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8, 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 };
        /// <summary>
        /// MaximumSum[n] : the largest possible sum of n different positive integers
        /// </summary>
        private static int[] MaximumSum = new int[10] { 0, 9, 9 + 8, 9 + 8 + 7, 9 + 8 + 7 + 6, 9 + 8 + 7 + 6 + 5, 9 + 8 + 7 + 6 + 5 + 4, 9 + 8 + 7 + 6 + 5 + 4 + 3, 9 + 8 + 7 + 6 + 5 + 4 + 3 + 2, 9 + 8 + 7 + 6 + 5 + 4 + 3 + 2 + 1 };

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
                return SolveAt(nRow, nCol + 1);
            }

            // we are in a value-type element. see if we can make it work.
            // first, find information about the current column and row.
            int nColStart, nColEnd, nColSum; //NB: nColStart is the ROW in which the current column starts!
            int nRowStart, nRowEnd, nRowSum; //NB: nRowStart is the COLUMN in which the current row starts!

            // find the row in which the current column starts
            for (nColStart = nRow; this[nColStart - 1, nCol].HasValue; nColStart--)
                ;
            // find the row in which the current column ends
            for (nColEnd = nRow; nColEnd < m_nRows - 1 && this[nColEnd + 1, nCol].HasValue; nColEnd++)
                ;
            // find the column in which the current row starts
            for (nRowStart = nCol; this[nRow, nRowStart - 1].HasValue; nRowStart--)
                ;
            // find the column in which the current row ends
            for (nRowEnd = nCol; nRowEnd < m_nCols - 1 && this[nRow, nRowEnd + 1].HasValue; nRowEnd++)
                ;
            // find the expected sums for the current row and column
            nColSum = this[nColStart - 1, nCol].SumDown;
            nRowSum = this[nRow, nRowStart - 1].SumRight;
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

            int curValue = 0;
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
            SetValue(val);
        }

        /// <summary>
        /// Set to a value element
        /// </summary>
        public void SetValue(int val)
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
            SetSum(sumdown, sumright);
        }

        /// <summary>
        /// Change the element to a sum-type element
        /// </summary>
        /// <param name="sumdown">Sum in downwards direction 
        /// (use Element.Unknown if irrelevant)</param>
        /// <param name="sumright">Sum to the right
        /// (use Element.Unknown if irrelevant)</param>
        public void SetSum(int sumdown, int sumright)
        {
            m_nValue = Unused;
            m_nSumDown = sumdown;
            m_nSumRight = sumright;
        }

        public override string ToString()
        {
            if (HasValue)
            {
                if (Value == Unknown)
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