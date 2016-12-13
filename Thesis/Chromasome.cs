using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyUtils;
using EvoOptimization;

namespace MyCellNet
{

    /// <summary>
    /// Chromasome contains one or more cells.  Each chromasome contains the affinity bits, and is the level at which merging effects, 
    /// but merging is implemented at the Hunter level.
    /// Maintains class and affinity bits.
    /// Ultimately, the class bits determine what the cell is looking for.  This may not be utilized early on. For now, it's one bit:
    ///     0 means to vote Yes if the function returns true;
    ///     1 means to vote Yes if the function returns false
    /// Affinity bits (2 of them) refer to horizontal or vertical affinity.  They guide Merger.
    ///     These bits get ORed together in the breeding process to determine whether the resulting offspring is horizontal or vertical.
    ///     Vertical cells move into separate chromasomes, horizontal cells combine using AND or AND NOT. 
    /// </summary>
    public class Chromasome
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
        private int numCells;
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
                return numCells;
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
        const int affinityBitLength = 2;
        public const int AffinityBitLength = affinityBitLength, ClassBitLength = classBitLength;
        const int classBitLength = 4;
        bool notFlag;
        double mutability = 1.0d / Cell.CellLength;
        BitArray affinityBits;
        BitArray classBits; // Just one right now, but probably more to come (hence, bitArray and not bool)
        Cell cell; //Cells are effectively a list
        /// <summary>
        /// Convenience accessor for 0 indexed cells, 0 being Chromosome->cell
        /// </summary>
        /// <param name="index">Traverses the list through index</param>
        /// <returns>The target cell</returns>
        public Cell this[int index]
        {
            get
            {
                return this.GetCell(index);

            }
        }
        private Cell GetCell(int p)
        {
            if (p > numCells) throw new ArgumentOutOfRangeException("Cell is out of range");
            Cell ret = cell;
            for (int i = 0; i < p; ++i)
            {
                ret = ret.NextCell;
            }
            return new Cell(ret);
        }
        public Chromasome(BitArray aff, BitArray cla)
        {
            
            affinityBits = new BitArray(aff);
            classBits = new BitArray(cla);
        }
        public void AddCell(Cell a)
        {

            if (cell == null) initWithCell(a);
            else cell.addCell(a);
           
        }

        private void initWithCell(Cell a)
        {
            cell = new Cell(a);
        }
        public string Serialize()
        {
            StringBuilder ret = new StringBuilder("Aff:");
            for (int i = 0; i < affinityBits.Length; ++i) ret.Append(affinityBits[i] == true ? "1" : "0");
            ret.Append( "Cl:");
            for (int i = 0; i < classBits.Length; ++i) ret.Append(classBits[i] == true ? "1" : "0");
            ret.Append("Not: " + (notFlag ? "1 " : "0 "));
            ret.Append(string.Format("Cells:{0}", Environment.NewLine));
            Cell temp = cell;
            while (temp != null)
            {
                ret.Append(temp.Serialize() + " ");
                temp = temp.NextCell;
            }
            ret.Append("endChromasome|"+Environment.NewLine);


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
        public Chromasome()
        {
            init();
        }
        public Chromasome(Cell a)
        {
            initArrays();
            cell = a;
        }
        /// <summary>
        /// Initialize the BitArrays
        /// </summary>
        private void initArrays()
        {
            affinityBits = new BitArray(affinityBitLength);
            for (int i = 0; i < affinityBitLength; i++) affinityBits[i] = (OptoGlobals.RNG.Next() % 2 == 1 ? true : false);
            classBits = new BitArray(classBitLength);
            for (int i = 0; i < classBitLength; i++) classBits[i] = (OptoGlobals.RNG.Next() % 2 == 1 ? true : false);
            notFlag = OptoGlobals.RNG.Next() % 2 == 1 ? true : false;
        }
        /// <summary>
        /// Initialize the Cell
        /// </summary>
        private void initCell()
        {
            cell = new Cell();
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
        /// <summary>
        /// Parse the instructions in the component cells and return a bool representing the vote
        /// </summary>
        /// <returns>Boolean representing the vote of the chromosome</returns>
        public int Vote(object data, out double ret, DateTime cutoff)
        {
            double lowest = ret = double.PositiveInfinity;
            bool vote = true;
            Cell current = cell;
            int returnVal = classBits.BitsToString().BinaryStringToInt();
            while (current != null)//step through the cells
            {
                vote = vote && current.Vote(data, out ret, cutoff);
                ret = Math.Min(ret, lowest);
                if (vote == false) break;
                if (current.JoinBit == false) break;//If a join bit is turned off, break the vote
                current = current.NextCell;
            }
            if (NotFlag == true) vote = !vote;
            if (vote) return returnVal;
            else return -1;

        }
        /// <summary>
        /// Passes the reset on to the list of cells
        /// </summary>

        /// <summary>
        /// Implements the following table for affinities:
        /// Affinity table- Affinity A, B, result (new affinity, chromasome, conj. chromasome)
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
        public static Chromasome[] Merge(Chromasome a, Chromasome b)
        {

            Affinities aAffinity = (Affinities)a.affinityBits.BitsToString().BinaryStringToInt();
            Affinities bAffinity = (Affinities)b.affinityBits.BitsToString().BinaryStringToInt();
            BitArray result = AffinityBitsFromAffinity(aAffinity | bAffinity);
            Affinities order = aAffinity | bAffinity;
            Chromasome[] ret;
            if (aAffinity == Affinities.complete || bAffinity == Affinities.complete ||
                (aAffinity == bAffinity && bAffinity != Affinities.dontCare))
            {//if both are competing for the same position, vertical
                ret = new Chromasome[2];
                ret[0] = a.deepCopy();

                ret[1] = b.deepCopy();

            }
            else if (bAffinity == Affinities.first || aAffinity == Affinities.last)
            {  //B and [not] A
                ret = new Chromasome[1];
                ret[0] = new Chromasome(b.cell.DeepCopy());
                ret[0].cell.joinCell(a.cell.DeepCopy());
                ret[0].affinityBits = AffinityBitsFromAffinity(bAffinity);
                ret[0].classBits = b.classBits;
                ret[0].updateCellNum();
            }
            else
            {
                ret = new Chromasome[1];
                ret[0] = new Chromasome(a.cell.DeepCopy());
                ret[0].cell.joinCell(b.cell.DeepCopy());
                ret[0].affinityBits = AffinityBitsFromAffinity(aAffinity);
                ret[0].classBits = a.classBits;
                ret[0].updateCellNum();
                //A and [not] B
            }

            return ret;

        }

        public Chromasome deepCopy()
        {
            Chromasome ret = new Chromasome();
            ret.classBits = new BitArray(classBits);
            ret.affinityBits = new BitArray(affinityBits);
            ret.cell = cell.DeepCopy();
            ret.updateCellNum();
            ret.notFlag = NotFlag;
            return ret;
        }

        //Evolution Related functions:
        public void Mutate()
        {
            for (int i = 0; i < classBitLength; ++i)
            {
                if (OptoGlobals.RNG.NextDouble() <= mutability)
                {
                    classBits[i] = !classBits[i];
                }
            }
            for (int i = 0; i < affinityBitLength; ++i)
            {
                if (OptoGlobals.RNG.NextDouble() <= mutability)
                {
                    affinityBits[i] = !affinityBits[i];
                }
            }
            if (OptoGlobals.RNG.NextDouble() <= mutability) notFlag = !notFlag;
            this.cell.Mutate();
        }
        private static void CrossChromaSpecificBits(Chromasome a, Chromasome b, ref Chromasome target, ref Chromasome notTarget, Chromasome[] ret)
        {
            for (int i = 0; i < classBitLength; i++)//The chromosome info makes sense to worry about order, wrt class/affinity bits
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance)
                    Util.switchTargets(a, b, ref target, ref notTarget);

                ret[0].classBits[i] = target.classBits[i];
                ret[1].classBits[i] = notTarget.classBits[i];
            }
            for (int i = 0; i < affinityBitLength; i++)
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
            Cell temp = cell;
            while (temp.NextCell != null)
            {
                temp.joinBit = true;
                temp = temp.NextCell;
            }
        }

        public static Chromasome[] CrossOver(Chromasome a, Chromasome b)
        {
            Chromasome target = a, notTarget = b;
            Chromasome[] ret = new Chromasome[2];
            ret[0] = new Chromasome();
            ret[1] = new Chromasome();
            a.updateCellNum(); b.updateCellNum();
            int least = Math.Min(a.numCells, b.numCells);
            CrossChromaSpecificBits(a, b, ref target, ref notTarget, ret);
            target = ret[0];
            notTarget = ret[1];
            ret[0].cell = null;
            ret[1].cell = null;
            //Chromasome crossover done, now for cells
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
                //Uniform, SinglePointChromasome, TwoPointChromasome
                case OptoGlobals.CrossoverModes.SinglePointChromasome:
                case OptoGlobals.CrossoverModes.TwoPointChromasome:
                    for (int i = 0; i < least; ++i)
                    {
                        if (i == stop1 ||
                            (OptoGlobals.CrossoverMode == OptoGlobals.CrossoverModes.TwoPointChromasome && i == stop2)) Util.switchTargets(ret[0], ret[1], ref target, ref notTarget);
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
            Chromasome mostCells;
            if (least == a.numCells)
            {
                //if (aCrossed.Count == a.numCells)

                mostCrossed = bCrossed;
                mostCells = b;
            }
            else
            {
                mostCrossed = aCrossed;
                mostCells = a;
            }
            while (mostCells.numCells != mostCrossed.Count)//distribute remaining cells uniformly
            {
                int i = GetUnpickedInt(mostCells, mostCrossed);
                if (OptoGlobals.RNG.NextDouble() < OptoGlobals.CrossoverChance) Util.switchTargets(ret[0], ret[1], ref target, ref notTarget);
                target.JoinCell(mostCells[i]);
                mostCrossed.Add(i);
            }
            ret[0].JoinAllCells();
            ret[1].JoinAllCells();
            return ret;

        }




        public string DisplayText
        {
            get
            {
                string ret = "";

                ret += classBits.BitsToString();
                Cell temp = cell;
                int count = 1;
                while (temp != null)
                {
                    ret += "Cell " + count++ + ": ";
                    ret += temp.BitsAsString();
                    temp = temp.NextCell;
                }
                ret += " ";
                ret += affinityBits.BitsToString();

                return ret;
            }
        }

        public void updateCellNum()
        {
            cell.updateCellNum();
            numCells = cell.NumCells;
        }

        internal void resetAffinityBits()
        {
            affinityBits[0] = false;
            affinityBits[1] = false;
        }

        internal void JoinCell(Cell other)
        {
            if (cell == null)
            {
                initWithCell(other);
                numCells = 1;
            }
            else
            {
                cell.joinCell(other);
                ++numCells;
            }
        }
            private static int GetUnpickedInt(Chromasome a, List<int> aCrossed)
        {
            if (a.numCells == aCrossed.Count) return -1;
            int i = OptoGlobals.RNG.Next(0, a.numCells);
            while (aCrossed.Contains(i)) i = OptoGlobals.RNG.Next(0, a.numCells);
            return i;
        }
    
    #region Human Readable Chromosome

            public static String[] affinityStrings = new String[4], classStrings = new String[16];
            static Chromasome()
            {
                affinityStrings[0] = "has no preference";
                affinityStrings[1] = "prefers the rear";
                affinityStrings[2] = "prefers the front";
                affinityStrings[3] = "considers itself complete";

            //Init classStringsHere


            }

            public String HumanReadableChromosome()
            {
                StringBuilder ret = new StringBuilder();
                int affinity = affinityBits.BitsToString().BinaryStringToInt();
                ret.AppendLine("\tThis chromosome " + affinityStrings[affinity]);
                ret.AppendLine("\tIt focuses on problems in the following class:");
                int Classes = classBits.BitsToString().BinaryStringToInt();
                ret.AppendLine("\t"+classStrings[Classes]);
                ret.AppendLine("\tBy aggregating the "+ (notFlag?"nay":"yes")+" votes from the following " + numCells+  " cell"+(numCells > 1?"s:":":"));
                Cell temp = cell;
                while (temp != null)
                {
                    ret.AppendLine("\t"+temp.HumanReadableCell());
                    temp = temp.NextCell;
                }
                return ret.ToString();

            }
    #endregion
    }
}
