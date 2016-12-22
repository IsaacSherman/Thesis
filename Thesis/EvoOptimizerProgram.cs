using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
namespace EvoOptimization
{
    class EvoOptimizerProgram<T> where T: Optimizer, new()
    {
        private int _maxGen,_popSize = 50, _saveAfterGens=10;
        private bool _validate = false, _noload = false, _multiThread = false, _running = false, _outputBaseline = true;
        
        private List<T> best;

        OptimoEvolver<T> D;
     
        private OptimoEvolver<T>.CrossoverType _crossOverType = OptimoEvolver<T>.CrossoverType.Uniform;
        private string _bestFilePath;

        public int SaveAfterGens
        {
            get
            {
                return _saveAfterGens;
            }

            set
            {
                _saveAfterGens = value;
            }
        }

        public int MaxGen
        {
            get
            {
                return _maxGen;
            }

            set
            {
                _maxGen = value;
            }
        }

        public bool Validate
        {
            get
            {
                return _validate;
            }

            set
            {
                _validate = value;
            }
        }

        public bool Noload
        {
            get
            {
                return _noload;
            }

            set
            {
                _noload = value;
            }
        }

        public bool MultiThread
        {
            get
            {
                return _multiThread;
            }

            set
            {
                _multiThread = value;
            }
        }

        public bool Running
        {
            get
            {
                return _running;
            }
        }

        public int PopSize
        {
            get
            {
                return _popSize;
            }

            set
            {
                _popSize = value;
            }
        }

        public OptimoEvolver<T>.CrossoverType CrossOverType
        {
            get
            {
                return _crossOverType;
            }

            set
            {
                _crossOverType = value;
            }
        }

        public string BestFilePath
        {
            get
            {
                return _bestFilePath;
            }

            set
            {
                _bestFilePath = value;
            }
        }

        public bool OutputBaseline
        {
            get
            {
                return _outputBaseline;
            }

            set
            {
                _outputBaseline = value;
            }
        }

        public bool IncludeAllFeatures { get; private set; }

        public string OutputFileStem { get; private set; }


        /// <summary>
        /// Convenience function, Calls ConfigureEvolver() then Run();
        /// </summary>
        public void ConfigureAndRun()
        {
            ConfigureEvolver();
            Run();
        }

        public EvoOptimizerProgram()
        {
            T bob = new T();
            _bestFilePath = bob.GetToken + "best.csv";

        }

        /// <summary>
        /// Run this to set up the evolver- to apply changes made to PopSize or other behavior (MultiThread, Validate, NoLoad, etc).
        /// Once set up, run Run() to begin the program.
        /// </summary>
        public void ConfigureEvolver()
        {

            if (OutputFileStem == null)  OutputFileStem = new T().GetToken;
            D = new OptimoEvolver<T>(_popSize, _crossOverType, OutputFileStem ,!_noload);
            LoadBestFromFile(_bestFilePath);
            int q = 1;
            Stopwatch sw = new Stopwatch();
            foreach (T o in best)
            {
                D.AddToPopulation(o, q++);
            }
            D.MultiThread = _multiThread;
            if (IncludeAllFeatures) D.SetPopToAllCols();
            
        }

        private void LoadBestFromFile(String path)
        {
            best = new List<T>();
            using (StreamReader fin = new StreamReader(new FileStream(path, FileMode.OpenOrCreate)))
            {
                String line = fin.ReadLine();
                while (!fin.EndOfStream)
                {
                    if (line[0] == '0' || line[0] == '1')
                    {
                        T temp = new T();
                        temp.SetBitsToString(line);
                        temp.Prepare();
                        best.Add(temp);
                    }
                    else continue;
                }

            }
        }

        public void Run()
        {
            if (D == null)
                throw new InvalidOperationException("Run ConfigureEvolver() first");
            if (_validate) { D.VerifyOutput(); }


            if (_outputBaseline)
            {
                //Run a simulation including all features to find the best combination of parameters for the particular classifier
                T baseline = new T();
                string basePath = baseline.GetToken + "Baseline";
                EvoOptimizerProgram<T> baseProgram = new EvoOptimizerProgram<T>();
                baseProgram.MaxGen = 100;
                baseProgram.SaveAfterGens = 25;
                baseProgram.PopSize = 50;
                baseProgram.IncludeAllFeatures = true;
                baseProgram.OutputFileStem = basePath;
                baseProgram.Noload = true;
                baseProgram._outputBaseline = false;
                baseProgram.ConfigureAndRun();
               
            }

            Stopwatch sw = new Stopwatch();
            _running = true;
            for (int x = 0; x < _maxGen; ++x)
            {
                sw.Start();
                D.AdvanceGeneration();
                Debug.WriteLine("Elapsed time for generation " + x + " = " + sw.ElapsedMilliseconds + "  ms");
                Console.WriteLine("Elapsed time for generation " + x + " = " + sw.ElapsedMilliseconds + "  ms");

                Console.WriteLine("Average number of columns = " + D.GetAverageFeatureCount());
                double sum = 0, count = 0;
                foreach(T t in D.Population)
                {
                    sum += t.Fitness;
                    count += 1;
                }
                Console.WriteLine("Best Fitness = " + D.Population[0].Fitness + " \n Average fitness = " + D.AverageFitness);
                if (x % _saveAfterGens == 0) D.DumpLookupToFile(OutputFileStem + x + ".csv");
                sw.Reset();
            }
            D.DumpLookupToFile(OutputFileStem + "FinalTable.csv");
            Console.ReadLine();
        }
    }
}
