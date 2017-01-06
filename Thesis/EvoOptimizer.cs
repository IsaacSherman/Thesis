using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoOptimization
{
    public interface IHazFitness
    {
        double Fitness { get; }
    }

    public class SupportingFunctions
    {

        public static int GetUnpickedInt(int max, List<int> aCrossed)
        {
            int i = OptoGlobals.RNG.Next(0, max);
            while (aCrossed.Contains(i)) i = OptoGlobals.RNG.Next(0, max);
            return i;
        }

        public static List<S> StochasticUniformSample<S>(List<S> inPop) where S : IHazFitness
        {
            List<S> outPop = new List<S>();
            double popFitness = 0.0;
            int popSize = inPop.Count;
            for (int i = 0; i < popSize; i++) popFitness += inPop[i].Fitness;
            double distance = 1.0 / (popSize / 2);//The distance between each of the markers
            double start = OptoGlobals.RNG.NextDouble() % distance;//Ensure the start point is before the first marker
            double[] pointers = new double[popSize / 2];
            for (int i = 0; i < popSize / 2; ++i) pointers[i] = start + i * distance;//populate the pointers
            double cumulativeFitness = 0.0d;
            int l = 0;
            foreach (double p in pointers)
            {
                while (cumulativeFitness < p)
                {
                    //When fitness is fairly close, relatively speaking, this will roughly uniformly sample the population picking every odd or every even
                    //This is a feature, not a bug- when there's wide disparity, 
                    cumulativeFitness += inPop[l++].Fitness / popFitness;
                    if (l >= popSize)
                    {
                        l--;

                    }
                    //CumulativeFitness approaches 1, however due to rounding errors, it sometimes goes a little over
                    //ie., it approaches 1.0000000007, which causes the last index to get skipped which causes a slew of further problems.
                    //Therefore, if it does do something like that, just decrement l and continue.
                }

                outPop.Add(inPop[l]);

            }
            Debug.Assert(outPop.Count == popSize / 2);
            return outPop;
        }
    }
        class EvoOptimizer<T> where T: Optimizer, new()
    {

        public delegate T[] NBreedingFunction(params T[] breeders);
        public delegate T[] BreedingFunction(T a, T b);
        public static T[] UniformCrossover(T a, T b)
        {
            Debug.Assert(a.Length == b.Length);

            if (a == null || b == null) return null;
            T target = a, notTarget = b;
            T[] ret = new T[2]; 
            ret[0] = new T();
            ret[1] = new T();

            for (int i = 0; i < a.Length; ++i)//it makes sense at this level to use ordered iteration
            {
                if (OptoGlobals.RNG.NextDouble() <= OptoGlobals.CrossoverChance) switchTargets(a, b, ref target, ref notTarget);
                ret[0].Bits[i] = target.Bits[i];
                ret[1].Bits[i] = notTarget.Bits[i];
            }

            ret[0].Prepare();
            ret[1].Prepare();
            return ret;
        }

        private static void switchTargets(T a, T b, ref T target, ref T notTarget)
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



        internal static void Mutate(ref T critter, double p2)
        {
            for (int i = 0; i < critter.Bits.Length; ++i)
            {
                if (OptoGlobals.RNG.NextDouble() < p2) critter.Bits[i] = !critter.Bits[i];
            }
            critter.Prepare();
        }

        internal static void FillListFromBreedingPop(List<T> nextGen, List<T> BreedingPop, int targetSize, BreedingFunction func)
        {
            int elitismNum = (int)(OptoGlobals.ElitismPercent * targetSize);
            while (nextGen.Count < targetSize)
            {
                int j = OptoGlobals.RNG.Next(0, elitismNum), k = OptoGlobals.RNG.Next(0, BreedingPop.Count);
                while (k == j) k = OptoGlobals.RNG.Next(0, BreedingPop.Count);
                foreach (T newGuy in func(nextGen[j], BreedingPop[k]))
                {
                    nextGen.Add(newGuy);
                }


            }
            while (nextGen.Count > targetSize)
            {
                nextGen.RemoveAt(nextGen.Count-1);
            }
        }

        public static List<T> StochasticUniformSample(List<T> inPop)
        {
            return SupportingFunctions.StochasticUniformSample(inPop);
        }

       
    }
}
