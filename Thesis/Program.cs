using System;
using System.Diagnostics;
using System.IO;
using EvoOptimization.MulticlassNBOptimizer;


namespace EvoOptimization
{
    class Program
    {



        static void Main(string[] args)
        {

            // Create the MATLAB instance 
            String GlobalPath = "../../../Data/Yeast/DataSetConfig.csv";
            if (args.Length >= 2) { GlobalPath = args[1]; }
           
            OptoGlobals.ConfigureForDataset(GlobalPath);

            MulticlassNBOptimizer.MulticlassNBOptimizer.RewriteBits();
            EvoOptimizerProgram<MulticlassNBOptimizer.MulticlassNBOptimizer> porgam = new EvoOptimizerProgram<MulticlassNBOptimizer.MulticlassNBOptimizer>();
            porgam.MaxGen = 100;
            porgam.SaveAfterGens = 25;
            porgam.PopSize = 50;

            //Configure the program here- set things like multi-threading, etc, if desired
            porgam.ConfigureAndRun();
        }

    }
}
