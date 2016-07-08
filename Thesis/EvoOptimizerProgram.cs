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
            D = new OptimoEvolver<T>(_popSize, _crossOverType, !_noload);
            LoadBestFromFile(_bestFilePath);
            int q = 1;
            Stopwatch sw = new Stopwatch();
            foreach (T o in best)
            {
                D.AddToPopulation(o, q++);
            }
            D.MultiThread = _multiThread;
            


            
        }

        private void LoadBestFromFile(String path)
        {
            best = new List<T>();
            using (StreamReader fin = new StreamReader(path))
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
                T baseline = new T();
                string basePath = baseline.GetToken + "BaseLine.csv";
                baseline.SetBitsToString(new string('0', baseline.Bits.Length));
                baseline.Prepare();
                baseline.Eval();
                using (StreamWriter baseFout = new StreamWriter(new FileStream(basePath, FileMode.Create)))
                {
                    baseline.DumpLabelsToStream(baseFout);
                }
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
                if (x % _saveAfterGens == 0) D.DumpLookupToFile(D.Population[0].GetToken + x + ".csv");
                sw.Reset();
            }
            D.DumpLookupToFile(D.Population[0].GetToken + "FinalTable.csv");
            Console.ReadLine();
        }
    }
}
