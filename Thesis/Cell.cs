using System;
using System.Collections;
using System.Text;
using MyUtils;
using System.Diagnostics;
using EvoOptimization;

namespace Emerson.CSI.Applet.MHM.Internal.EvoAlgApplet
{
    /// <summary>
    /// The Cell is a building block for chromasomes.  At the simplest level, the cell consists of a function, upper and lower limits.
    /// As complexity increases (more cells get joined together) it also maintains join bits, and affinity bits.
    ///     
    /// The function bit corresponds to a hard-coded function the cell will call on the data.
    ///     These functions will return a double which contains how many standard deviations away from the norm the value is.
    /// The limit bits are checked against the value returned by the function.  If the value is within the limits, it will vote yes.  
    ///     If not, it will vote no.  There are 6 or so bits in each of the limit fields, formated as 11.1111; these 
    ///     code to standard deviations outside of the norm.  So a lower limit of 01.1000 and an upper limit of 11.0000 would code to 
    ///     if f(x) returns a value between 1.5 and 3 standard deviations from the norm,  then return (vote) YES.  If the class bit was NOT, 
    ///     it would reverse this vote.
    ///     If limits become invalid (lower > upper) they will simply be swapped using errorcheck().
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
        const int classBitStart = 0, classLength = 1;
        const int functionLength = 4, functionStart = 1;
        const int joinBitLength = 1;
        const int limitLength = 9;
        const int maskLength = 11;
        
        const int maskComponentLength = 6, maskOrientationLength = 3, maskLocationLength = 2;
        const int lowerLimitStart = functionStart + functionLength, upperLimitStart = lowerLimitStart + limitLength,
            maskStart = upperLimitStart + limitLength, joinBitStart = maskStart + maskLength;
        public const int    NumberOfFunctions = 1<<functionLength;
        const int powerOffset = 4;
        public const int CellLength = functionLength + (2 * limitLength) + classLength + maskLength + joinBitLength;
        public const int MaskComponentLength = maskComponentLength;
        #endregion
        //Member fields
        double lLimit = -10, uLimit = -10, mutability = 1;
        int numCells = 1;
        
        BitArray bits = new BitArray(CellLength);


        public Cell NextCell = null;
        public delegate double DataDelegate(object data);
        
        public static DataDelegate[] functions;
        /// <summary>
        /// Initialize delegates or use accessors and human readable labels
        /// </summary>
        static Cell()
        {
           
            functions = new DataDelegate[NumberOfFunctions];
            
            initDelegatesAndNames();


        }


        private static void initDelegatesAndNames()
        {

        }

        private static void initMaskLocationStrings()
        {

        }

        private static void initMaskOrientationStrings()
        {
         
        }
        public Cell(BitArray newBits, Cell nextCell)
        {
            bits = new BitArray(newBits);
            addCell(nextCell);
            ErrorCheck();
        }

        public string Serialize()
        {
            StringBuilder ret = new StringBuilder("Cell:");
            for (int i = 0; i < bits.Length; ++i) ret.Append(bits[i] == true ? "1" : "0");
            ret.Append("|");
            return ret.ToString();
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
                if(functions[index] != null && dd!= null) functions[index] -= dd;
            }
        }
        //Member Properties

        public bool NotFlag
        {
            get
            {
                return bits[0];
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
        internal  bool joinBit
        {
            get { return bits[joinBitStart]; }
            set { bits[joinBitStart] = value; }
        }
        public int NumCells
        {
            get
            {
                return numCells;
            }
        } //Make sure numCells is read only outside of Cell.
        public bool JoinBit { get { return joinBit; } }



        //Member functions

        //Constructors and constructor adjacent
        public Cell()
        {
            this.initBits();
        }
        /// <summary>
        /// This constructor is used in the crossover process to copy a cell without populating NextCell.
        /// </summary>
        /// <param name="temp">Cell to copy</param>
        public Cell(Cell temp)
        {
            if (temp != null)
            {
                bits = new BitArray(temp.bits);

            }
            NextCell = null;//Do not copy the other cells in line
            evalLimits();
            ErrorCheck();

        }

        private void initBits()
        {
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i] =  (OptoGlobals.RNG.Next() % 2 == 1 ? true : false);
            }
            joinBit = false;//just make 1 cell
            ErrorCheck();
        }
        /// <summary>
        /// Attaches the Cell other to this one in the list (if a cell is already in line, it gets tacked onto the end).  Joinbit is set.
        /// </summary>
        /// <param name="other"></param>
        public void joinCell(Cell other)
        {
            if (NextCell == null)//If this is the last cell in the chain
            {
                NextCell = other;
                joinBit = true;
            }
            else//if not, pass it down to the next cell
                NextCell.joinCell(other);
            ++numCells;//This is particularly important for the head cell to know how many it has trailing, for the chromosome.
        }
        /// <summary>
        /// Identical to joinCell, but the join bit is not altered
        /// </summary>
        /// <param name="other"></param>
        public void addCell(Cell other)
        {
            if (NextCell == null)//If this is the last cell in the chain
                NextCell = other;
            else//if not, pass it down to the next cell
                NextCell.addCell(other);
            ++numCells;
        }
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
                int rand = OptoGlobals.RNG.Next(lowerLimitStart, upperLimitStart);
                bits[rand] = !bits[rand];//randomly invert some bit, 
                if (bits[rand] == true)
                    swapLowerToUpper();//If bits[rand] is true, then it was 0, and the lower limit just got larger.
                
                //This block is reached one time in every 2^(2limitLength) cells, or for a limit length of 7, once every 16,384 cells.
            evalLimits();
            }
            Cell temp = this;
            int count = 0;
            while (temp != null)
            {
                temp = temp.NextCell;
                count++;
                if (count < 0)
                    throw new Exception("Infinite Loop in Cell");
            }
        }


        private void swapLowerToUpper()
        {
            BitArray temp = new BitArray(bits.Range(lowerLimitStart, upperLimitStart));
            for (int i = 0; i < limitLength; i++)
            {
                bits[lowerLimitStart + i] = bits[upperLimitStart + i];
                bits[upperLimitStart + i] = temp[i];
            }
            ErrorCheck();
        }

        //decoding functions
        public string BitsAsString()
        {
            return bits.BitsToString();
        }

        private string bitsInRange(uint start, uint end)
        {
            return bits.Range(start, end).BitsToString();
        }
        private string GetFunctionString()
        {
            return bitsInRange(functionStart, functionStart + functionLength);
        }
   
        private string getLowerLimitString()
        {
            return bitsInRange(lowerLimitStart, upperLimitStart);
        }
        private string getUpperLimitString()
        {
            return bitsInRange(upperLimitStart, maskStart);
        }



        //Evaluation related functions


        public bool Vote(object data, out double ret, DateTime cutoff)
        {

                bool vote = doFunction(GetFunctionString().BinaryStringToInt(), data,out ret, cutoff);
                if (NotFlag) vote = !vote;//not the vote, if the not flag is in effect

            return vote;
            
        }



        private bool doFunction(int value, object data, out double ret, DateTime cutoff)//This runs the detector function coded for.
        {
            bool vote;
            ret = functions[value](data);
            if (ret > LowerLimit && ret < UpperLimit) vote = true;
            else vote = false;
            
            return vote;
        }

        //Evolution related functions
        public void Mutate()
        {
            for (int i = 0; i < CellLength; i++)
            {
                if (OptoGlobals.RNG.NextDouble() <= mutability)
                    bits[i] = !bits[i];

            }
            ErrorCheck();//If upper limit <= lower limit, fix it
            if (NextCell != null) NextCell.Mutate();
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
                // if (a.OptoGlobals.RNG.NextDouble() <= EvoGlobals.CrossOverChance) switchTargets(a, b, ref target, ref notTarget);
                ret[0].bits[i] = target.bits[i];
                ret[1].bits[i] = notTarget.bits[i];
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
            Cell temp = this, retTemp = ret;
            while (temp != null)
            {
                //iterate through temp, assign retTemp, then increment retTemp
                if (temp.NextCell != null) retTemp.NextCell = new Cell(temp.NextCell);
                temp = temp.NextCell;
                retTemp = retTemp.NextCell;
            }

            ret.ErrorCheck();
            this.ErrorCheck();
            return ret;
        }
        /// <summary>
        /// Forces a recount of the cells in the 
        /// </summary>
        public void updateCellNum()
        {
            numCells = 0;
            Cell temp = this;
            while (temp != null)
            {
                numCells++;
                temp = temp.NextCell;
                if (numCells < 0) throw new Exception("Infinite Loop in Cell");
            }
            temp = this;
            int count = numCells;
            while (temp != null)
            {
                temp.numCells = count--;
                temp = temp.NextCell;
            }
        }
#region HumanReadableSegment
        private static String[] functionNames = new String[NumberOfFunctions], maskOrientationStrings = new String[8], 
             maskLocationStrings= new String[4], maskComponentStrings = new String[6];
        
        public String HumanReadableCell(){
            StringBuilder ret = new StringBuilder();
            ret.AppendLine("\tThis cell looks at " + functionNames[GetFunctionString().BinaryStringToInt()]);
            ret.AppendLine("\t\twhose return value "+ (NotFlag? "is between " : "is not between ") + LowerLimit+ 
                " and " + UpperLimit + " standard deviations");
            return ret.ToString();
            }




#endregion


    }
}
