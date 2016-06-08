using System;
using System.Diagnostics;
using System.IO;


namespace EvoOptimization.SVMOptimizationNET40
{
    class Program
    {
        


        static void Main(string[] args)
        {
            //One of : Both Horz(in and Out) or both (Vert in and out), + either axial
            //Change the features to copy missing data
            // Create the MATLAB instance 
            Console.WriteLine("Entering Program.cs");
            OptimoEvolver<SVMOptimizer> D = new OptimoEvolver<SVMOptimizer>();
            Stopwatch sw = new Stopwatch();
            SVMOptimizer best = new SVMOptimizer("00000000010010000010000011011000001000000010010010001000011000001110010000010000010000010000000010100000000001010100000010000000000001010001010000011000000000001001000001011000010010010001000000000000001001000000000001000000010000011000000000111");

 
            D.AddToPopulation(best, 4);
            D.MultiThread = false;
            for (int x = 0; x < 100; ++x)
            {
                sw.Start();
                D.AdvanceGeneration();
                Debug.WriteLine("Elapsed time for generation " + x + " = " + sw.ElapsedMilliseconds + "  ms");
                Console.WriteLine("Elapsed time for generation " + x + " = " + sw.ElapsedMilliseconds + "  ms");

                Console.WriteLine("Average number of columns = " + D.GetAverageFeatureCount());
                if (x % 10 == 0) D.DumpLookupToFile("SVMTable" + x + ".csv");
                sw.Reset();
            }
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            D.DumpLookupToFile("SVMFinalTable.csv");
            Console.ReadLine(); 

        }

        private static void ComparativeTest()
        {
            OptimoEvolver<SVMOptimizer> threaded, unthreaded;
            Stopwatch sw = new Stopwatch();
            OptoGlobals.RNG = new Random(Seed: 1);
            threaded = new OptimoEvolver<SVMOptimizer>(25,OptimoEvolver<SVMOptimizer>.CrossoverType.Uniform);
            threaded.MultiThread = true;
            OptoGlobals.RNG = new Random(Seed: 1);
            unthreaded = new OptimoEvolver<SVMOptimizer>(25, OptimoEvolver<SVMOptimizer>.CrossoverType.Uniform);
            unthreaded.MultiThread = false;
            sw.Start();
            threaded.AdvanceGeneration();
            sw.Stop();
            Debug.WriteLine("THreaded evaluation of a generation took " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
            unthreaded.AdvanceGeneration();
            sw.Stop();
            Debug.WriteLine("Unthreaded evaluation of a generation took " + sw.ElapsedMilliseconds + "ms");

        }


        private static double nonThreadTest()
        {
            SVMOptimizer test1 = new SVMOptimizer(), test2 = new SVMOptimizer();
            SVMOptimizer[] evo = (SVMOptimizer[])EvoOptimizer<SVMOptimizer>.UniformCrossover(test1, test2);

            double[,] cost = new double[2, 2] { { 0, 1 }, { 2, 1 } };

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            test1.Eval();
            test2.Eval();
            evo[0].Eval();
            evo[1].Eval();

            sw.Stop();
            Console.WriteLine("Time elapsed (single run)= " + sw.ElapsedMilliseconds + "ms");
            return sw.ElapsedMilliseconds;
            // Display result 
        }

        private static double threadTest()
        {
            SVMOptimizer test1 = new SVMOptimizer(), test2 = new SVMOptimizer();
            SVMOptimizer[] evo = (SVMOptimizer[])EvoOptimizer<SVMOptimizer>.UniformCrossover(test1, test2);

            double[,] cost = new double[2, 2] { { 0, 1 }, { 2, 1 } };

            System.Threading.Thread[] thredz = new System.Threading.Thread[4];

            thredz[0] = new System.Threading.Thread(test1.Eval);
            thredz[1] = new System.Threading.Thread(test2.Eval);
            thredz[2] = new System.Threading.Thread(evo[0].Eval);
            thredz[3] = new System.Threading.Thread(evo[1].Eval);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            for (int i = 0; i < 4; ++i)
            {
                thredz[i].Start();
            }
            while (thredz[0].IsAlive && thredz[1].IsAlive && thredz[3].IsAlive && thredz[2].IsAlive) continue;
            sw.Stop();
            Console.WriteLine("Time elapsed = " + sw.ElapsedMilliseconds + "ms");
            return sw.ElapsedMilliseconds;
            // Display result 
        }
    }
}
