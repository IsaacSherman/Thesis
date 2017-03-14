using System;
using System.Collections;
using System.Text;
using MyUtils;
using System.Diagnostics;
using EvoOptimization;
using System.IO;

namespace MyCellNet
{
    /// <summary>
    /// The Cell is a building block for Chromosomes.  At the simplest level, the cell consists of a function, upper and lower limits.
    /// As complexity increases (more cells get joined together) it also maintains join bits, and affinity bits.
    ///     
    /// The function bit corresponds to a hard-coded function the cell will call on the data.
    ///     These functions will return a double which contains how many standard deviations away from the norm the value is.
    /// The limit bits are checked against the value returned by the function.  If the value is within the limits, it will vote yes.  
    ///     If not, it will vote no.  There are 6 or so bits in each of the limit fields, formated as 11.1111; these 
    ///     code to standard deviations outside of the norm.  So a lower limit of 01.1000 and an upper limit of 11.0000 would code to 
    ///     if f(x) returns a value between 1.5 and 3,  then return (vote) YES.  If the class bit was NOT, 
    ///     it would reverse this vote.
    ///     If limits become invalid (lower > upper) they will simply be swapped during errorcheck().
    /// The join bit refers to where the cell links up with another cell.  Note that while joinbit is encoded, NextCell is separate;
    ///     NextCell is a pointer to a cell if a cell exists (so if joinbit is true, then NextCell is not null) but if joinbit is false, 
    ///     NextCell may or may not be null.  Thus, when stepping through the list for mutations and whatnot, there may be a chain of dead cells which
    ///     may be reactivated in a later generation.  This should facilitate escaping local optima.
    ///     0 means there is no following cell.
    ///     1 means there is a following cell, and the votes will be ANDed together in conjunction with the following cell's class bit
    /// Voting is performed with each vertical set of cells getting one vote.
    ///     These votes are merged into a single vote for the chromosome, all of which 
    ///     are then aggregated and turned into a single vote for the hunter.
    /// 
    /// </summary>
    public class Cell
    {
        #region Constants

        //


        //Bits resolve as follows: NotFlag, Function, Lower Limit, Upper Limit
        static private int _notFlagStart, _functionStart, _lLimitStart, _uLimitStart, _functionLength, _notFlagLength = 1, limitLength = 8, _joinBitStart, powerOffset = 0;
        static public int CellLength, NumberOfFunctions;

        /// <summary>
        /// Handles initialization of static ints, which revolve around the number of functions, in this case features possible given the data.
        /// Initialize delegates or use accessors and human readable labels
        /// </summary>
        static Cell()
        {
            reinforceConstantRelations();

        }

        private static void reinforceConstantRelations()
        {
            _notFlagStart = 0;
            _functionStart = _notFlagStart + _notFlagLength;
            NumberOfFunctions = OptoGlobals.NumberOfFeatures;
            _functionLength = (int)Math.Ceiling(Math.Log(NumberOfFunctions, 2)); //So our cells will have enough bits to carry the necessary number of functions
            _lLimitStart = _functionStart + _functionLength;
            _uLimitStart = _lLimitStart + limitLength;
            _joinBitStart = _uLimitStart + limitLength;
            CellLength = _joinBitStart + _notFlagLength;
            functions = new DataDelegate[NumberOfFunctions];
            initDelegatesAndNames();
        }

        protected void rerollBits(int start, int end)
        {
            for (int i = start; i < end; ++i)
            {
                _bits[i] = OptoGlobals.RNG.NextDouble() < .5;
            }
        }
        #endregion
        //Member fields
        double lLimit = -10, uLimit = -10;
        int numCells = 1;

        BitArray _bits = new BitArray(CellLength);


        public delegate double DataDelegate(object data);

        public static DataDelegate[] functions;



        private static void initDelegatesAndNames()
        {
            functions = new DataDelegate[OptoGlobals.NumberOfFeatures];
            for (int i = 0; i < OptoGlobals.NumberOfFeatures; ++i)
            {
                functions[i] = DelegateBuilder.SimpleLookup(i);
            }
        }

        public Cell(BitArray newBits, Cell nextCell)
        {
            _bits = new BitArray(newBits);
            //addCell(nextCell);
            ErrorCheck();
        }

        public string Serialize()
        {
            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < _bits.Length; ++i) ret.Append(_bits[i]? "1" : "0");
            return ret.ToString();
        }


        public Cell(string x):this()
        {
            _bits = new BitArray(x.Length);
            for (int i = 0; i < x.Length; ++i) _bits[i] = x[i] == '1';
            evalLimits();
        }

        public string FunctionIndex
        {
            get
            {
                return GetFunctionString().BinaryStringToInt().ToString();
            }
        }


        public static DataDelegate GetFunction(int index)
        {
            if (index >= NumberOfFunctions || index < 0) throw new IndexOutOfRangeException();
            else return functions[index];
        }
        /// <summary>
        /// Sets the function at index to use dd.  Note that this uses +=, so multicasting is possible.
        /// </summary>
        /// <param name="index">index of the function</param>
        /// <param name="dd">DataDelegate of the form "double dd (object)"</param>
        public static void SetFunction(int index, DataDelegate dd)
        {
            if (index >= NumberOfFunctions || index < 0) throw new IndexOutOfRangeException();
            else
            {
                functions[index] += dd;
            }
        }
        /// <summary>
        /// Sets the function at index to use dd.  Note that this uses -=.
        /// </summary>
        /// <param name="index">index of the function</param>
        /// <param name="dd">DataDelegate of the form "double dd (object)"</param>
        public static void RemoveFunction(int index, DataDelegate dd)
        {
            if (index >= NumberOfFunctions || index < 0) throw new IndexOutOfRangeException();
            else
            {
                if (functions[index] != null && dd != null) functions[index] -= dd;
            }
        }
        //Member Properties

        public bool NotFlag
        {
            get
            {
                return _bits[0];
            }

        }
        public double UpperLimit
        {
            get
            {
                return uLimit;
            }
        }
        public double LowerLimit
        {
            get
            {
                return lLimit;
            }
        }

        public bool JoinBit
        {
            get { return _bits[_bits.Count - 1]; }
            set { _bits[_bits.Count - 1] = value; }

        }

        //Member functions

        //Constructors and constructor adjacent
        public Cell()
        {
            initBits();
        }
        /// <summary>
        /// This constructor is used in the crossover process to copy a cell without populating NextCell.
        /// </summary>
        /// <param name="temp">Cell to copy</param>
        public Cell(Cell temp)
        {
            string previousString = temp.HumanReadableCell();
            if (temp != null)
            {
                _bits = new BitArray(temp._bits);

            }
            evalLimits();
            ErrorCheck();
            string newString = HumanReadableCell();
            Debug.Assert(newString == previousString);

        }

        private void initBits()
        {
            for (int i = 0; i < _bits.Length; i++)
            {
                _bits[i] = (OptoGlobals.RNG.Next() % 2 == 1 ? true : false);
            }
            JoinBit = true;
            ErrorCheck();
        }
        ///// <summary>
        ///// Attaches the Cell other to this one in the list (if a cell is already in line, it gets tacked onto the end).  Joinbit is set.
        ///// </summary>
        ///// <param name="other"></param>
        //public void joinCell(Cell other)
        //{
        //    if (NextCell == null)//If this is the last cell in the chain
        //    {
        //        NextCell = other;
        //        joinBit = true;
        //    }
        //    else//if not, pass it down to the next cell
        //        NextCell.joinCell(other);
        //}
        ///// <summary>
        ///// Identical to joinCell, but the join bit is not altered
        ///// </summary>
        ///// <param name="other"></param>
        //public void addCell(Cell other)
        //{
        //    if (NextCell == null)//If this is the last cell in the chain
        //        NextCell = other;
        //    else//if not, pass it down to the next cell
        //        NextCell.addCell(other);
        //    ++numCells;
        //}
        private void evalLimits()
        {

            uLimit = getUpperLimitString().BinaryStringToDouble(powerOffset - limitLength);
            lLimit = getLowerLimitString().BinaryStringToDouble(powerOffset - limitLength);

        }
        public void ErrorCheck()
        {
            evalLimits();

            if (LowerLimit > UpperLimit)
            {
                swapLowerToUpper();
            }
            else if (LowerLimit == UpperLimit)
            {
                int rand = OptoGlobals.RNG.Next(_lLimitStart, _uLimitStart);
                _bits[rand] = !_bits[rand];//randomly invert some bit, 
                if (_bits[rand] == true)
                    swapLowerToUpper();//If bits[rand] is true, then it was 0, and the lower limit just got larger.

                //This block is reached one time in every 2^(2limitLength) cells, or for a limit length of 7, once every 16,384 cells.
                evalLimits();
            }

            if (bitsInRange(_functionStart, _functionStart + _functionLength).BinaryStringToInt() >= NumberOfFunctions)
            {
                rerollBits(_functionStart, _functionStart + _functionLength);
                ErrorCheck();
                return;
            }

        }


        private void swapLowerToUpper()
        {
            BitArray temp = new BitArray(_bits.Range((uint)_lLimitStart, (uint)_uLimitStart));
            for (int i = 0; i < limitLength; i++)
            {
                _bits[_lLimitStart + i] = _bits[_uLimitStart + i];
                _bits[_uLimitStart + i] = temp[i];
            }
            ErrorCheck();
        }

        //decoding functions
        public string BitsAsString()
        {
            return _bits.BitsToString();
        }


        private string bitsInRange(int start, int end)
        {
            return bitsInRange((uint)start, (uint)end);
        }

        private string bitsInRange(uint start, uint end)
        {
            return _bits.Range(start, end).BitsToString();
        }
        private string GetFunctionString()
        {
            return bitsInRange(_functionStart, _functionStart + _functionLength);
        }

        private string getLowerLimitString()
        {
            return bitsInRange(_lLimitStart, _uLimitStart);
        }
        private string getUpperLimitString()
        {
            return bitsInRange(_uLimitStart, _joinBitStart);
        }



        //Evaluation related functions


        public bool Vote(object data, out double ret)
        {

            bool vote = doFunction(GetFunctionString().BinaryStringToInt(), data, out ret);
            if (NotFlag) vote = !vote;//not the vote, if the not flag is in effect

            return vote;

        }

        private bool doFunction(int value, object data, out double ret)//This runs the detector function coded for.
        {
            bool vote;
            ret = functions[value](data);
            vote = (ret > LowerLimit && ret < UpperLimit);
            
            return vote;
        }

        //Evolution related functions
        public void Mutate()
        {
            for (int i = 0; i < CellLength; i++)
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.MutationChance)
                    _bits[i] = !_bits[i];

            }
            ErrorCheck();//If upper limit <= lower limit, fix it
//            if (NextCell != null) NextCell.Mutate();
        }




        public static Cell[] Crossover(Cell a, Cell b)
        {
            if (a == null || b == null) return null;
            Cell target = a, notTarget = b;
            Cell[] ret = new Cell[2];
            ret[0] = new Cell();
            ret[1] = new Cell();
            for (int i = 0; i < CellLength; ++i)//it makes sense at this level to use ordered iteration
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(a, b, ref target, ref notTarget);
                ret[0]._bits[i] = target._bits[i];
                ret[1]._bits[i] = notTarget._bits[i];
            }
            ret[0].ErrorCheck();
            ret[1].ErrorCheck();
            return ret;
        }
        private static void switchTargets(Cell a, Cell b, ref Cell target, ref Cell notTarget)
        {
            if (target == a)
            {
                target = b;
                notTarget = a;
            }
            else
            {
                target = a;
                notTarget = b;
            }
        }
        /// <summary>
        /// Explicitly copies all the cells in the line.
        /// </summary>
        /// <returns>A new Cell which is a deep copy of this</returns>
        public Cell DeepCopy()
        {
            Cell ret = new Cell(this);
            return ret;
        }
        /// <summary>
        /// Forces a recount of the cells in the 
        /// </summary>

        #region HumanReadableSegment


        public String HumanReadableCell(){
            StringBuilder ret = new StringBuilder();
            ret.AppendLine("\tThis cell looks at feature " + GetFunctionString().BinaryStringToInt());
            ret.AppendLine("\t\twhose return value "+ (NotFlag? "is between " : "is not between ") + LowerLimit+ 
                " and " + UpperLimit + " standard deviations");
            return ret.ToString();
            }

        internal static void SetNumberOfFeatures()
        {
            NumberOfFunctions = OptoGlobals.NumberOfFeatures;
            reinforceConstantRelations();
        }




        #endregion



        internal static Cell CellFromHumanReadableCell(string hrc)
        {
            Cell ret = new Cell();
            StringReader sin = new StringReader(hrc);
            string line = sin.ReadLine();
            int featureIndex = line.IndexOf("feature")+7;
            line = line.Substring(featureIndex, line.Length-featureIndex).Trim();
            int feature = Int16.Parse(line);
            BitArray featureBits = Util.BitsFromInt(Cell._functionLength, feature);
            for (int i = 0; i < featureBits.Length; ++i)
            {
                ret._bits[_functionStart + i] = featureBits[i];
            }
            line = sin.ReadLine();
            string[] tokens = { "between ", " and " }, lims;
            double llim, ulim;
            lims = line.Split(tokens, StringSplitOptions.RemoveEmptyEntries);
            llim = double.Parse(lims[1].Trim());
            ulim = double.Parse(lims[2].Substring(0, lims[2].IndexOf(" standard")));
            llim *= 1 << limitLength;
            ulim *= 1 << limitLength;
            BitArray llimBits, ulimBits;
            llimBits = Util.BitsFromInt(limitLength, (int)llim);
            ulimBits = Util.BitsFromInt(limitLength, (int)ulim);
            for (int i = 0; i < llimBits.Length; ++i)
            {
                ret._bits[_lLimitStart + i] = llimBits[i];
                ret._bits[_uLimitStart + i] = ulimBits[i];
            }
            bool notFlag = !lims[0].Contains(" not");
            ret._bits[_notFlagStart] = notFlag;

            return ret;
        }
    }
}
