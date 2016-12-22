using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using MyUtils;
using System.Diagnostics;
using EvoOptimization;

namespace MyCellNet
{



    /// <summary>
    /// The hunter simply holds all the chromosomes and coordinates their function. 
    /// No new data is added, but it does implement some methods.  I may include fitness for IComparable.  
    /// Breed, vote, and merge (though merger may become its own class)
    /// </summary>
    public class Hunter : IComparable<Hunter>
    {
        List<Chromosome> myChromasomes;
        public int NumChromasomes { get { return myChromasomes.Count; } }
        public string tag="";
        public double Fitness = 0;
        private double accuracy = 0;

        public double Complexity
        {
            get
            {
                double total = 0;
                foreach (Chromosome x in myChromasomes)
                {
                    total += x.NumCells;
                }
                return total;
            }
        }
        public Hunter()
        {
            initChromasomes();
        }
        public Hunter(int chromanum, int cells = 1)
        {
            myChromasomes = new List<Chromosome>(chromanum);
            for (int i = 0; i < chromanum; ++i)
            {
                myChromasomes.Add(new Chromosome());
                for (int j = 1; j < cells; ++j)
                {
                    myChromasomes[i].JoinCell(new Cell());
                }
                updateCellNum();
            }
        }

        public void overWriteChromaClassBits(ref int num)
        {
            Debug.Assert(myChromasomes.Count != 0);
            for (int i = 0; i < myChromasomes.Count; ++i)
            {
                BitArray temp = Util.BitsFromInt(Chromosome.ClassBitLength, num++%16);
                myChromasomes[i].ClassBits = temp;

            }
        }

        protected Hunter(Chromosome x)
        {
            initChromasomes();
            myChromasomes.RemoveAt(0);
            this.AddChromasome(x);
        }

        public Hunter EliteCopy()
        {
            Hunter ret = new Hunter().StripChromasomes();//initializes everything, empties chromosome
            foreach (Chromosome x in myChromasomes)
            {
                ret.myChromasomes.Add(x.deepCopy());
            }
            return ret;

        }

        public void AddChromasome(Chromosome x)
        {
            x.updateCellNum();
            myChromasomes.Add(x.deepCopy());

        }
        private void initChromasomes()
        {
            myChromasomes = new List<Chromosome>();
            Chromosome temp = new Chromosome();
            myChromasomes.Add(temp);
            temp.updateCellNum();

        }
        /// <summary>
        /// Takes two Hunters and merges them. Note that this effectively doubles to complexity of offspring, so it is necessary 
        /// to make this event relatively rare (in 5 generations, 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Hunter Merger(Hunter first, Hunter second)
        {

            int firstNum = first.NumChromasomes, secondNum = second.NumChromasomes;
            int min = Math.Min(firstNum, secondNum);
            Hunter ret;
            Chromosome[] temp;
            //Step through, merging each pair, until we run out of matched pairs
            temp = Chromosome.Merge(first.myChromasomes[0], second.myChromasomes[0]);
            ret = new Hunter().StripChromasomes();
            for (int i = 0; i < min; i++)
            {
                if (ret.Complexity > OptoGlobals.ComplexityCap) break;
                foreach (Chromosome x in temp)
                {
                    ret.AddChromasome(x);
                    if (ret.Complexity > OptoGlobals.ComplexityCap) return ret;
                }

            }
            //At this point, either first or second still has chromosomes left over, we'll just copy them in
            Hunter target = (min == firstNum ? second : first);
            int max = Math.Max(firstNum, secondNum);
            for (int i = min; i < max; ++i) ret.AddChromasome(target.myChromasomes[i]);
            ret.tag = first.tag + "-+-" + second.tag;


            return ret;

 /*           int firstNum = first.NumChromasomes, secondNum = second.NumChromasomes;
            int min = Math.Min(firstNum, secondNum);
            Hunter ret;
            Chromasome[] temp;
            //Step through, merging each pair, until we run out of matched pairs
            temp = Chromasome.Merge(first.myChromasomes[0], second.myChromasomes[0]);
            ret = new Hunter().StripChromasomes();
            for (int i = 0; i < min; i++)
            {
                if (ret.Complexity > OptoGlobals.ComplexityCap) break;
                foreach (Chromasome x in temp)
                {
                    ret.AddChromasome(x);
                    if (ret.Complexity > OptoGlobals.ComplexityCap) return ret;
                }
                
            }
            //At this point, either first or second still has chromosomes left over, we'll just copy them in
            Hunter target = (min == firstNum ? second : first);
            int max = Math.Max(firstNum, secondNum);
            for (int i = min; i < max; ++i) ret.AddChromasome(target.myChromasomes[i]);


            
            return ret;
            */

        }
        /// <summary>
        /// Vote needs a parameter, it's what the cell actually votes on, but I don't know what that looks like yet.
        /// </summary>
        /// <returns>Whether the Hunter attacks or not.</returns>
        public int Vote(object data, out double ret, DateTime cutoff)
        {
            ret = 0;
            //So we need an array of ints which hold the counts for each class
            int[] results = new int[OptoGlobals.NumberOfClasses];
            for (int i = 0; i < myChromasomes.Count; ++i){
                int temp = myChromasomes[i].Vote(data, out ret, cutoff);
                if (temp >= 0) results[temp] += myChromasomes[i].NumCells;
                }
            int maxVotes = results.Max();
            if (maxVotes == 0) return -1;
            else
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    if (maxVotes == results[i]) return i;
                }
            }
            return -1;//uncertain
        }

        public Hunter StripChromasomes()
        {

                while (myChromasomes.Count > 0)
                    myChromasomes.RemoveAt(0);
             return this;
        }

        public string Serialize()
        {
            string ret = "Chromasomes:";
            foreach (Chromosome x in myChromasomes)
            {
                ret += x.Serialize();
                
            }
            ret += String.Format("endHunter|{0}", Environment.NewLine);
            return ret;
        }
        private static int GetUnpickedInt(Hunter a, List<int> aCrossed)
        {
            int i = OptoGlobals.RNG.Next(0, a.myChromasomes.Count);
            while (aCrossed.Contains(i)) i = OptoGlobals.RNG.Next(0, a.myChromasomes.Count);
            return i;
        }

        public static Hunter[] Crossover(Hunter a, Hunter b)
        {
            Hunter[] ret = new Hunter[2];

            ret[0] = new Hunter().StripChromasomes();
            ret[1] = new Hunter().StripChromasomes();

            Hunter target = ret[0], notTarget = ret[1], mostChromosomes;
            Chromosome[] temp = new Chromosome[2];//If a has 7 chromasomes, and b has 3,
            //we want to end as soon as either fails
            //So if a and b both have count 3, a is true, but b is false, so the comparison fails
            List<int> aCrossed = new List<int>(a.myChromasomes.Count), bCrossed = new List<int>(b.myChromasomes.Count);
            List<int> mostCrossed;
            int max = Math.Min(a.myChromasomes.Count, b.myChromasomes.Count), point1 = OptoGlobals.RNG.Next(0, max), point2 = OptoGlobals.RNG.Next(point1, max);
            #region CrossoverModeBlock
            switch (OptoGlobals.CrossoverMode)
            {
                case OptoGlobals.CrossoverModes.Uniform:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromasome(a[i]);
                        notTarget.AddChromasome(b[i]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.TwoPointHunter:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        //temp = Chromasome.CrossOver(a[i], b[i]);
                        target.AddChromasome(a[i]);
                        notTarget.AddChromasome(b[i]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (i == point1 || i == point2) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.SinglePointHunter:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        //temp = Chromasome.CrossOver(a[i], b[i]);
                        target.AddChromasome(a[i]);
                        notTarget.AddChromasome(b[i]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (i == point1) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.TwoPointChromasome:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromasome(temp[0]);
                        notTarget.AddChromasome(temp[1]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (i == point1 || i == point2) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.SinglePointChromasome:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromasome(temp[0]);
                        notTarget.AddChromasome(temp[1]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (i == point1) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.RandomHunter:

                    while (aCrossed.Count != a.myChromasomes.Count && bCrossed.Count != b.myChromasomes.Count)
                    {
                        int i = GetUnpickedInt(a, aCrossed);
                        int j = GetUnpickedInt(b, bCrossed);
                        if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                        //temp = Chromasome.CrossOver(a[i], b[j]);
                        target.AddChromasome(a[i]);
                        notTarget.AddChromasome(b[j]);
                        aCrossed.Add(i);
                        bCrossed.Add(j);
                    }
                    break;
                case OptoGlobals.CrossoverModes.RandomChromasome:
                    while (aCrossed.Count != a.myChromasomes.Count && bCrossed.Count != b.myChromasomes.Count)
                    {
                        int i = GetUnpickedInt(a, aCrossed);
                        int j = GetUnpickedInt(b, bCrossed);
                        if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                        temp = Chromosome.CrossOver(a[i], b[j]);
                        target.AddChromasome(temp[0]);
                        notTarget.AddChromasome(temp[1]);
                        aCrossed.Add(i);
                        bCrossed.Add(j);
                    }
                    break;
            }
            #endregion
            if (aCrossed.Count == a.myChromasomes.Count)
            {
                mostChromosomes = b;
                mostCrossed = bCrossed;
            }
            else
            {
                mostChromosomes = a;
                mostCrossed = aCrossed;
            }
            //Randomly distribute the remaining chromasomes
            while (mostCrossed.Count != mostChromosomes.myChromasomes.Count)
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                int i = GetUnpickedInt(mostChromosomes, mostCrossed);
                target.AddChromasome(mostChromosomes[i]);
                mostCrossed.Add(i);
            }
            ret[0].updateCellNum();
            ret[1].updateCellNum();
            return ret;
 /*           Hunter[] ret = new Hunter[2];
            ret[0] = new Hunter().StripChromasomes();
            ret[1] = new Hunter().StripChromasomes();
            Hunter target = ret[0], notTarget = ret[1], mostChromosomes;
            Chromasome[] temp = new Chromasome[2];
            int min = Math.Min(a.myChromasomes.Count, b.myChromasomes.Count);
            for (int i = 0; i < min; ++i)
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) Hunter.switchTargets(ret[0], ret[1], ref target, ref notTarget);
                temp = Chromasome.CrossOver(a[i], b[i]);
                target[i] = temp[0];
                notTarget[i] = temp[1];
            }

            int max = Math.Max(a.myChromasomes.Count, b.myChromasomes.Count);
            if (max == a.myChromasomes.Count) mostChromosomes = a;
            else mostChromosomes = b;

            for (int i = min; i < max; i++)
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                if (mostChromosomes[i] != null)
                    target[i] = mostChromosomes[i];
            }
            ret[0].updateCellNum();
            ret[1].updateCellNum();
            return ret;
            */
        }

        Hunter updateCellNum()
        {
            foreach (Chromosome x in myChromasomes)
            {
                x.updateCellNum();
            }
            return this;
        }
        public Chromosome this[int index]
        {
            get
            {
                if (index < 0 || index > myChromasomes.Count) throw new IndexOutOfRangeException();
              else return myChromasomes[index];  
            }
            set
            {
                if (index < myChromasomes.Count && index >=0)
                    myChromasomes.Insert(index, value);
                else
                    myChromasomes.Add(value);

            }
        }
        private static void switchTargets(Hunter a, Hunter b, ref Hunter target, ref Hunter notTarget)
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
        public string DisplayText
        {
            get
            {
                string ret = "";
                int count = 1;
                foreach (Chromosome x in myChromasomes)
                {
                    ret += "Chromosome " + count++ + ":";
                    ret += x.DisplayText;
                    ret += '\n';
                }
                return ret;
            }
        }

        public void Mutate()
        {
            foreach (Chromosome x in myChromasomes)
            {
                x.Mutate();
            }
        }


        public int CompareTo(Hunter other)
        {
            if (this.Fitness == other.Fitness)
            {//Tiebreaker
                return -1 * (this.Complexity.CompareTo(other.Complexity));
            }
            else return (this.Fitness.CompareTo(other.Fitness));

        }

        double GetFitness()
        {
            return Fitness;
        }

        internal void resetAffinity()
        {
            foreach (Chromosome x in myChromasomes)
            {
                x.resetAffinityBits();
            }
        }

        public String HumanReadableHunter
        {
            get
            {
                StringBuilder ret = new StringBuilder();
                ret.AppendLine("This hunter has the following " + myChromasomes.Count + " chromosome" + (myChromasomes.Count == 1 ? ":" : "s:"));
                foreach (Chromosome x in myChromasomes)
                {
                    ret.AppendLine(x.HumanReadableChromosome());
                }
                return ret.ToString();
            }
        }


    }

}