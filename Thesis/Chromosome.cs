using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyUtils;
using EvoOptimization;

namespace MyCellNet
{

    /// <summary>
    /// Chromosome contains one or more cells.  Each Chromosome contains the affinity bits, and is the level at which merging effects, 
    /// but merging is implemented at the Hunter level.
    /// Maintains class and affinity bits.
    /// Ultimately, the class bits determine what the cell is looking for.  This may not be utilized early on. For now, it's one bit:
    ///     0 means to vote Yes if the function returns true;
    ///     1 means to vote Yes if the function returns false
    /// Affinity bits (2 of them) refer to horizontal or vertical affinity.  They guide Merger.
    ///     These bits get ORed together in the breeding process to determine whether the resulting offspring is horizontal or vertical.
    ///     Vertical cells move into separate Chromosomes, horizontal cells combine using AND or AND NOT. 
    /// </summary>
    public class Chromosome
    {
        /// <summary>
        /// Various values for styles of crossover.
        /// </summary>
        public enum BreedMode : byte
        {
            MutationOnly = 1, OneCrossover = 2, DoubleCross = 4, UniformCross1 = 8,
            UniformCross2 = 16, UniformCross4 = 32, UniformCross8 = 64, RandomCross = 128
        }
        public enum Affinities : byte
        {
            dontCare = 0, last = 1, first = 2, complete = 3
        }
        public int ClassIndex
        {
            get
            {
                return classBits.BitsToString().BinaryStringToInt();
            }
        }
        public int NumCells
        {
            get
            {
                return cells.Count;
            }
        }
        static public BitArray AffinityBitsFromAffinity(Affinities a)
        {
            BitArray ret = new BitArray(2);
            switch (a)
            {
                case Affinities.complete:
                    ret[0] = true;
                    ret[1] = true;
                    break;
                case Affinities.dontCare:
                    ret[0] = false;
                    ret[1] = false;
                    break;
                case Affinities.first:
                    ret[0] = true;
                    ret[1] = false;
                    break;
                case Affinities.last:
                    ret[0] = false;
                    ret[1] = true;
                    break;
            }
            return ret;
        }
        const int _affinityBitLength = 2;
        public static int AffinityBitLength{get{ return _affinityBitLength; } }
        static int _classBitLength = 4;
        public static int ClassBitLength { get { return _classBitLength; } }
        bool notFlag;
        double mutability = 1.0d / Cell.CellLength;
        BitArray affinityBits;
        BitArray classBits; // Just one right now, but probably more to come (hence, bitArray and not bool)
        List<Cell> cells;
        /// <summary>
        /// Convenience accessor for 0 indexed cells, 0 being Chromosome->cell
        /// </summary>
        /// <param name="index">Traverses the list through index</param>
        /// <returns>The target cell</returns>
        public Cell this[int index]
        {
            get
            {
                return cells[index];

            }
        }
        private Cell GetCell(int p)
        {
            return cells[p];
        }

        public Chromosome(BitArray aff, BitArray cla)
        {
            
            affinityBits = new BitArray(aff);
            if(cla.Length != ClassBitLength)
            {
                classBits = new BitArray(ClassBitLength);
                for (int i = 0; i < _classBitLength; ++i)
                    classBits[i] = cla[i];
                ErrorCheck();
            }
            classBits = new BitArray(cla);
        }
        public void AddCell(Cell a)
        {

            if (cells == null) initWithCell(a);
            else cells.Add(a);
           
        }


        internal static void SetNumberOfClasses()
        {
            _classBitLength = (int) Math.Ceiling(Math.Log(OptoGlobals.NumberOfClasses, 2));

        }

        private void initWithCell(Cell a)
        {
            cells = new List<Cell>();
            cells.Add(new Cell(a));
        }
        public string Serialize()
        {
            StringBuilder ret = new StringBuilder("Aff:");
            for (int i = 0; i < affinityBits.Length; ++i) ret.Append(affinityBits[i] == true ? "1" : "0");
            ret.Append( "Cl:");
            for (int i = 0; i < classBits.Length; ++i) ret.Append(classBits[i] == true ? "1" : "0");
            ret.Append("Not: " + (notFlag ? "1 " : "0 "));
            ret.Append(string.Format("Cells:{0}", Environment.NewLine));
            foreach(Cell temp in cells){
                ret.Append(temp.Serialize() + " ");
            }
            ret.Append("endChromosome|"+Environment.NewLine);


            return ret.ToString();
        }

        public bool NotFlag
        {
            get
            {
                return notFlag;
            }
        }
        public BitArray AffinityBits
        {
            get { return affinityBits; }
        }

        public BitArray ClassBits
        {
            get
            {
                return classBits;
            }
            set
            {
                classBits = value;
            }
        }
        /// <summary>
        /// default constructor, really shouldn't be used outside of testing- 
        /// instead, supply your own Random.
        /// </summary>
        public Chromosome()
        {
            init();
        }
        public Chromosome(Cell a)
        {
            initArrays();
            initWithCell(a);
        }
        /// <summary>
        /// Initialize the BitArrays
        /// </summary>
        private void initArrays()
        {

            affinityBits = new BitArray(_affinityBitLength);
            classBits = new BitArray(_classBitLength);
            affinityBits = affinityBits.RerollBitArray(OptoGlobals.RNG);
            classBits = classBits.RerollBitArray(OptoGlobals.RNG);

            notFlag = OptoGlobals.RNG.Next() % 2 == 1 ? true : false;
            ErrorCheck();
        }
        /// <summary>
        /// Initialize the Cell
        /// </summary>
        private void initCell()
        {
            initWithCell(new Cell());
        }
        /// <summary>
        /// Initialize the Chromosome
        /// </summary>
        private void init()
        {
            initCell();
            initArrays();
            updateCellNum();
        }

        internal void ErrorCheck()
        {
            if (classBits.BitsToString().BinaryStringToInt() >= OptoGlobals.NumberOfClasses)
            {
                classBits = classBits.RerollBitArray(OptoGlobals.RNG);
                ErrorCheck();
            }
        }

        /// <summary>
        /// Parse the instructions in the component cells and return a bool representing the vote
        /// </summary>
        /// <returns>Boolean representing the vote of the chromosome</returns>
        public int Vote(object data, out double ret)
        {
            double lowest = ret = double.PositiveInfinity;
            bool vote = true;
           
            int classVote = classBits.BitsToString().BinaryStringToInt();
            foreach (Cell current in cells)            {
                vote = vote && current.Vote(data, out ret);
                ret = Math.Min(ret, lowest);
                if (vote == false || current.JoinBit) break;
            }
            if (NotFlag == true) vote = !vote;
            if (vote) return classVote;
            else return -1;

        }
        /// <summary>
        /// Passes the reset on to the list of cells
        /// </summary>

        /// <summary>
        /// Implements the following table for affinities:
        /// Affinity table- Affinity A, B, result (new affinity, Chromosome, conj. Chromosome)
        ///        00 00 00 A and[not] B
        ///        00 01 01 A and[not] B
        ///        00 10 10 B and[not] A
        ///        00 11 Chromosomes laid out vertically
        ///        01 00 01 B and[not] A
        ///        01 01 Chromosomes laid out vertically
        ///        01 10 11 B and[not] A
        ///        01 11 Chromosomes laid out vertically
        ///        10 00 10 A and[not] B
        ///        10 01 11 A and[not] B
        ///        10 10 Chromosomes laid out vertically
        ///        10 11 Chromosomes laid out vertically
        ///        11 00 Chromosomes laid out vertically
        ///        11 01 Chromosomes laid out vertically
        ///        11 10 Chromosomes laid out vertically
        ///        11 11 Chromosomes laid out vertically
        /// </summary>
        /// <param name="a">Chromosome A(above)</param>
        /// <param name="b">Chromosome B(above)</param>
        /// <returns></returns>
        public static Chromosome[] Merge(Chromosome a, Chromosome b)
        {

            Affinities aAffinity = (Affinities)a.affinityBits.BitsToString().BinaryStringToInt();
            Affinities bAffinity = (Affinities)b.affinityBits.BitsToString().BinaryStringToInt();
            BitArray result = AffinityBitsFromAffinity(aAffinity | bAffinity);
            Affinities order = aAffinity | bAffinity;
            Chromosome[] ret;
            if (aAffinity == Affinities.complete || bAffinity == Affinities.complete ||
                (aAffinity == bAffinity && bAffinity != Affinities.dontCare))
            {//if both are competing for the same position, vertical
                ret = new Chromosome[2];
                ret[0] = a.deepCopy();

                ret[1] = b.deepCopy();

            }
            else if (bAffinity == Affinities.first || aAffinity == Affinities.last)
            {  //B and [not] A
                ret = new Chromosome[1];
                ret[0] = new Chromosome(b.cells);
                ret[0] = JoinCells(ret[0], a);
                ret[0].affinityBits = AffinityBitsFromAffinity(bAffinity);
                ret[0].classBits = new BitArray(b.classBits);
            }
            else
            {
                ret = new Chromosome[1];
                ret[0] = new Chromosome(a.cells);
                ret[0]= JoinCells(ret[0], b);
                ret[0].affinityBits = AffinityBitsFromAffinity(aAffinity);
                ret[0].classBits = new BitArray(a.classBits);
                //A and [not] B
            }

            return ret;

        }

        private static Chromosome JoinCells(Chromosome chromosome, Chromosome append)
        {
            Chromosome ret = new Chromosome(chromosome.cells);
            foreach (Cell a in append.cells)
            {
                ret.cells.Add(a.DeepCopy());
            }
            return ret;
        }

        public Chromosome deepCopy()
        {
            Chromosome ret = new Chromosome();
            ret.cells = new List<Cell>();
            ret.classBits = new BitArray(classBits);
            ret.affinityBits = new BitArray(affinityBits);
            foreach(Cell x in cells)
            {
                ret.AddCell(x.DeepCopy());
            }
            ret.notFlag = NotFlag;
            return ret;
        }

        //Evolution Related functions:
        public void Mutate()
        {
            for (int i = 0; i < _classBitLength; ++i)
            {
                if (OptoGlobals.RNG.NextDouble() <= mutability)
                {
                    classBits[i] = !classBits[i];
                }
            }
            for (int i = 0; i < _affinityBitLength; ++i)
            {
                if (OptoGlobals.RNG.NextDouble() <= mutability)
                {
                    affinityBits[i] = !affinityBits[i];
                }
            }
            if (OptoGlobals.RNG.NextDouble() <= mutability) notFlag = !notFlag;
            foreach(Cell cell in cells)  cell.Mutate();
        }
        private static void CrossChromoSpecificBits(Chromosome a, Chromosome b, ref Chromosome target, ref Chromosome notTarget, Chromosome[] ret)
        {
            for (int i = 0; i < _classBitLength; i++)//The chromosome info makes sense to worry about order, wrt class/affinity bits
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance)
                    Util.switchTargets(a, b, ref target, ref notTarget);

                ret[0].classBits[i] = target.classBits[i];
                ret[1].classBits[i] = notTarget.classBits[i];
            }
            for (int i = 0; i < _affinityBitLength; i++)
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance)
                    Util.switchTargets(a, b, ref target, ref notTarget);
                ret[0].affinityBits[i] = target.affinityBits[i];
                ret[1].affinityBits[i] = notTarget.affinityBits[i];
            }
            if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance)
                Util.switchTargets(a, b, ref target, ref notTarget);
            ret[0].notFlag = target.NotFlag;
            ret[1].notFlag = notTarget.NotFlag;

        }
        private void JoinAllCells()
        {
            foreach (Cell temp in cells)  temp.JoinBit = true;
        }

        public static Chromosome[] CrossOver(Chromosome a, Chromosome b)
        {
            Chromosome target = a, notTarget = b;
            Chromosome[] ret = new Chromosome[2];
            ret[0] = new Chromosome();
            ret[1] = new Chromosome();
            a.updateCellNum(); b.updateCellNum();
            int least = Math.Min(a.NumCells, b.NumCells);
            CrossChromoSpecificBits(a, b, ref target, ref notTarget, ret);
            target = ret[0];
            notTarget = ret[1];
            ret[0].cells = null;
            ret[1].cells = null;
            //Chromosome crossover done, now for cells
            List<int> aCrossed = new List<int>(), bCrossed = new List<int>();

            int stop1, stop2;
            stop1 = OptoGlobals.RNG.Next(0, least);
            stop2 = OptoGlobals.RNG.Next(stop1, least);
            switch (OptoGlobals.CrossoverMode)
            {
                case OptoGlobals.CrossoverModes.Uniform:
                    for (int i = 0; i < least; ++i)
                    {
                        if (OptoGlobals.RNG.NextDouble() < OptoGlobals.CrossoverChance) Util.switchTargets(ret[0], ret[1], ref target, ref notTarget);
                        target.JoinCell(a[i]);
                        notTarget.JoinCell(b[i]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                    }
                    break;
                //Uniform, SinglePointChromosome, TwoPointChromosome
                case OptoGlobals.CrossoverModes.SinglePointChromosome:
                case OptoGlobals.CrossoverModes.TwoPointChromosome:
                    for (int i = 0; i < least; ++i)
                    {
                        if (i == stop1 ||
                            (OptoGlobals.CrossoverMode == OptoGlobals.CrossoverModes.TwoPointChromosome && i == stop2)) Util.switchTargets(ret[0], ret[1], ref target, ref notTarget);
                        target.JoinCell(a[i]);
                        notTarget.JoinCell(b[i]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                    }
                    break;
                default:
                    break;
            }


            //Now, all the common cells have been crossed over. 
            //update target to point to the chromosome with the most cells, which will be supplying the rest of the info
            List<int> mostCrossed;
            Chromosome mostCells;
            if (least == a.NumCells)
            {
                //if (aCrossed.Count == a.NumCells)

                mostCrossed = bCrossed;
                mostCells = b;
            }
            else
            {
                mostCrossed = aCrossed;
                mostCells = a;
            }
            while (mostCells.NumCells != mostCrossed.Count)//distribute remaining cells uniformly
            {
                int i = GetUnpickedInt(mostCells, mostCrossed);
                if (OptoGlobals.RNG.NextDouble() < OptoGlobals.CrossoverChance) Util.switchTargets(ret[0], ret[1], ref target, ref notTarget);
                target.JoinCell(mostCells[i]);
                mostCrossed.Add(i);
            }
            ret[0].JoinAllCells();
            ret[0].ErrorCheck();
            ret[1].JoinAllCells();
            ret[1].ErrorCheck();
            return ret;

        }




        public string DisplayText
        {
            get
            {
                string ret = "";

                ret += classBits.BitsToString();
                int count = 1;
                foreach (Cell temp in cells)
                {
                    ret += "Cell " + count++ + ": ";
                    ret += temp.BitsAsString();
                }
                ret += " ";
                ret += affinityBits.BitsToString();

                return ret;
            }
        }

        public void updateCellNum()
        {
            //cell.updateCellNum();
           // NumCells = cell.NumCells;
        }

        internal void resetAffinityBits()
        {
            affinityBits[0] = false;
            affinityBits[1] = false;
        }

        internal void JoinCell(Cell other)
        {
            if (cells == null)
            {
                initWithCell(other);
            }
            else
            {
                cells[cells.Count - 1].JoinBit = true;
                AddCell(other);
            }
        }
            private static int GetUnpickedInt(Chromosome a, List<int> aCrossed)
        {
            if (a.NumCells == aCrossed.Count) return -1;
            int i = OptoGlobals.RNG.Next(0, a.NumCells);
            while (aCrossed.Contains(i)) i = OptoGlobals.RNG.Next(0, a.NumCells);
            return i;
        }
    
    #region Human Readable Chromosome

            public static String[] affinityStrings = new String[4], classStrings = new String[16];
            static Chromosome()
            {
                affinityStrings[0] = "has no preference";
                affinityStrings[1] = "prefers the rear";
                affinityStrings[2] = "prefers the front";
                affinityStrings[3] = "considers itself complete";

            //Init classStringsHere


            }

        public Chromosome(List<Cell> cells)
        {
            foreach (Cell x in cells)
                AddCell(x.DeepCopy());
        }

        public String HumanReadableChromosome()
            {
                StringBuilder ret = new StringBuilder();
                int affinity = affinityBits.BitsToString().BinaryStringToInt();
                ret.AppendLine("\tThis chromosome " + affinityStrings[affinity]);
                ret.AppendLine("\tIt focuses on problems in the following class:");
                int Classes = classBits.BitsToString().BinaryStringToInt();
                ret.AppendLine("\t"+classStrings[Classes]);
                ret.AppendLine("\tBy aggregating the "+ (notFlag?"nay":"yes")+" votes from the following " + NumCells+  " cell"+(NumCells > 1?"s:":":"));
                foreach(Cell temp in cells) { 
                    ret.AppendLine("\t"+temp.HumanReadableCell());
                }
                return ret.ToString();

            }

        #endregion
    }
}
