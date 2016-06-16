using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoOptimization
{
    class EvoOptimizer<T> where T: Optimizer, new()
    {

        public delegate T[] NBreedingFunction(params T[] breeders);
        public delegate T[] BreedingFunction(T a, T b);
        public static T[] UniformCrossover(T a, T b)
        {
            System.Diagnostics.Debug.Assert(a.Length == b.Length);

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
    }
}
