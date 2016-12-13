using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using MyUtils;
namespace EvoOptimization
{
    public class OptimoEvolver<T> where T : Optimizer, IComparable<Optimizer>, new()
    {
        public enum CrossoverType { Uniform, OnePoint, TwoPoint };
        protected Dictionary<String, Tuple<Double, Double>> _lookup;
        protected bool FocusOnAllColumns = false;

        CrossoverType _cross;
        public bool ReadInCurrentGeneration = false;
        protected int _popSize, generation;
        public bool CountFeatures = true;
        protected List<T> _population;
        public List<T> Population { get { return _population; } }
        public Boolean MultiThread { get; set; }
        public double AverageFitness { get; private set; }
        public List<double> AverageFitnessTracker = new List<double>(), BestFitnessTracker = new List<double>();

        protected string _outputPath, _currentGenPath;
        public OptimoEvolver(int PopSize, CrossoverType xType, bool loadFromFile = false)
        {
            ReadInCurrentGeneration = loadFromFile;
            CountFeatures = OptoGlobals.IsDebugMode;
            Console.WriteLine("Count Features is " + (CountFeatures ? "On" : "Off"));
            generation = 0;
            _cross = xType;
            _popSize = PopSize;
            _population = new List<T>(_popSize);
            for (int i = 0; i < _popSize; ++i)
            {
                _population.Add(new T());
            }
            _outputPath = "OutputTable" + _population[0].GetToken + ".csv";
            _currentGenPath = "CurrentGenPop" + _population[0].GetToken + ".csv";
            _lookup = new Dictionary<string, Tuple<Double,Double>>();
            if (File.Exists(_outputPath)) readLookupFromFile();
            else File.Create(_outputPath);
            if (FocusOnAllColumns) SetPopToAllCols();     
            if (ReadInCurrentGeneration)
            {
                if (File.Exists(_currentGenPath)) loadPopulationFromFile();
                else dumpPopulationToFile();

            }

        }

        private void dumpPopulationToFile()
        {
            using (StreamWriter fout = new StreamWriter(File.Create(_currentGenPath)))
            {
                foreach (Optimizer O in Population)
                {
                    fout.WriteLine(O.ToString());
                }
            }
        }

        private void loadPopulationFromFile()
        {
            List<String> newPop = new List<string>();
            using (StreamReader fin = new StreamReader(File.OpenRead(_currentGenPath)))
            {
                while (!fin.EndOfStream)
                {
                    newPop.Add(fin.ReadLine());
                }
            }
            for (int i = 0; i < newPop.Count; ++i) //If the file doesn't exist or is short, Population remnant will still be valid
            {
                Population[i] = new T();//Make sure fitness and anything else don't get carried over
                Population[i].SetBitsToString(newPop[i]);
                Population[i].Prepare();
            }
                    
        }

        protected virtual void readLookupFromFile()
        {
            using (StreamReader fin = new StreamReader(File.OpenRead(_outputPath)))
            {
                //eat first two lines
                fin.ReadLine();
                fin.ReadLine();
                char[] param = { ',' };
                int TLength = _population[0].Bits.Length;
                while (!fin.EndOfStream)
                {
                    string key, line = fin.ReadLine();
                    string[] tokens = line.Split(param);
                    bool success = TLength == tokens[0].Length;
                    if (!success) 
                        continue;
                    key = tokens[0];
                    
                    Tuple<Double, double> toilAndTrouble = null;
                    try
                    {
                        toilAndTrouble = new Tuple<double, double>(Double.Parse(tokens[1]), double.Parse(tokens[2]));
                    }
                    catch (ArgumentNullException e)
                    {
                        success = false;
                        Debug.WriteLine("ArgumentNullException; parsing line \"" + line + "\" failed");
                    }
                    catch (FormatException e)
                    {
                        success = false;
                        Debug.WriteLine("Format exception; parsing line \"" + line + "\" failed");
                        continue;
                    }
                    finally
                    {
                        if (success)
                        {
                            if (_lookup.ContainsKey(key))
                            {
                                Console.WriteLine("Values for key already in table = (" + _lookup[key].Item1 + ", " + _lookup[key].Item2 +
                                    ") and new vals = (" + tokens[1] + ", " + tokens[2] + ")");
                            }
                            else _lookup.Add(key, toilAndTrouble);
                        }
                    }

                }
            }
            DumpLookupFailSafe(new SortedList<string, Tuple<double, double>>(_lookup));
        }

        public void SetPopToAllCols()
        {
            foreach (T opto in _population) opto.AllColumns();
        }

        public void AddToPopulation(T add, int index){
            if(index >= 0 && index < _popSize){
                _population[index] = add;
            }
        }

        public OptimoEvolver()
            : this(50, CrossoverType.Uniform)
        { }

        public void AdvanceGeneration()
        {
            dumpPopulationToFile();
            if (MultiThread) ThreadEvalAllOptimizers();
            else SeriallyEvalOptimizers();
            getMetrics();
            _population.Sort();
            _population.Reverse();
            CompactNonElites();
            GenerateNextGeneration();
            if (FocusOnAllColumns) SetPopToAllCols();
            ++generation;
        }

        private void getMetrics()
        {
            double count = 0, sum = 0, max = -1;
            foreach (T x in Population)
            {
                if (x.Fitness < 0)
                {
                    x.Fitness = 0;
                }
                count += 1;
                max = (x.Fitness > max ? x.Fitness : max);
                sum += x.Fitness;
                AverageFitness = sum / count;
            }
            AverageFitnessTracker.Add(AverageFitness);
            BestFitnessTracker.Add(max);

        }

        protected void CompactNonElites()
        {
            for (int i = (int)(OptoGlobals.ElitismPercent * _popSize); i < _popSize; ++i)
            {
                _population[i].CompactMemory();
            }
        
        }

        public virtual void VerifyOutput()
        {
            foreach (string x in _lookup.Keys)
            {
                T temp = new T();
                temp.SetBitsToString(x);
                temp.Prepare();
                temp.Eval();
                _lookup[x] = new Tuple<double, double>(temp.GetConfusionMatrix().AverageAccuracy, temp.GetConfusionMatrix().MatthewsCorrelationCoefficient);
            }
        }

        public Double GetAverageFeatureCount()
        {
            double count = 0;
            foreach (T O in _population)
            {
                foreach (bool bit in O.Bits.Range(0, (uint)OptoGlobals.NumberOfFeatures)) count += Convert.ToInt16(bit);
            }
            return count / _popSize;
        }

        public Double GetAverageColumnCount()
        {
            double count = 0;
            foreach (T O in _population)
            {
                foreach (bool bit in O.Bits) count += Convert.ToInt16(bit);
            }
            return count / _popSize;
        }
        protected void GenerateNextGeneration()
        {
            List<T> BreedingPop = StochasticUniformSample(_population);
            List<T> nextGen = Elitism(_population);
            EvoOptimizer<T>.FillListFromBreedingPop(nextGen, BreedingPop, _popSize, EvoOptimizer<T>.UniformCrossover);
            for (int i = (int)(OptoGlobals.ElitismPercent * _popSize); i < _popSize; ++i)
            {
                T temp = nextGen[i];
                EvoOptimizer<T>.Mutate(ref temp, OptoGlobals.MutationChance);
                nextGen[i] = temp;
            }

            _population = nextGen;
        }

        protected static List<T> Elitism(List<T> population)
        {
            int popSize = (int)(OptoGlobals.ElitismPercent * population.Count);
            List<T> ret = new List<T>(population.Count);
            for (int i = 0; i < popSize; ++i)
            {
                ret.Add(population[i]);
            }
            return ret;
        }

        protected static List<T> StochasticUniformSample(List<T> inPop)
        {
            List<T> outPop = new List<T>();
            double popFitness = 0.0;
            int popSize = inPop.Count;
            for (int i = 0; i < popSize; i++) popFitness += inPop[i].Fitness;
            double distance = 1.0 / (popSize / 2);//The distance between each of the markers
            double start = OptoGlobals.RNG.NextDouble() % distance;//Ensure the start point is before the first marker
            double[] pointers = new double[popSize / 2];
            for (int i = 0; i < popSize / 2; ++i) pointers[i] = start + i * distance;//populate the pointers
            double cumulativeFitness = 0.0d;
            int l = 0;
            foreach (double p in pointers)
            {
                while (cumulativeFitness < p)
                {
                    //When fitness is fairly close, relatively speaking, this will roughly uniformly sample the population picking every odd or every even
                    //This is a feature, not a bug- when there's wide disparity, 
                    cumulativeFitness += inPop[l++].Fitness / popFitness;
                    if (l >= popSize)
                    {
                        l--;
                    }
                    //CumulativeFitness approaches 1, however due to rounding errors, it sometimes goes a little over
                    //ie., it approaches 1.0000000007, which causes the last index to get skipped which causes a slew of further problems.
                    //Therefore, if it does do something like that, just decrement l and continue.
                }

                outPop.Add(inPop[l]);

            }
            Debug.Assert(outPop.Count == popSize / 2);
            return outPop;

        }


        protected virtual void SeriallyEvalOptimizers()
        {
            foreach (Optimizer O in _population)
            {//Add any strings that need evaluations 
                String OString = O.ToString();
                if (_lookup.ContainsKey(OString))
                {
                    O.Fitness = _lookup[OString].Item1;
                    O.MCC = _lookup[OString].Item2;
                    O.MakeEmptyConfumat();
                }
                else
                {
                    O.Eval();
                }
            }

            finishUpEvaluations();

        }

        protected virtual void ThreadEvalAllOptimizers()
        {
            List<Thread> threadPool = new List<System.Threading.Thread>(_popSize);
            foreach (Optimizer O in _population)
            {//Add any strings that need evaluations 
                String OString = O.ToString();
                if (_lookup.ContainsKey(OString))
                {
                    O.Fitness = _lookup[OString].Item1;
                    O.MCC = _lookup[OString].Item2;
                }
                else
                {
                    Thread a = new Thread(O.Eval);
                    a.Name = Optimizer.Token + OString;
                    threadPool.Add(a);

                }
            }
            foreach (Thread t in threadPool)
            {
                t.Start();
            }


            
            while (threadPool.Any<Thread>(t => t.IsAlive)) Thread.Sleep(500);
            finishUpEvaluations();

        }

        protected void finishUpEvaluations()
        {
            if (CountFeatures)
            {
                int[] featureCounterTemp = new int[OptoGlobals.NumberOfFeatures];

                foreach (T critter in _population)
                {
                    for (int i = 0; i < OptoGlobals.NumberOfFeatures; ++i)
                        if (critter.Bits[i]) ++featureCounterTemp[i];
                }
                featureCounter.Add(featureCounterTemp);

            }
            foreach (T critter in _population)
            {

                if (_lookup.ContainsKey(critter.ToString())) continue;
                else _lookup.Add(critter.ToString(), new Tuple<Double, Double>(critter.Fitness, critter.MCC));
            }
        }

        public String PrintFeatureCountsForGeneration(int gen)
        {
            if (!CountFeatures) return "Not Counting";
            StringBuilder ret = new StringBuilder();
            
            for (int i = 0; i < OptoGlobals.NumberOfFeatures; ++i)
            {
                ret.Append(featureCounter[gen][i]);
                ret.Append(",");
            }

            return ret.ToString();
        }

        public String PrintAllFeatureCounts()
        {
            if (!CountFeatures) return "Not Counting Features";
            StringBuilder ret = new StringBuilder();
            int startingColumn = OptoGlobals.dataHeaders.Count - OptoGlobals.NumberOfFeatures;
            ret.Append("Generation,");
            for (int i = 0; i < OptoGlobals.NumberOfFeatures; ++i)
            {
                ret.Append(OptoGlobals.dataHeaders[startingColumn + i] + ",");
            }
            ret.AppendLine();

            for (int i = 0; i < featureCounter.Count; ++i)
            {
                ret.Append(i);
                ret.Append(",");
                ret.AppendLine(PrintFeatureCountsForGeneration(i));
            }
            return ret.ToString();
        }

        List<int[]> featureCounter = new List<int[]>();

        public virtual void DumpLookupToFile(string p)
        {
            if (Population[0].GetConfusionMatrix().totalReportedNegatives == Population[0].GetConfusionMatrix().totalReportedPositives) Population[0].Eval();
            SortedList<String, Tuple<Double, Double>> output = new SortedList<string, Tuple<Double, Double>>(_lookup);
                    _population[0].Save("Best " + Optimizer.Token + " gen" + generation);

                using (StreamWriter fout2 = new StreamWriter(new BufferedStream(File.Create("LabelsFromBest" + p))))
                {
                    fout2.WriteLine(_population[0].ToString() + ", ");
                    _population[0].DumpLabelsToStream(fout2);
                }
                using (StreamWriter fout2 = new StreamWriter(new BufferedStream(File.Create("CVLabelsFromBest" + p))))
                {
                    fout2.WriteLine(_population[0].ToString() + ", ");
                    _population[0].DumpCVLabelsToStream(fout2);
            }
                    DumpLookupFailSafe(output);
            using (StreamWriter fout2 = new StreamWriter(new BufferedStream(File.Create(Population[0].GetToken + "CumulativeFitness.csv"))))
            {

                StringBuilder genLine = new StringBuilder("Generation:,"), avgLine = new StringBuilder("Average Fitness:,"), bestLine = new StringBuilder("Best Fitness:,");
                for (int i = 0; i < BestFitnessTracker.Count; ++i)
                {
                    genLine.Append(i);
                    genLine.Append(",");
                    avgLine.Append(AverageFitnessTracker[i] + ",");
                    bestLine.Append(BestFitnessTracker[i] + ",");


                }
                fout2.WriteLine(genLine.ToString());
                fout2.WriteLine(avgLine.ToString());
                fout2.WriteLine(bestLine.ToString());
                fout2.WriteLine();

            }

            

        }

        protected virtual void DumpLookupFailSafe(SortedList<String, Tuple<Double, Double>> output)
        {
            bool success = false;

            while (!success)
            {
                try
                {
                    using (StreamWriter fout = new StreamWriter(new BufferedStream(File.Create(_outputPath))))
                    {

                        fout.WriteLine("String, Accuracy, MCC");
                        foreach (KeyValuePair<String, Tuple<Double, Double>> kvp in output)
                        {
                            fout.WriteLine(kvp.Key + ", " + kvp.Value.Item1 + ", " + kvp.Value.Item2);
                        }
                        //Optimizer.RefreshEvaluator();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message + "\n Waiting to retry (indefinitely)");
                    Thread.Sleep(1000);
                    success = false;
                }
                success = true;
            }
        }
    }
}