﻿using System;
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
    /// No new data is added, but it does implement some methods.  
    /// </summary>
    public class Hunter : IComparable<Hunter>, IHazFitness
    {
        List<Chromosome> myChromosomes;
        public int NumChromosomes { get { return myChromosomes.Count; } }
        public string tag="";
        
        private double accuracy = 0;
        public int[,] ConfusionMatrix { get; private set; }
        public double Complexity
        {
            get
            {
                double total = 0;
                foreach (Chromosome x in myChromosomes)
                {
                    total += x.NumCells;
                }
                return total;
            }
        }
        public Hunter()
        {
            initChromosomes();
        }
        public Hunter(int chromanum, int cells = 1)
        {
            myChromosomes = new List<Chromosome>(chromanum);
            for (int i = 0; i < chromanum; ++i)
            {
                myChromosomes.Add(new Chromosome());
                for (int j = 1; j < cells; ++j)
                {
                    myChromosomes[i].JoinCell(new Cell());
                }
                updateCellNum();
            }
        }

        internal void ErrorCheck()
        {
            foreach (Chromosome c in myChromosomes) c.ErrorCheck();
        }

        public void overWriteClassBits(ref int num)
        {
            Debug.Assert(myChromosomes.Count != 0);
            for (int i = 0; i < myChromosomes.Count; ++i)
            {
                BitArray temp = Util.BitsFromInt(Chromosome.ClassBitLength, num++%16);
                myChromosomes[i].ClassBits = temp;

            }
        }

        protected Hunter(Chromosome x)
        {
            initChromosomes();
            myChromosomes.RemoveAt(0);
            this.AddChromosome(x);
        }



        public Hunter EliteCopy()
        {
            Hunter ret = new Hunter().StripChromosomes();//initializes everything, empties chromosome
            ret.Fitness = Fitness;
            ret.ConfusionMatrix = ConfusionMatrix;
            foreach (Chromosome x in myChromosomes)
            {
                ret.myChromosomes.Add(x.deepCopy());
            }
            return ret;

        }

        /// <summary>
        /// The most important thing this function does is to adjust the Hunter's confusion matrix.  
        /// </summary>
        /// <param name="setx">the input vectors</param>
        /// <param name="setY">the class identity</param>
        internal void EvaluateSet(List<double[]> setx, List<int> setY)
        {
            ConfusionMatrix = new int[OptoGlobals.NumberOfClasses, OptoGlobals.NumberOfClasses];
            int n = 0;
            int[] countPerClass = new int[OptoGlobals.NumberOfClasses], predictionPerClass = new int[OptoGlobals.NumberOfClasses];
            ErrorCheck();
            for (int i = 0; i < setx.Count; ++i)
            {
                double[] data = setx[i];

                int x = Vote(data);
                if (x >= 0)//a -1 means it didn't vote
                {
                    int y = setY[i];
                    ConfusionMatrix[x, y] += 1;
                    countPerClass[y] += 1;
                    predictionPerClass[x] += 1;
                }
                ++n;
                
            }

            double totalRight = 0;
            double[] rightPerClass = new double[OptoGlobals.NumberOfClasses];
            for (int i = 0; i < OptoGlobals.NumberOfClasses; ++i)
            {
                totalRight += ConfusionMatrix[i, i];
                rightPerClass[i] = ConfusionMatrix[i, i] / (1.0+countPerClass[i]);

            }
            Fitness = rightPerClass.Average();
        }

        public void AddChromosome(Chromosome x)
        {
            
            myChromosomes.Add(x.deepCopy());

        }
        private void initChromosomes()
        {
            myChromosomes = new List<Chromosome>();
            Chromosome temp = new Chromosome();
            myChromosomes.Add(temp);
            temp.updateCellNum();

        }
        /// <summary>
        /// Takes two Hunters and merges them. Note that this effectively doubles to complexity of offspring, so it is necessary 
        /// to make this event relatively rare and to penalize unneccessary complexity
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Hunter Merger(Hunter first, Hunter second)
        {

            int firstNum = first.NumChromosomes, secondNum = second.NumChromosomes;
            int min = Math.Min(firstNum, secondNum);
            Hunter ret;
            Chromosome[] temp;
            //Step through, merging each pair, until we run out of matched pairs
            ret = new Hunter().StripChromosomes();
            for (int i = 0; i < min; ++i)
            {
            temp = Chromosome.Merge(first.myChromosomes[i], second.myChromosomes[i]);

                foreach (Chromosome x in temp)
                {
                    ret.AddChromosome(x);
                }

            }
            //At this point, either first or second still has chromosomes left over, we'll just copy them in
            Hunter target = (min == firstNum ? second : first);
            int max = Math.Max(firstNum, secondNum);
            for (int i = min; i < max; ++i) ret.AddChromosome(target.myChromosomes[i]);
            ret.tag = first.tag + "-+-" + second.tag;


            return ret;
        }
        /// <summary>
        /// Vote needs a parameter, it's what the cell actually votes on, but that can look like anything for polymorphism or something.
        /// It's assumed to be a double[] by the code currently, but that's a fairly trivial change.  It also expects a double as a return value, which 
        /// will be stored in the out variable ret. 
        /// </summary>
        /// <returns>Whether the Hunter attacks or not.</returns>
        public int Vote(object data, out double ret)
        {
            ret = 0;
            //So we need an array of ints which hold the counts for each class
            int[] results = new int[OptoGlobals.NumberOfClasses];
            foreach (Chromosome x in myChromosomes)
            {
                int temp = x.Vote(data, out ret);

                if (temp >= OptoGlobals.NumberOfClasses)
                    throw new AccessViolationException("wtf");
                if (temp >= 0) results[temp] += x.NumCells;

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

        public int Vote(double[] data)
        {
            double outVal;
            int ret = Vote(data, out outVal);///For easier debugging, outval probably has useful data in it
            return ret;
        }

        public Hunter StripChromosomes()
        {

            myChromosomes = new List<Chromosome>();
            return this;
        }

        public string Serialize()
        {
            string ret = "Chromosomes:";
            foreach (Chromosome x in myChromosomes)
            {
                ret += x.Serialize();
                
            }
            ret += String.Format("endHunter|{0}", Environment.NewLine);
            return ret;
        }


        public static Hunter[] Crossover(Hunter a, Hunter b)
        {
            Hunter[] ret = new Hunter[2];

            ret[0] = new Hunter().StripChromosomes();
            ret[1] = new Hunter().StripChromosomes();

            Hunter target = ret[0], notTarget = ret[1], mostChromosomes;
            Chromosome[] temp = new Chromosome[2];//If a has 7 Chromosomes, and b has 3,
            //we want to end as soon as either fails
            //So if a and b both have count 3, a is true, but b is false, so the comparison fails
            List<int> aCrossed = new List<int>(a.myChromosomes.Count), bCrossed = new List<int>(b.myChromosomes.Count);
            List<int> mostCrossed;
            int max = Math.Min(a.myChromosomes.Count, b.myChromosomes.Count), point1 = OptoGlobals.RNG.Next(0, max), point2 = OptoGlobals.RNG.Next(point1, max);
            #region CrossoverModeBlock
            switch (OptoGlobals.CrossoverMode)
            {
                case OptoGlobals.CrossoverModes.Uniform:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromosome(a[i]);
                        notTarget.AddChromosome(b[i]);
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
                        //temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromosome(a[i]);
                        notTarget.AddChromosome(b[i]);
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
                        //temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromosome(a[i]);
                        notTarget.AddChromosome(b[i]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (i == point1) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.TwoPointChromosome:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromosome(temp[0]);
                        notTarget.AddChromosome(temp[1]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (i == point1 || i == point2) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.SinglePointChromosome:
                    for (int i = 0; i < max; ++i)
                    {
                        //Trying a form of uniform crossover, 2 point
                        //just swapping chromosomes, not crossing over at that level
                        temp = Chromosome.CrossOver(a[i], b[i]);
                        target.AddChromosome(temp[0]);
                        notTarget.AddChromosome(temp[1]);
                        aCrossed.Add(i);
                        bCrossed.Add(i);
                        if (i == point1) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                    }
                    break;
                case OptoGlobals.CrossoverModes.RandomHunter:

                    while (aCrossed.Count != a.myChromosomes.Count && bCrossed.Count != b.myChromosomes.Count)
                    {
                        int i = SupportingFunctions.GetUnpickedInt(a.myChromosomes.Count, aCrossed);
                        int j = SupportingFunctions.GetUnpickedInt(b.myChromosomes.Count, bCrossed);
                        if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                        //temp = Chromosome.CrossOver(a[i], b[j]);
                        target.AddChromosome(a[i]);
                        notTarget.AddChromosome(b[j]);
                        aCrossed.Add(i);
                        bCrossed.Add(j);
                    }
                    break;
                case OptoGlobals.CrossoverModes.RandomChromosome:
                    while (aCrossed.Count != a.myChromosomes.Count && bCrossed.Count != b.myChromosomes.Count)
                    {
                        int i = SupportingFunctions.GetUnpickedInt(a.myChromosomes.Count, aCrossed);
                        int j = SupportingFunctions.GetUnpickedInt(b.myChromosomes.Count, bCrossed);
                        if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                        temp = Chromosome.CrossOver(a[i], b[j]);
                        target.AddChromosome(temp[0]);
                        notTarget.AddChromosome(temp[1]);
                        aCrossed.Add(i);
                        bCrossed.Add(j);
                    }
                    break;
            }
            #endregion
            if (aCrossed.Count == a.myChromosomes.Count)
            {
                mostChromosomes = b;
                mostCrossed = bCrossed;
            }
            else
            {
                mostChromosomes = a;
                mostCrossed = aCrossed;
            }
            //Randomly distribute the remaining Chromosomes
            while (mostCrossed.Count != mostChromosomes.myChromosomes.Count)
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(ret[0], ret[1], ref target, ref notTarget);
                int i = SupportingFunctions.GetUnpickedInt(mostChromosomes.myChromosomes.Count, mostCrossed);
                target.AddChromosome(mostChromosomes[i]);
                mostCrossed.Add(i);
            }
            ret[0].updateCellNum();
            ret[1].updateCellNum();
            return ret;
 /*           Hunter[] ret = new Hunter[2];
            ret[0] = new Hunter().StripChromosomes();
            ret[1] = new Hunter().StripChromosomes();
            Hunter target = ret[0], notTarget = ret[1], mostChromosomes;
            Chromosome[] temp = new Chromosome[2];
            int min = Math.Min(a.myChromosomes.Count, b.myChromosomes.Count);
            for (int i = 0; i < min; ++i)
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) Hunter.switchTargets(ret[0], ret[1], ref target, ref notTarget);
                temp = Chromosome.CrossOver(a[i], b[i]);
                target[i] = temp[0];
                notTarget[i] = temp[1];
            }

            int max = Math.Max(a.myChromosomes.Count, b.myChromosomes.Count);
            if (max == a.myChromosomes.Count) mostChromosomes = a;
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
            foreach (Chromosome x in myChromosomes)
            {
                x.updateCellNum();
            }
            return this;
        }
        public Chromosome this[int index]
        {
            get
            {
                if (index < 0 || index > myChromosomes.Count) throw new IndexOutOfRangeException();
              else return myChromosomes[index];  
            }
            set
            {
                if (index < myChromosomes.Count && index >=0)
                    myChromosomes.Insert(index, value);
                else
                    myChromosomes.Add(value);

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
                foreach (Chromosome x in myChromosomes)
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
            foreach (Chromosome x in myChromosomes)
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
            foreach (Chromosome x in myChromosomes)
            {
                x.resetAffinityBits();
            }
        }

        public String HumanReadableHunter
        {
            get
            {
                StringBuilder ret = new StringBuilder();
                ret.AppendLine("This hunter has the following " + myChromosomes.Count + " chromosome" + (myChromosomes.Count == 1 ? ":" : "s:"));
                foreach (Chromosome x in myChromosomes)
                {
                    ret.AppendLine(x.HumanReadableChromosome());
                }
                return ret.ToString();
            }
        }
        double _fitness = 0;
        public double Fitness
        {
            get
            {
                return _fitness;
            }
            internal set
            {
                _fitness = value;
            }
        }
    }

}