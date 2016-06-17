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
            String GlobalPath = "/../Data/Yeast/DataSetConfig.csv";
            if (args.Length >= 2) { GlobalPath = args[1]; }

            OptoGlobals.ConfigureForDataset(GlobalPath);
            EvoOptimizerProgram<SVMOptimizer> porgam = new EvoOptimizerProgram<SVMOptimizer>();
            //Configure the program here- set things like multi-threading, etc, if desired
            porgam.ConfigureAndRun();
        }

    }
}
