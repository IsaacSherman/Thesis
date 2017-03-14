using System;
using EvoOptimization;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using MyUtils;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace MyCellNet
{
    internal class Daedalus
    {

        public int MaxGen { get; internal set; }
        public int PopSize { get; internal set; }
        public int RecordInterval { get; internal set; }
        public int InitialComplexityUpperBound { get; internal set; }
        public int MaxCellComplexity { get
            {
                return (int)OptoGlobals.ComplexityCap;
            }
            internal set
            {
                OptoGlobals.ComplexityCap = value;
            }
        }

        private string popfileName = "popStats.csv", hunterFileName = "bestHunter.csv";

        private List<Hunter> population = new List<Hunter>();

        private int generation;
        public Daedalus()
        {
            MaxGen = 100;
            PopSize = 50;
            RecordInterval = 10;
            InitialComplexityUpperBound = 1;
            MaxCellComplexity = 100;
            generation = 0;
        }
        private Dictionary<String, Double> categoryValues;
        internal void ConfigureCellDelegatesForDatabase()
        {
            Cell.SetNumberOfFeatures();
            Chromosome.SetNumberOfClasses();

            ///Cells expect double[] to their delegates, in this iteration (it's easily subclassed and changed)
            ///the next few lines populate static variables that hold the data in the format expected.
            ///Purely done for simplicity.
           
        }



        internal void Run()
        {
            population = new List<Hunter>(PopSize);
            
            bool successfulLoad = loadBestHunter();
            for (int i = (successfulLoad? 1:0); i < PopSize; ++i)
            {
                Hunter temp = new Hunter().StripChromosomes();
                int size = (int)Math.Ceiling(Math.Log(OptoGlobals.NumberOfClasses, 2));
                for (int j = 0; j < OptoGlobals.NumberOfClasses; ++j)
                {
                    BitArray classBits = Util.BitsFromInt(size, j);
                    temp.AddChromosome(new Chromosome(new System.Collections.BitArray(2, true), classBits));
                    int targetSize = OptoGlobals.RNG.Next(1, InitialComplexityUpperBound + 1);
                    while (temp.NumChromosomes < targetSize){
                        temp.AddChromosome(new Chromosome());
                    }
                }
                population.Add(temp);

            }

            for (generation = 0; generation < MaxGen; ++generation)
            {
                advanceGeneration();
                Console.WriteLine("Starting Generation " + generation);
                if (generation % RecordInterval == 0)
                {
                    dumpData();
                }


            }
            dumpData();
        }

        private void advanceGeneration()
        {


            evaluatePopulation();
            
            if (generation % RecordInterval == 0)
            {
                for (int i = 0; i < PopSize; ++i)
                {
                    population[i].EvaluateSet(OptoGlobals.DaedalusValidationSet, OptoGlobals.DaedalusValidationY, true);
                }
               
            }

            population.Sort();
            population.Reverse();
            gatherStats();
            generateNextGeneration();

        }

        List<Double> _sAverageComplexity, _sBestFitness, _sAverageFitness,  _sWorstFitness, _sMaxComplexity, _sComplexityOfBestHunter, _sMinComplexity,
            _sFitnessStdDev, _sComplexityStdDev;

        string _previousBestString = "";
        Double _previousBestFitness = 0;
        private void gatherStats()
        {
            if(_sAverageFitness == null)
            {
                _sComplexityOfBestHunter = new List<double>(MaxGen);
                _sBestFitness = new List<double>(MaxGen);
                _sAverageFitness = new List<double>(MaxGen);
                _sWorstFitness = new List<double>(MaxGen);
                _sMaxComplexity = new List<double>(MaxGen);
                _sMinComplexity = new List<double>(MaxGen);
                _sAverageComplexity = new List<double>(MaxGen);
                _sFitnessStdDev = new List<double>(MaxGen);
                _sComplexityStdDev= new List<double>(MaxGen);

            }
            _previousBestFitness = (_sBestFitness.Count < 1 ? 0 : _sBestFitness[_sBestFitness.Count - 1]);
            _sBestFitness.Add(population[0].Fitness);
            _sComplexityOfBestHunter.Add(population[0].Complexity);
            List<double> workingFitness = new List<double>(PopSize), workingComplexity = new List<double>(PopSize);
            foreach(Hunter x in population)
            {
                workingComplexity.Add(x.Complexity);
                workingFitness.Add(x.Fitness);
            }
            if (_previousBestFitness > population[0].Fitness)
            {
                string _currentBestString = population[0].Serialize();
                //System.Diagnostics.Debugger.Break();
            }
            _previousBestString = population[0].Serialize();

            _sMinComplexity.Add(workingComplexity.Min());
            _sMaxComplexity.Add(workingComplexity.Max());
            _sWorstFitness.Add(workingFitness.Min());
            _sAverageComplexity.Add(workingComplexity.Average());
            _sAverageFitness.Add(workingFitness.Average());
            _sFitnessStdDev.Add(workingFitness.ToArray().StandardDeviationIgnoringNAN());
            _sComplexityStdDev.Add(workingComplexity.ToArray().StandardDeviationIgnoringNAN());

            workingComplexity = workingFitness = null;

        }




        /// <summary>
        /// So we need population stats to include Complexity, Fitness, Best, average, worst, by generation
        /// The confusion matrix and "DNA" of the best hunter.
        /// </summary>
        private void dumpData()
        {
            string directory = "./" + OptoGlobals.EnvironmentTag + OptoGlobals.DataSetName + "Daedalus/";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            using (StreamWriter fout = new StreamWriter(new FileStream(directory + popfileName, FileMode.Create)))
            {
                StringBuilder x = new StringBuilder("Generation:,");
                for (int i = 0; i < generation; ++i) x.Append(i + ",");
                x.Remove(x.Length - 1, 1);//trim the last comma
                fout.WriteLine(x.ToString());

                fout.WriteLine(makeLineFromData(_sBestFitness, "Best Fitness:,").ToString());
                fout.WriteLine(makeLineFromData(_sComplexityOfBestHunter, "Complexity Of Best Hunter:,").ToString());
                fout.WriteLine(makeLineFromData(_sAverageFitness, "Average Fitness:,").ToString());
                fout.WriteLine(makeLineFromData(_sWorstFitness, "Worst Fitness,").ToString());
                fout.WriteLine(makeLineFromData(_sMaxComplexity, "Highest Complexity:,").ToString());
                fout.WriteLine(makeLineFromData(_sMinComplexity, "Lowest Complexity:,").ToString());
                fout.WriteLine(makeLineFromData(_sAverageComplexity, "Average Complexity:,").ToString());
                fout.WriteLine(makeLineFromData(_sFitnessStdDev, "Fitness Standard Deviations:,").ToString());
                fout.WriteLine(makeLineFromData(_sComplexityStdDev, "Complexity Standard Deviations:,").ToString());

                
            }
            using (StreamWriter fout = new StreamWriter(new FileStream(directory + hunterFileName, FileMode.Create)))
            {
                Hunter best = population[0];
                fout.WriteLine(best.Serialize());
                fout.WriteLine();
                fout.WriteLine("Best Fitness = " + best.Fitness);
                fout.WriteLine(best.HumanReadableHunter);//Dump the human readable portion
                int[,] cm = best.ConfusionMatrix;
                StringBuilder x = new StringBuilder();
                x.Append("Predicted/Actual,");
                for (int i = 0; i < OptoGlobals.NumberOfClasses; ++i)
                {
                    x.Append(OptoGlobals.ClassList[i] + ",");
                }
                x.DeleteLastChar();
                fout.WriteLine(x.ToString());
                x = new StringBuilder();
                if (cm == null)
                {
                    best.EvaluateSet(OptoGlobals.DaedalusTrainingSet, OptoGlobals.DaedalusTrainingY, false);
                    cm = best.ConfusionMatrix;
                }
                fout.WriteLine("Training Confusion Matrix");
                for (int i = 0; i < OptoGlobals.NumberOfClasses; ++i)
                {
                    x.Append(OptoGlobals.ClassList[i] + ",");
                    for (int j = 0; j < OptoGlobals.NumberOfClasses; ++j)
                    {
                        x.Append(cm[i, j]);
                        x.Append(",");

                    }
                    x.DeleteLastChar();
                    fout.WriteLine(x.ToString());
                    x = new StringBuilder();
                }
                cm = best.ValidationMatrix;
                if (cm == null)
                {
                    best.EvaluateSet(OptoGlobals.DaedalusValidationSet, OptoGlobals.DaedalusValidationY, true);
                    cm = best.ValidationMatrix;
                }
                fout.WriteLine("Validation Confusion Matrix");
                x.Append("Predicted/Actual,");
                for (int i = 0; i < OptoGlobals.NumberOfClasses; ++i)
                {
                    x.Append(OptoGlobals.ClassList[i] + ",");
                }
                for (int i = 0; i < OptoGlobals.NumberOfClasses; ++i)
                {
                    x.Append(OptoGlobals.ClassList[i] + ",");
                    for (int j = 0; j < OptoGlobals.NumberOfClasses; ++j)
                    {
                        x.Append(cm[i, j]);
                        x.Append(",");

                    }
                    x.DeleteLastChar();
                    fout.WriteLine(x.ToString());
                    x = new StringBuilder();
                }


            }

        }



        private bool loadBestHunter()
        {
            string directory = "./" + OptoGlobals.EnvironmentTag+ OptoGlobals.DataSetName + "Daedalus/";
            if (!Directory.Exists(directory)) return false;
            using (StreamReader fin = new StreamReader(directory + "bestHunter.csv"))
            {
                StringBuilder serial = new StringBuilder();
                string nextLine = fin.ReadLine();
                while (nextLine != "")
                {
                    serial.AppendLine(nextLine);
                    nextLine = fin.ReadLine();
                }
                population.Add(new Hunter(serial.ToString()));
            }
            return true;
        }



        private StringBuilder makeLineFromData<T>(List<T> set, string v)
        {
            StringBuilder ret = new StringBuilder(v);
            foreach (T x in set)
            {
                ret.Append(x);
                ret.Append(",");
            }
            ret.DeleteLastChar();//trim the last comma

            return ret;
        }




        private void evaluatePopulation()
        {
            List<Thread> threadPool = new List<Thread>(PopSize);

            foreach (Hunter x in population)
            {
                x.ErrorCheck();
                threadPool.Add(new Thread(() => x.EvaluateSet(OptoGlobals.DaedalusTrainingSet, OptoGlobals.DaedalusTrainingY)));
            }
            foreach (Thread t in threadPool)
            {
                t.Start();
            }
            while (threadPool.Any(t => t.IsAlive)) Thread.Sleep(500);


            foreach (Hunter x in population)
            {
                Hunter.AdjustFitnessForComplexity(x);
            }


        }


        private void generateNextGeneration()
        {
            Console.WriteLine("Before generation: best fitness is " + population[0].Fitness);
            List<Hunter> nextGen = new List<Hunter>(PopSize), breedingPop = new List<Hunter>(PopSize / 2);
            breedingPop = SupportingFunctions.StochasticUniformSample(population);

            for (int i = 0; i < breedingPop.Count; ++i)
            {
                if (breedingPop[i].Fitness == 0)
                {
                    breedingPop[i] = new Hunter(breedingPop[i].NumChromosomes, 1);
                }
            }

                Console.WriteLine("Filling breeding pool");
            fillListFromBreedingPop(nextGen, breedingPop);
            mutatePopulation(nextGen, (int)Math.Ceiling(OptoGlobals.ElitismPercent * PopSize));
            population = nextGen;
        }

        private void mutatePopulation(List<Hunter> nextGen, int startingAt)
        {
            for (int i = startingAt; i < nextGen.Count; ++i) nextGen[i].Mutate();
        }




        /// <summary>
        /// First, we'll copy the elites over.  Then we'll perform merger (every hunter in the breeding population has a chance of performing
        /// merger.  These are added to a list, and then executed without replacement.
        /// </summary>
        /// <param name="nextGen">Filling this</param>
        /// <param name="BreedingPop">by using this</param>
        private void fillListFromBreedingPop(List<Hunter> nextGen, List<Hunter> BreedingPop)
        {
            int elitismNum = (int)Math.Ceiling(OptoGlobals.ElitismPercent * PopSize );
            Console.WriteLine("Elitism...");
            for(int e = 0; e < elitismNum; ++e)
            {
                Console.WriteLine("Elitism: Adding hunter with fitness " + population[e].Fitness);
                if (population[e].Fitness == 0) 
                    nextGen.Add(new Hunter(population[e].NumChromosomes, 2)); 
                nextGen.Add (population[e].EliteCopy());
            }
            Console.WriteLine("Merge...");

            List<int> mergeList = new List<int>(15), used;
            int i=-1;
            Console.WriteLine("Before merge: best fitness is " + nextGen[0].Fitness);

            while (++i < BreedingPop.Count)
                if (OptoGlobals.RNG.NextDouble() < OptoGlobals.MergerPercent) mergeList.Add(i);

            used = new List<int>(mergeList);
            foreach(int target in mergeList)
            {
                
                used.Add(target);
                int k = SupportingFunctions.GetUnpickedInt(BreedingPop.Count, used);
                used.Add(k);
                //Console.WriteLine("Merging Hunters " + k + " and " + target);
                nextGen.Add(Hunter.Merger(BreedingPop[target], BreedingPop[k]));
            }
            //Now, we will use Crossover for the remaining slots
            while (nextGen.Count < PopSize)
            {

                int j = OptoGlobals.RNG.Next(0, elitismNum), k = OptoGlobals.RNG.Next(0, BreedingPop.Count);
                while (k == j) k = OptoGlobals.RNG.Next(0, BreedingPop.Count);
                foreach (Hunter newGuy in Hunter.Crossover(nextGen[j], BreedingPop[k]))
                {
                    nextGen.Add(newGuy.EliteCopy());
                }


            }
            //Since Crossover returns 2 at a time, we will strip off any extras
            while (nextGen.Count > PopSize)
            {
                nextGen.RemoveAt(nextGen.Count - 1);
            }
        }




        internal void InsertHunter(Hunter test)
        {
            population[PopSize - 3] = test;
        }
    }

    
}