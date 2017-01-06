using System;

using MyCellNet;
using System.Collections.Generic;

namespace EvoOptimization
{
    class Program
    {



        static void Main(string[] args)
        {


            // Create the MATLAB instance 
            String GlobalPath = "../../../Data/Yeast/DataSetConfig.csv";
            int maxGen = 100, saveAfterGens = 25, popSize = 50, baseCompUB = 10, maxComp = 100;

            if (args.Length >= 2) { GlobalPath = args[1]; }
            for (int i = 2; i < args.Length; ++i)
            {
                switch (args[i].ToLower())
                {
                    case "path":
                    case "-p":
                        GlobalPath = args[++i];
                        break;
                    case "gen":
                    case "-g":
                        maxGen = Int32.Parse(args[++i]);
                        break;
                    case "save":
                    case "-r":
                        saveAfterGens = Int32.Parse(args[++i]);
                        break;
                    case "population":
                    case "-pop":
                        saveAfterGens = Int32.Parse(args[++i]);
                        break;
                    case "compub":
                    case "-c":
                        baseCompUB = Int32.Parse(args[++i]);
                        break;
                    case "maxcomp":
                    case "-m":
                        maxComp = Int32.Parse(args[++i]);
                        break;
                }
            }

            OptoGlobals.ConfigureForDataset(GlobalPath);
            double[] poot =  { OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(),
            OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble()};
            object pootwrap = poot;
            Hunter x = new Hunter();
            double nerp;
            x.Vote(pootwrap, out nerp);

            MulticlassNBOptimizer.MulticlassNBOptimizer.RewriteBits();
            EvoOptimizerProgram<MulticlassNBOptimizer.MulticlassNBOptimizer> porgam = new EvoOptimizerProgram<MulticlassNBOptimizer.MulticlassNBOptimizer>();
            porgam.MaxGen = maxGen;
            porgam.SaveAfterGens = saveAfterGens;
            porgam.PopSize = popSize;

            //Configure the program here- set things like multi-threading, etc, if desired
            porgam.ConfigureAndRun();
            Daedalus D = new Daedalus();
            D.MaxGen = maxGen;
            D.RecordInterval = saveAfterGens;
            D.PopSize = popSize;
            D.InitialComplexityUpperBound = baseCompUB;
            D.MaxCellComplexity = maxComp;
            D.ConfigureCellDelegatesForDatabase();
            D.Run();

        }

        private static double[] NormTest()
        {
            double[] poot = { OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(), OptoGlobals.RNG.NextDouble(),
            OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble(),OptoGlobals.RNG.NextDouble()};
            List<Double> test = new List<double>(poot);
            List<List<Double>> stat = new List<List<double>>();
            for (int i = 0; i < 10; ++i)
            {
                double[] t = { 0, 0, 0, 0 };
                List<Double> temp = OptoGlobals.GetStats(poot);
                stat.Add(temp);
            }
            List<Double> t3 = OptoGlobals.StdDevNorm(test, stat);
            List<List<double>> devStats = new List<List<double>>(10);
            List<Double> devTemp = OptoGlobals.GetStats(t3.ToArray());
            for (int i = 0; i < 10; ++i)
            {
                devStats.Add(devTemp);
            }
            List<Double> t2 = OptoGlobals.MinMaxNorm(test, stat);
            List<Double> t4 = OptoGlobals.MinMaxNorm(t3, devStats);
            return poot;
        }
    }
}
