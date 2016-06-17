using System;
using System.Collections.Generic;
using System.IO;
using EvoOptimization;
using System.Diagnostics;
namespace Thesis
{
    class EvoOptimizerProgram<T> where T: Optimizer, new()
    {
        private int _maxGen,_popSize = 50, _saveAfterGens;
        private bool _validate = false, _noload = false, _multiThread = false, _running = false;


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

        public EvoOptimizerProgram(string [] args,  int maxGen = 100, int save = 10)
        {
            _maxGen = maxGen;
            _saveAfterGens = save;
            T bob = new T();
            _bestFilePath = bob.GetToken + "best.csv";

        }

        public EvoOptimizerProgram() : this(null)
        {
        }

        /// <summary>
        /// Run this, 
        /// </summary>
        public void ConfigureEvolver()
        {
            D = new OptimoEvolver<T>(_popSize, _crossOverType, !_noload);
            List<T> best = new List<T>();
            LoadBestFromFile(_bestFilePath);
            int q = 1;
            Stopwatch sw = new Stopwatch();
            foreach (T o in best)
            {
                D.AddToPopulation(o, q++);
            }
            D.MultiThread = _multiThread;
            
            if (_validate) { D.VerifyOutput(); }

            
        }

        private void LoadBestFromFile(String path)
        {
            using (StreamReader fin = new StreamReader(path))
            {
                String line = fin.ReadLine();
                while (!fin.EndOfStream)
                {
                    if (line[0] == '0' || line[0] == '1') { 
                    T temp = new T();
                    T.SetBitsToString(line);
                        _bestFilePath.
                }

            }
        }

        public void Run()
        {
            if (D == null)
                throw new InvalidOperationException("Run ConfigureEvolver() first");
            
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
