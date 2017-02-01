using System;
using EvoOptimization;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using MyUtils;
using System.IO;
using System.Text;

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

        internal void ConfigureCellDelegatesForDatabase()
        {
            Chromosome.SetNumberOfClasses();
            Cell.SetNumberOfFeatures();

            ///Cells expect double[] to their delegates, in this iteration (it's easily subclassed and changed)
            ///the next few lines populate static variables that hold the data in the format expected.
            ///Purely done for simplicity.
            trainingSet = setFromCollection(OptoGlobals.TrainingXNormed);
            validationSet = setFromCollection(OptoGlobals.TestingXNormed);
            trainingY = new List<int>(Util.Flatten2dArray(OptoGlobals.trainingYIntArray));
            validationY = new List<int>(Util.Flatten2dArray(OptoGlobals.testingYIntArray));
           
        }

        private List<double[]> setFromCollection(List<List<double>> set)
        {
            List<Double[]> ret = new List<double[]>(set.Count);
            foreach (List<Double> x in set) ret.Add(x.ToArray());
            return ret;

        }

        internal void Run()
        {
            population = new List<Hunter>(PopSize);
            for (int i = 0; i < PopSize; ++i)
            {
                population.Add(new Hunter(OptoGlobals.RNG.Next(1, InitialComplexityUpperBound + 1)));
            }
            for (generation = 0; generation < MaxGen; ++generation)
            {
                advanceGeneration();
                if (generation % RecordInterval == 0) dumpData();

            }
            dumpData();
        }

        Hunter lastBest;

        private void advanceGeneration()
        {
            
            evaluatePopulation();
            adjustFitnessForComplexity();
            population.Sort();
            population.Reverse();
            gatherStats();
            if (generation > 1 && population[0].Fitness < _sBestFitness[generation - 1])
            {
                Console.WriteLine("We lost fitness, shouldn't be possible");
            }
            generateNextGeneration();
            lastBest = population[0].EliteCopy();
        }

        List<Double> _sAverageComplexity, _sBestFitness, _sAverageFitness,  _sWorstFitness, _sMaxComplexity, _sComplexityOfBestHunter, _sMinComplexity,
            _sFitnessStdDev, _sComplexityStdDev;

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

            _sBestFitness.Add(population[0].Fitness);
            _sComplexityOfBestHunter.Add(population[0].Complexity);
            List<double> workingFitness = new List<double>(PopSize), workingComplexity = new List<double>(PopSize);
            foreach(Hunter x in population)
            {
                workingComplexity.Add(x.Complexity);
                workingFitness.Add(x.Fitness);
            }

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
            string directory = "./" + OptoGlobals.DataSetName + "Daedalus/";
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
                //fout.WriteLine(best.Serialize());//Dump the hunter data (should implement better way)
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

        private void adjustFitnessForComplexity()
        {
            foreach(Hunter x in population)
            {
                x.Fitness *= (OptoGlobals.ComplexityCap - x.Complexity )/ OptoGlobals.ComplexityCap;
                if (x.Fitness < 0) x.Fitness = 0;
            }
        }

        static internal List<Double[]> trainingSet, validationSet;
        static internal List<int> trainingY, validationY;


        private void evaluatePopulation()
        {
            List<Thread> threadPool = new List<Thread>(PopSize);

            foreach (Hunter x in population)
            {
                threadPool.Add(new Thread(() => x.EvaluateSet(trainingSet, trainingY)));
            }
            foreach (Thread t in threadPool)
            {
                t.Start();
            }
            
            while (threadPool.Any(t => t.IsAlive)) Thread.Sleep(500);

        }

        private void generateNextGeneration()
        {

            List<Hunter> nextGen = new List<Hunter>(PopSize), breedingPop = new List<Hunter>(PopSize / 2);
            breedingPop = SupportingFunctions.StochasticUniformSample(population);
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
            for(int e = 0; e < elitismNum; ++e)
            {
                nextGen.Add (population[e]);
            }
            List<int> mergeList = new List<int>(15), used;
            int i=-1;
            while (++i < BreedingPop.Count)
                if (OptoGlobals.RNG.NextDouble() < OptoGlobals.MergerPercent) mergeList.Add(i);

            used = new List<int>(mergeList);
            foreach(int target in mergeList)
            {
                
                used.Add(target);
                int k = SupportingFunctions.GetUnpickedInt(BreedingPop.Count, used);
                used.Add(k);

                nextGen.Add(Hunter.Merger(BreedingPop[target], BreedingPop[k]));
            }
            //Now, we will use Crossover for the remaining slots
            while (nextGen.Count < PopSize)
            {
                int j = OptoGlobals.RNG.Next(0, elitismNum), k = OptoGlobals.RNG.Next(0, BreedingPop.Count);
                while (k == j) k = OptoGlobals.RNG.Next(0, BreedingPop.Count);
                foreach (Hunter newGuy in Hunter.Crossover(nextGen[j], BreedingPop[k]))
                {
                    nextGen.Add(newGuy);
                }


            }
            //Since Crossover returns 2 at a time, we will strip off any extras
            while (nextGen.Count > PopSize)
            {
                nextGen.RemoveAt(nextGen.Count - 1);
            }
        }



        }

    
}