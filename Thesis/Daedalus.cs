using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Emerson.CSI.DataProvider;
using MyUtils;
namespace Emerson.CSI.Applet.MHM.Internal.EvoAlgApplet
{
    /// <summary>
    /// TODO:
    /// Put IDS and intensities into another file, tab separated
    /// Make a dictionary with the ID 
    /// So, the program needs to use the machine name to look up the ID 
    /// </summary>

    public class Daedalus : IDisposable
    {
        #region supportingClasses:
        #region class ConfusionDouble<T>:
        /// <summary>
        /// This is a simple double which holds a type and a confusion matrix.  This allows for simple storage and retrieval of statistics.  
        /// </summary>
        /// <typeparam name="T">Hunter or Prey, or anything that implements IComparable and new().</typeparam>
        public class ConfusionDouble<T> : IComparable<ConfusionDouble<T>> where T : IComparable<T>, IEvoCritter<T>, new()
        {

            T myT;
            MultidimensionalConfusionMatrix myMatrix;

            public ConfusionDouble(T h, MultidimensionalConfusionMatrix c)
            {
                myT = h;
                myMatrix = c;
            }
            public ConfusionDouble()
            {

                myT = new T();
                myMatrix = EvoGlobals.DefaultCM();

            }
            public ConfusionDouble(ConfusionDouble<T> other)
            {

                myT = other.TheT;
                myMatrix = other.Matrix;
            }
            public string Serialize()
            {
                string ret = typeof(T) == typeof(Hunter) ? "Hunter:" : "Prey:";
                ret += myT.Serialize();
                ret += "\n" + myMatrix.Serialize() + "\n";
                return ret;
            }
            public T TheT { get { return myT; } }
            public MultidimensionalConfusionMatrix Matrix { get { return myMatrix; } }



            int IComparable<ConfusionDouble<T>>.CompareTo(ConfusionDouble<T> other)
            {
                return myT.CompareTo(other.myT);
            }
        }
        #endregion
        #region class TupleSet<T>:
        private class TupleSet<T> where T : IComparable<T>, IEvoCritter<T>, new()
        {
            private ArrayList list = new ArrayList();


            public ConfusionDouble<T> this[int index]
            {
                get
                {
                    if (index >= list.Count) return null;
                    return ((ConfusionDouble<T>)list[index]);
                }
                set
                {
                    list[index] = value;
                }
            }

            public int Add<T>(ConfusionDouble<T> value) where T : IEvoCritter<T>, IComparable<T>, new()
            {
                return (list.Add(value));
            }

            public int IndexOf(ConfusionDouble<T> value)
            {
                return (list.IndexOf(value));
            }

            public void Insert(int index, ConfusionDouble<T> value)
            {

                list.Insert(index, value);
            }

            public void Remove(ConfusionDouble<T> value)
            {
                list.Remove(value);
            }
            public void RemoveAt(int index){
                list.RemoveAt(index);
            }
            public bool Contains(ConfusionDouble<T> value)
            {

                return (list.Contains(value));
            }

            protected void OnInsert(int index, ConfusionDouble<T> value)
            {
                // Insert additional code to be run only when inserting values.
            }

            protected void OnRemove(int index, ConfusionDouble<T> value)
            {
                // Insert additional code to be run only when removing values.
            }

            protected void OnSet(int index, ConfusionDouble<T> oldValue, ConfusionDouble<T> newValue)
            {
                // Insert additional code to be run only when setting values.
            }

            protected void OnValidate(ConfusionDouble<T> value)
            {
                if (value.GetType() != typeof(ConfusionDouble<T>))
                    throw new ArgumentException("value must be of type MyTuple.", "value");
            }

            public void Sort()
            {
                IEvoCritter<T> biff = new T();

                if (typeof(T) == typeof(Hunter))
                    list.Sort(CompareHunterTuples());
                else
                    list.Sort(ComparePreyTuples());
                 
            }

            private IComparer ComparePreyTuples()
            {
                return new PreyTupleComparer();
            }

            private IComparer CompareHunterTuples()
            {
                return new HunterTupleComparer();
            }

            private IComparer ComparePrey()
            {
                throw new NotImplementedException();
            }

            private IComparer CompareHunters()
            {
                return new HunterComparer();
            }
            private class HunterTupleComparer : IComparer
            {

                int IComparer.Compare(object x, object y)
                {
                    ConfusionDouble<Hunter> a = x as ConfusionDouble<Hunter>, b = y as ConfusionDouble<Hunter>;
                    int comp = b.TheT.Fitness.CompareTo(a.TheT.Fitness);
                    if (comp == 0) return b.Matrix.MatthewsCorrelationCoefficient.CompareTo(a.Matrix.MatthewsCorrelationCoefficient);

                    return comp;
                }
            }
            private class PreyTupleComparer : IComparer
            {

                int IComparer.Compare(object x, object y)
                {
                    ConfusionDouble<Prey> a = x as ConfusionDouble<Prey>, b = y as ConfusionDouble<Prey>;
                    return a.TheT.CompareTo(b.TheT);
                }
            }



            private class HunterComparer : IComparer
            {

                public int Compare(object x, object y)
                {
                    Hunter a = x as Hunter, b = y as Hunter;
                    return a.CompareTo(b);
                }
            }
            public class PreyComparer : IComparer
            {

                public int Compare(object x, object y)
                {
                    Prey a = x as Prey, b = y as Prey;
                    return a.CompareTo(b);
                }
            }
            /// <summary>
            /// Generates a new confusion matrix, and returns the hunter/prey to initial values except for the chromasomes
            /// </summary>
            /// <param name="c"></param>
            public

 void AddCopy<T>(ConfusionDouble<T> c) where T : IEvoCritter<T>, IComparable<T>, new()
            {

                ConfusionDouble<T> temp = new ConfusionDouble<T>(c.TheT.EliteCopy(), EvoGlobals.DefaultCM());
                Add(temp);

            }

            public int Count { get { return list.Count; } }

            public void PopBack()
            {
                this.list.RemoveAt(list.Count - 1);
            }

        }
        #endregion
        
        #endregion

        #region class Daedalus
        List<Equipment> dataTable;
        List<List<IItem>> children;
        private TupleSet<Hunter> hunterPop;
        private TupleSet<Prey> preyPop;
        private TupleSet<Hunter> validationPop;
        private int gen = 0;
        private Random RNG = EvoGlobals.GlobalRNG;
        List<IItem> testCases;
        public const int HunterPopSize = 50;
        public const int PreyPopSize = 20;
        bool overwriteHistory = false;
        private TupleSet<Hunter> bleachers = new TupleSet<Hunter>();
        /// <summary>
        /// What should show up in History?  We want highest hit rate, average hit rate, 
        /// lowest hitrate, highest and average complexity, and highest prey fitness if applicable
        /// </summary>
        public List<double[]> History;//double is size 6
        public List<double[]> output;//Record highest MCC, Accuracy, FN Rate and FP Rate for both validation and non-validation data 
        
        const int historyHMC = 0, historyAMC = 1, historyLMC = 2, historyHC = 3, historyAC = 4, historyPF = 5, historyHHF = 6,  historyGen = 7,historyArraySize = 8;
        const int maxHistory = 2000;//Number of generations to be saved
        public int Generation { get { return gen; } }
        bool preyReleased = false;
        public bool ReleaseTheChameleons { get { return preyReleased; } set { if (!preyReleased) preyReleased = value; } }
        RightAnswerGetter answers = new RightAnswerGetter();
        StreamWriter historyLog;
        public Daedalus()
        {
            hunterPop = new TupleSet<Hunter>();
            preyPop = new TupleSet<Prey>();
            output = new List<double[]>();
            for (int i = 0; i < HunterPopSize; i++)
            {
                hunterPop.Add(new ConfusionDouble<Hunter>(new Hunter(RNG.Next(1,3)), EvoGlobals.DefaultCM()));
                
            }
            SetClassBitsForAllHunters();

            History = new List<double[]>();
            dataTable = new List<Equipment>();
            if (openStreamAsNewFile() == false) throw new FileNotFoundException("File system not cooperating");
        }
        int classCount = 0;
        private void SetClassBitsForAllHunters()
        {
            for (int i = 0; i < HunterPopSize; ++i)
            {
                Hunter temp = hunterPop[i].TheT as Hunter;
                temp.overWriteChromaClassBits(ref classCount);
            }
            classCount %= 16;
        }
        private bool openStreamInAppendMode()
        {
            return true;
            historyLog = new StreamWriter(EvoGlobals.DaedalusHistoryPath, true);
            if(historyLog != null) return true;
            return false;
        }
        private bool openStreamAsNewFile()
        {
            if (File.Exists(EvoGlobals.DaedalusHistoryPath)) File.Delete(EvoGlobals.DaedalusHistoryPath);
            historyLog = new StreamWriter(EvoGlobals.DaedalusHistoryPath, true);
            if (historyLog != null) return true;
            return false;
        }
        private object getData()
        {
            return null;
        }

        public string SnapShot
        {
            get
            {
                int i = gen-1;
                if (gen > maxHistory) i = maxHistory - 1; 
                if (gen == 0 || History[i] == null) return "No data\n";
                return string.Format("Generation {7}\n(Hunter)\nHighest Fitness:\t\t{6}\nHighest MCC:\t\t{0}\nAverage MCC:"+
                    "\t\t{1}\nLowest MCC:\t\t{2}\nHighest Complexity:\t\t{3}\nAverageComplexity:\t\t{4}\n\n(Prey)\n"+
                    "Highest Prey Fitness:\t\t{5}\n",
                    History[i][historyHMC], History[i][historyAMC], History[i][historyLMC], History[i][historyHC],
                    History[i][historyAC], History[i][historyPF], History[i][historyHHF], gen);
            }
        }

        public void AdvanceGeneration()
        {
            try
            {
                evaluateFitness();
                hunterPop.Sort();
                validationPop.Sort();
                updateHistory();
                updateOutput();
                plateauBuster();
                Int64 mem = System.GC.GetTotalMemory(false);
                if (mem > 850000000)
                {
                    EvoAlgFunctionSuite.ResetLookupTable();
                }
                if (false)
                {
                    ConfusionMatrix cm = hunterPop[0].Matrix;
                    if (cm.FalseNegatives == 0 && cm.MatthewsCorrelationCoefficient > .75) preyReleased = true;
                }

                Hunter supreme = hunterPop[0].TheT;//Mostly for debugging purposes
                ConfusionMatrix cm2 = hunterPop[0].Matrix;
                double highestMCC = cm2.MatthewsCorrelationCoefficient, nextHighest = hunterPop[1].Matrix.MatthewsCorrelationCoefficient;
                generateNextGen();



                ++gen;
            }
            catch (InsufficientMemoryException e)
            {
                EvoAlgFunctionSuite.ResetLookupTable();
            }
            catch (OutOfMemoryException e)
            {
                EvoAlgFunctionSuite.ResetLookupTable();
            }
        }
        double currentMax = -10;
        int generationsAtMax = 0;
        int BleachersMax = (int)(EvoGlobals.ElitismPercentage * HunterPopSize);
        int BleachersGen = 100, fromBleacherGen = 0, toBleacherGen = -1;
        double BaselineFitness = -0.1;
        private void plateauBuster()
        {
            double MCC = getHighestHunterFitness();
            double diff = MCC - currentMax;
                currentMax = MCC;           
            //Diff < 0? 
            if (Math.Abs(diff) < 5.0e-16 || diff < 0)
            {
                ++generationsAtMax;
            }
            else
            {
                generationsAtMax=1;
            }
                if (generationsAtMax > BleachersGen)
                {
                    generationsAtMax = 1;
                    BaselineFitness = -.1;
                    fromBleacherGen = toBleacherGen+1;
                    toBleacherGen = Generation;
                    using (StreamWriter e = new StreamWriter(new FileStream("bleacherOutputFr-" + fromBleacherGen + "To-" + toBleacherGen + ".csv", FileMode.Create)))
                    {
                        writeHistory(fromBleacherGen, toBleacherGen, e);
                        e.WriteLine();
                        e.WriteLine(hunterPop[0].TheT.Serialize());
                        Hunter temp = hunterPop[0].TheT as Hunter;
                        e.WriteLine(temp.HumanReadableHunter);
                        e.WriteLine(hunterPop[0].Matrix.Serialize());
                    }

                    if (bleachers.Count == BleachersMax)
                    {
                        for (int i = 0; i < BleachersMax; ++i) hunterPop.PopBack();
                        for (int i = 0; i < BleachersMax; ++i) hunterPop.Add(bleachers[i]);
                        hunterPop.Sort();
                        using (StreamWriter f = new StreamWriter("Bleachers.txt", true))
                        {
                            for (int i = 0; i < BleachersMax; ++i)
                            {
                                f.WriteLine(bleachers[i].TheT.HumanReadableHunter);
                                f.WriteLine(bleachers[i].TheT.Serialize());
                                f.WriteLine("TP, TN, FP, FN, MCC, Accuracy");
                                ConfusionMatrix m = bleachers[i].Matrix;
                                f.WriteLine(String.Format("{0}, {1}, {2}, {3}, {4}, {5}", m.TruePositives, m.TrueNegatives, m.FalsePositives, m.FalseNegatives,
                                    m.MatthewsCorrelationCoefficient, m.Accuracy));
                            }
                            f.Close();
                        }
                        for (int i = 0; i < BleachersMax; ++i) bleachers.PopBack();

                    }
                    else
                    {
                        bleachers.Add(hunterPop[0]);
                        double comp = getAverageComplexity();
                        for (int i = 0; i < HunterPopSize; ++i)
                        {
                            hunterPop.RemoveAt(0);

                            hunterPop.Add(new ConfusionDouble<Hunter>(new Hunter(RNG.Next(1, 20)), EvoGlobals.DefaultCM()));

                        }
                        SetClassBitsForAllHunters();

                    }

                }

            
        }

        public void reseedDaedalus(int complexity, int numCells)
        {
            for (int i = 2; i < HunterPopSize; ++i)
            {
                int chromosomes = complexity / (i % numCells + 1), cells = 1 + (i % numCells);
                Hunter temp = new Hunter(chromosomes, cells);

                ConfusionDouble<Hunter> t = new ConfusionDouble<Hunter>(temp, EvoGlobals.DefaultCM());
                hunterPop[i] = t;
            }
        }

        public void resetAffinities()
        {
            for (int i = 0; i < HunterPopSize; ++i)
            {
                Hunter temp = hunterPop[i].TheT;
                temp.resetAffinity();
            }
        }

        private void updateHistory()
        {
            int i = gen;
            if (gen >= maxHistory)
            {
                i = maxHistory -1;
                if(History.Count > 0) History.RemoveAt(0);
            }
            History.Add(new double[historyArraySize]);

            History[i][historyHMC] = getHighestMCC();
            History[i][historyAMC] = getAverageMCC();
            History[i][historyLMC] = getLowestMCC();
            History[i][historyHC] = getHighestComplexity();
            History[i][historyAC] = getAverageComplexity();
            History[i][historyPF] = getHighestPreyFitness();
            History[i][historyHHF] = getHighestHunterFitness();
            History[i][historyGen] = gen;
            
            //historyLog.Write(HistoryAsString);
            //historyLog.Flush();
        }
        private void updateOutput()
        {
            //Record highest MCC, Accuracy, FN Rate and FP Rate for both validation and non-validation data
            double[] thisGen = new double[8];
            if (output == null) output = new List<double[]>();
            int i = 0;
            ConfusionDouble<Hunter> bestNormal = hunterPop[i], bestValid = validationPop[i];
            while(bestNormal.TheT != bestValid.TheT){
                bestValid = validationPop[++i];
                if(i >= validationPop.Count) return;
            }
            thisGen[0] = bestNormal.Matrix.MatthewsCorrelationCoefficient;
            thisGen[1] = bestValid.Matrix.MatthewsCorrelationCoefficient; 
            thisGen[2] = bestNormal.Matrix.Accuracy;
            thisGen[3] = bestValid.Matrix.Accuracy;
            thisGen[4] = bestNormal.Matrix.FallOut;
            thisGen[5] = bestValid.Matrix.FallOut;
            thisGen[6] = bestNormal.Matrix.MissRate;
            thisGen[7] = bestValid.Matrix.MissRate;
            output.Add(thisGen);

        }



        private double getHighestHunterFitness()
        {
            double max = Double.NegativeInfinity;
            for (int i = 0; i < HunterPopSize; ++i)
                max = Math.Max(hunterPop[i].TheT.Fitness, max);
            return max;
        }

        private double getHighestPreyFitness()
        {
            double max = Double.NegativeInfinity;
            if (ReleaseTheChameleons)
            {
                for (int i = 0; i < PreyPopSize; ++i) max = Math.Max(preyPop[i].TheT.Fitness, max);
            }
            return max;
        }

        private double getAverageComplexity()
        {
            double sum = 0;
            for (int i = 0; i < hunterPop.Count; ++i) sum += hunterPop[i].TheT.Complexity;
            return sum / hunterPop.Count;
        }

        private double getHighestComplexity()
        {
            double max = double.NegativeInfinity;
            for (int i = 0; i < HunterPopSize; ++i) max = Math.Max(hunterPop[i].TheT.Complexity, max);
            return max;
        }

        private double getLowestMCC()
        {
            double min = double.PositiveInfinity;
            for (int i = 0; i < HunterPopSize; ++i) min = Math.Min(hunterPop[i].Matrix.MatthewsCorrelationCoefficient, min);
            return min;
        }

        private double getAverageMCC()
        {
            double sum = 0;
            for (int i = 0; i < HunterPopSize; ++i) sum += hunterPop[i].Matrix.MatthewsCorrelationCoefficient;
            return sum / HunterPopSize;
        }

        private double getHighestMCC()
        {

            double max = double.NegativeInfinity;
            for (int i = 0; i < HunterPopSize; ++i) max = Math.Max(hunterPop[i].Matrix.MatthewsCorrelationCoefficient, max);
            return max;
        }

        private void generateNextGen()
        {

            BitArray kept = new BitArray(HunterPopSize, false);//Signifies which have made it into keep, not sure why I thought that was important, but I'm trusting myself.  For now.
            if (ReleaseTheChameleons)
            {
                preyPop.Sort();
                TupleSet<Prey> preyBreeders = new TupleSet<Prey>();
                TupleSet<Prey> preyNextGen = new TupleSet<Prey>();

                BitArray preyKept = new BitArray(PreyPopSize, false);
                elitism(preyKept, preyNextGen);
                stochasticUniformSample(preyKept, preyBreeders);
                breed(preyBreeders, preyNextGen);
            }
            TupleSet<Hunter> breedingPop = new TupleSet<Hunter>();
            TupleSet<Hunter> keep = new TupleSet<Hunter>();
            elitism(kept, keep);
            //Stochastic Uniform Sampling, we generate a roulette wheel using pointers and then generate a single random number
            //which marks the start of the first marker, then spread the other pointers out uniformly from there.  This guarantees that some less
            //successful Hunters will get in the mix, keeping the population diverse, but also maintains a positive selective pressure.
            stochasticUniformSample(kept, breedingPop);
            //Breed the hunters in BreedingPop, and then put the result into the hunter population
            if (EvoGlobals.TAG == true)
            {
                for (int i = 0; i < breedingPop.Count; ++i)
                {
                    char tag = (char)((int)('A' + i));
                    Hunter temp = breedingPop[i].TheT as Hunter;
                    temp.tag = " " + tag;
                }
            }
            breed(breedingPop, keep);
            hunterPop = keep;

        }

        private void stochasticUniformSample<T>(BitArray kept, TupleSet<T> breedingPop) where T : IComparable<T>, IEvoCritter<T>, new()
        {
            double popFitness = 0.0;
            int popSize = typeof(T) == typeof(Hunter) ? HunterPopSize : PreyPopSize;
            TupleSet<T> basePop = (typeof(T) == typeof(Hunter) ? hunterPop as TupleSet<T> : preyPop as TupleSet<T>);
            for (int i = 0; i < popSize; i++) popFitness += basePop[i].TheT.GetFitness();
            double distance = 1.0 / (popSize / 2);//The distance between each of the markers
            double start = RNG.NextDouble() % distance;//Ensure the start point is before the first marker
            double[] pointers = new double[popSize / 2];
            for (int i = 0; i < popSize / 2; ++i) pointers[i] = start + i * distance;//populate the pointers
            double cumulativeFitness = 0.0d;
            int l = 0;
            foreach (double p in pointers)
            {
                while (cumulativeFitness < p)
                {
                    cumulativeFitness += basePop[l++].TheT.GetFitness() / popFitness;
                    if (l >= popSize)
                    {
                        l--;
                    }
                    //CumulativeFitness approaches 1, however due to rounding errors, it sometimes goes a little over
                    //ie., it approaches 1.0000007, which causes the last index to get skipped which causes a slew of further problems.
                    //Therefore, if it does do something like that, just decrement l and continue.
                }

                breedingPop.Add(typeof(T) == typeof(Hunter) ? hunterPop[l] as ConfusionDouble<T> : preyPop[l] as ConfusionDouble<T>);
                if (l < popSize) kept[l] = true;

            }
            Debug.Assert(breedingPop.Count == popSize / 2);
        }

        public string SerializeDaedalus()
        {
            StringBuilder ret = new StringBuilder(String.Format("Generation:{1}{0}Hunters:{0}", Environment.NewLine, gen.ToString()));
            for (int i = 0; i < HunterPopSize; ++i) ret.Append(hunterPop[i].Serialize());
          
            for (int i = 0; i < bleachers.Count; ++i)
            {
                ret.Append("Bleachers:");
                ret.Append(bleachers[i].Serialize()+Environment.NewLine);

            }

            return ret.ToString();
        }

        public Daedalus(string data)
        {
            RNG = EvoGlobals.GlobalRNG;
            hunterPop = new TupleSet<Hunter>();
            preyPop = new TupleSet<Prey>();
            History = new List<double[]>();
            dataTable = new List<Equipment>();
            overwriteHistory = true;
            Hunter h = null; Prey p = null;
            bool gettingHunter = true;
            if (openStreamInAppendMode() == false) throw new FileNotFoundException("File System not being cooperative");
            using (StreamReader read = new StreamReader(EvoGlobals.DaedalusDataPath, Encoding.Default, true, 1))
            {
                while (read.EndOfStream == false)
                {
                    Chromasome c;
                    MultidimensionalConfusionMatrix cm;
                    bool gettingBleachers = false;
                    int position = 0, start;
                    string line = read.ReadLine();
                    if (line.StartsWith("Generation"))
                    {
                        start = line.IndexOf(':') + 1;
                        position = line.Length;

                        string bort = line.Substring(start, position - start);
                        gen = Convert.ToInt32(bort);
                    }
                    else if (line.StartsWith("Hunter:"))
                    {
                        gettingHunter = true;
                        position = line.IndexOf("Aff:") + 4;
                        start = line.IndexOf("Cl:");
                        h = new Hunter().StripChromasomes();
                        while (read.EndOfStream == false &&
                            extractAndAddChromasome(read, h, ref position, ref line, out c)) ;

                    }
                    else if (line.StartsWith("Matrix:"))
                    {
                        cm = parseConfusionMatrix(line, position);
                        if (gettingHunter && !gettingBleachers)
                        {
                            ConfusionDouble<Hunter> bob = new ConfusionDouble<Hunter>(h, cm);
                            hunterPop.Add(bob);
                        }
                        else if
                            (gettingHunter && gettingBleachers)
                        {
                            bleachers.Add(new ConfusionDouble<Hunter>(h, cm));
                        }
                        else
                        {
                            ConfusionDouble<Prey> bob = new ConfusionDouble<Prey>(p, cm);
                            preyPop.Add(bob);
                        }
                    }

                    else if (line.StartsWith("Prey:"))
                    {
                        if (line.Length < Prey.ChromasomeLength) continue;
                        gettingHunter = false;
                        BitArray pbits = new BitArray(Prey.ChromasomeLength, false);
                        string temp = line.Substring(position, Prey.ChromasomeLength);
                        for (int i = 0; i < Prey.ChromasomeLength; ++i) pbits[i] = temp[i] == '1';
                        p = new Prey(pbits);
                    }
                    else if (line.StartsWith("History:"))
                    {
                        History = new List<double[]>(gen);
                        line = read.ReadLine();
                        char[] separ = { '|' };
                        string[] doubles = line.Split(separ);
                        for (int i = gen < maxHistory? 0 : gen - maxHistory; i < gen; ++i)
                        {
                            double[] hist = new double[historyArraySize];

                            for (int j = 1; j < historyArraySize; ++j)
                            {
                                hist[j - 1] = Convert.ToDouble(doubles[j]);
                            }
                            History.Add(hist);
                        }
                    }

                    else if (line.StartsWith("Bleachers"))
                    {
                        gettingBleachers = true;
                        gettingHunter = true;
                        position = line.IndexOf("Aff:") + 4;
                        start = line.IndexOf("Cl:");
                        h = new Hunter().StripChromasomes();
                        while (read.EndOfStream == false &&
                            extractAndAddChromasome(read, h, ref position, ref line, out c)) ;

                    }
                    else continue;//Ignore other lines;
                }
                read.Close();
            }
            gen = 0;
        }

        private static bool extractAndAddChromasome(StreamReader read, Hunter h, ref int position, ref string line, out Chromasome c)
        {
            string temp = line.Substring(position, Chromasome.AffinityBitLength);
            BitArray AffBits = new BitArray(Chromasome.AffinityBitLength, false),
                classBits = new BitArray(Chromasome.ClassBitLength, false);
            for (int i = 0; i < AffBits.Length; ++i) AffBits[i] = temp[i] == '1';
            position = line.IndexOf("Cl:") + 3;
            temp = line.Substring(position, Chromasome.ClassBitLength);

            for (int i = 0; i < Chromasome.ClassBitLength; ++i) classBits[i] = temp[i] == '1';
            bool notFlag;
            temp = line.Substring(line.IndexOf("Not:") + 4, 3);
            notFlag = temp[0] == '1' || temp[1] == '1';
            c = new Chromasome(AffBits, classBits);
            line = read.ReadLine();
            position = 0;
            while (extractAndAddCell(c, ref position, line, ref temp)) ;

            position = line.IndexOf("endChromasome");
            if (position == -1) throw new ArgumentException("endChromasome not found where it should be.");
            h.AddChromasome(c);
            line = read.ReadLine();
            position = 0;
            if (line.IndexOf("endHunter") != -1) return false;//done with chromasomes
            return true;
        }

        private static bool extractAndAddCell(Chromasome c, ref int position, string line, ref string temp)
        {
            position = line.IndexOf(":", position) + 1;
            if (position == 0) return false;//String not found, done with cells
            BitArray cbits = new BitArray(Cell.CellLength, false);
            temp = line.Substring(position, Cell.CellLength);
            for (int i = 0; i < Cell.CellLength; ++i) cbits[i] = temp[i] == '1';
            position += Cell.CellLength;
            Cell tCell = new Cell(cbits, null);
            c.AddCell(tCell);
            return true;
        }
        private static MultidimensionalConfusionMatrix parseConfusionMatrix(string data, int position)
        {
            MultidimensionalConfusionMatrix c = EvoGlobals.DefaultCM();

            while (data[position++] != ' ' && position < data.Length) ;//eat "matrix: "
            int tp, fp, tn, fn;
            tp = tn = fp = fn = 0;
            int start = position;
            while (data[position++] != ',' && position < data.Length) ;//advance to comma
            tp = Convert.ToInt32(data.Substring(start, position - start - 1));
            while (data[position++] != ' ' && position < data.Length) ;//eat space
            start = position;
            while (data[position++] != ',' && position < data.Length) ;//advance to comma
            tn = Convert.ToInt32(data.Substring(start, position - start - 1));
            while (data[position++] != ' ' && position < data.Length) ;//eat space
            start = position;
            while (data[position++] != ',' && position < data.Length) ;//advance to comma
            fp = Convert.ToInt32(data.Substring(start, position - start - 1));
            while (data[position++] != ' ' && position < data.Length) ;//eat space
            start = position;
            while (data[position++] != '|' && position < data.Length) ;//advance to pipe
            fn = Convert.ToInt32(data.Substring(start, position - start - 1));

            return c;
        }


        private void elitism<T>(BitArray kept, TupleSet<T> keep) where T : IEvoCritter<T>, IComparable<T>, new()
        {
            //elitism
            int popSize = typeof(T) == typeof(Hunter) ? HunterPopSize : PreyPopSize;
            TupleSet<T> basePop = (typeof(T) == typeof(Hunter) ? hunterPop as TupleSet<T> : preyPop as TupleSet<T>);
            int indexToKeep = (int)Math.Floor(EvoGlobals.ElitismPercentage * popSize);

            for (int i = 0; i < indexToKeep; ++i)
            {
                keep.AddCopy(basePop[i]);
                kept[i] = true;//
            }

        }
        /// <summary>
        /// Breed the hunters, and ensure that keep.count == HunterPopSize
        /// </summary>
        /// 
        /// <param name="breedingPop"></param>
        /// <param name="keep"></param>
        private void breed<T>(TupleSet<T> breedingPop, TupleSet<T> keep) where T : IEvoCritter<T>, IComparable<T>, new()
        {
            int popSize = typeof(T) == typeof(Hunter) ? HunterPopSize : PreyPopSize;
            for (int i = 0, u = 0; i < breedingPop.Count; i++)
            {
                double modifier, effectiveMergerPercentage;
                modifier = 10.0 / getAverageComplexity();
                effectiveMergerPercentage = modifier * EvoGlobals.MergerPercentage;
                //While u == i
                do { u = RNG.Next(0, breedingPop.Count); } while (u == i);
                //check for merger first
                if (typeof(T) == typeof(Hunter) && RNG.NextDouble() <= effectiveMergerPercentage)
                {
                    merge(breedingPop as TupleSet<Hunter>, keep as TupleSet<Hunter>, i, u);

                    continue;
                }
                else
                {
                    crossover<T>(breedingPop, keep, i, u);

                }
                if (keep.Count >= popSize) break;
            }
            //This code will probably never be entered, but just in case
            while (keep.Count < popSize)
            {
                int i, u;
                do { i = RNG.Next(0, breedingPop.Count); u = RNG.Next(0, breedingPop.Count); } while (i == u);
                crossover<T>(breedingPop, keep, i, u);
            }

            while (keep.Count != popSize)
                keep.PopBack();
            for (int i = (int)Math.Floor(popSize * EvoGlobals.ElitismPercentage); i < popSize; ++i)
            {
                keep[i].TheT.Mutate();
            }

            //Now, keep is the same size as HunterPop
        }
        /// <summary>
        /// Performs crossover on the hunters kept in BreedingPop in [i] and [u], and puts their spawn into keep.
        /// </summary>
        /// <param name="breedingPop">Holds the breeders</param>
        /// <param name="keep">Holds the offspring</param>
        /// <param name="i">index of parent 1</param>
        /// <param name="u">index of parent 2</param>
        private static void crossover<T>(TupleSet<T> breedingPop, TupleSet<T> keep, int i, int u) where T : IEvoCritter<T>, IComparable<T>, new()
        {
            T raw = new T();
            while (breedingPop[u] == null) --u;
            T[] temp = raw.Crossover(breedingPop[i].TheT, breedingPop[u].TheT);
            ConfusionDouble<T>[] tempDouble = new ConfusionDouble<T>[2];
            for (int j = 0; j < 2; j++)
            {
                tempDouble[j] = new ConfusionDouble<T>(temp[j], new MultidimensionalConfusionMatrix(EvoGlobals.numFaults, EvoGlobals.faultNames));
                keep.AddCopy(tempDouble[j]);
            }
        }
        /// <summary>
        /// Performs the merge operation in breedingPop at indices i and u, then places the new hunter in keep.
        /// </summary>
        /// <param name="breedingPop">Holds the breeders</param>
        /// <param name="keep">Holds the offspring</param>
        /// <param name="i">index of parent 1</param>
        /// <param name="u">index of parent 2</param>
        private static void merge(TupleSet<Hunter> breedingPop, TupleSet<Hunter> keep, int i, int u)
        {
            while (breedingPop[u] == null) --u;
            Hunter temp = Hunter.Merger(breedingPop[i].TheT, breedingPop[u].TheT);
            ConfusionDouble<Hunter> newDouble = new ConfusionDouble<Hunter>(temp, EvoGlobals.DefaultCM());
            keep.AddCopy(newDouble);
        }
        /*
            public Chromasome[] Sample()
        {

            double distance = 1.0 / (PopSize / 2);//With pop size of 5, distance should be .2
            double start = RNG.NextDouble();
            while (start >= distance) start = RNG.NextDouble();//Ensure start is between 0 and distance (guarantee 0 gets to breed);
            double[] pointers = new double[PopSize / 2];
            for (int i = 0; i < PopSize / 2; i++)
                pointers[i] = start + i * distance;
            return Roulette(pointers);
        }

        public Chromasome[] Roulette(double[] pointers)
        {
            Chromasome[] keep = new Chromasome[Pop.Length / 2];
            int i = 0, j = 1;
            double cumulativeFitness = 0.0D;
            keep[0] = Pop[0];//Elitism
            foreach (double P in pointers)
            {
                while (cumulativeFitness < P)
                {
                    cumulativeFitness += Pop[i++].Fitness / popFitness;

                    if (i >= Pop.Length)
                        i = Pop.Length - 1;
                }
                if (j >= keep.Length) break;
                keep[j++] = Pop[i];



            }
            Array.Sort(keep);
            return keep;

        }
        }

            */
        private void evaluateFitness()
        {
            if (ReleaseTheChameleons == false)
            {
                HunterFitness();
            }
            else//Prey is in play
            {
                PreyFitness();
            }
            //So the problem now is to figure out how to evaluate fitness now that 
        }

        private void HunterFitness()
        {
            /*
                 * This should look something like : 
                 * for each testCase in testCases: 
                 *      for each hunter in population:
                 *          Hunter evaluates testCase;
                 *          assign fitness etc.,
                */
            validationPop = new TupleSet<Hunter>();
            double minFitness = 0;
            for (int i = 0; i < HunterPopSize; ++i){
                validationPop.Add(new ConfusionDouble<Hunter>(hunterPop[i].TheT, EvoGlobals.DefaultCM()));
            }
            for (int i = 0; i < HunterPopSize; i++)
            {
                Hunter temp = hunterPop[i].TheT;
                int dates = 0;
                int skipped = 0;
                dataTable.FisherYatesShuffle();
                foreach (Equipment data in dataTable)
                {
                    string key = extractKeyFromEquipment(data);
                    if (answerExistsInDB(key) == false)
                    {
                        skipped++;
                        continue;//Skip to next equipment
                    }
                    dates++;
                    Dictionary<string, int> dict = answers.getDatesForEquip(key);
                    foreach (KeyValuePair<string, int> p in dict)
                    {
                        DateTime cutoff = answers.ExtractDateFromString(p.Key);
                        double ret; int ans;
                        int rightAnswer=0;
                        bool Validate = p.Value % 5 == 0;
                        KeyValuePair<int,int> pair = this.getRightAnswer(p.Value);
                        
                        ans = pair.Key;
                        BitArray classBits = (RightAnswerGetter.convertClassToBits(pair.Value));
                        int tempsAnswer = temp.Vote(data, out ret, cutoff);

                        if (ans == -1) continue;
                        if (ans < 2) rightAnswer = 0;
                        else if (tempsAnswer != -1 && classBits[tempsAnswer]) rightAnswer = tempsAnswer;
                        //if the answer is in the class bits, since answers are multi-valued, it's a good hit
                        else while (!classBits[rightAnswer]) ++rightAnswer;
                        
                        if (ret == -1)
                            temp.Fitness -= 1.5;
                        if (!Validate)//Record the event in the standard matrix and change fitness
                        {
                            if (rightAnswer == tempsAnswer)
                            {
                                if (rightAnswer == 0) temp.Fitness += .5;
                                else temp.Fitness += 5;
                            }
                            else if (tempsAnswer == -1)
                            {
                                temp.Fitness -= .01;//Points for uncertainty
                                tempsAnswer = EvoGlobals.Uncertainty; //16 is now reserved for uncertainty
                            }
                            else temp.Fitness -= 1;//Can't do FP/FN dichotomy here, since it always is both for multiple classes 
                            
                            hunterPop[i].Matrix.RecordEvent(rightAnswer, tempsAnswer);

                        }
                        else //record Validation
                        {
                            Debug.Assert(hunterPop[i].TheT == validationPop[i].TheT);//Pointer comparison
                            if (tempsAnswer == -1) tempsAnswer = EvoGlobals.Uncertainty;
                            validationPop[i].Matrix.RecordEvent(rightAnswer, tempsAnswer);
                        }
                    }
                }
                double factor = (1 - temp.Complexity * EvoGlobals.Alpha) / (dates*dataTable.Count + 1);
                MultidimensionalConfusionMatrix cm = hunterPop[i].Matrix;
               if (temp.Fitness < 0) temp.Fitness = .1;
                temp.Fitness *= factor;
 
                factor = cm.MatthewsCorrelationCoefficient / 10;

                temp.Fitness += factor;
                if (temp.Fitness < 0) temp.Fitness = 1e-7;
            }
           
              //  for (int i = 0; i < HunterPopSize; ++i) hunterPop[i].TheT.Fitness -= BaselineFitness;//Bring lowest fitness up to 0
 
        }



        private void PreyFitness()
        {
            /* this should look something like:
             * for each testCase in testCases:
             *      for each prey in population:
             *          for each hunter in population:
             *              hunter evaluates test case masked by prey
             *  However, this means there are a LOT of calculations every generation, 50 Hunters * 20 prey * 36 test cases = 36000 evaluations by potentially 2000 functions at a time...
             *  We'll worry about it when we get there.  Making up a table for every function shouldn't be too hard.
             *  */

            for (int z = 0; z < PreyPopSize; ++z)
            {
                Prey x = preyPop[z].TheT;
                EvoAlgFunctionSuite.applyToEquipment(x);
                foreach (Equipment data in dataTable)
                {
                    string key = extractKeyFromEquipment(data);
                    if (answerExistsInDB(key) == false) continue;//Skip to next equipment
                    Dictionary<string, int> dict = answers.getDatesForEquip(key);
                    foreach (KeyValuePair<string, int> p in dict)
                    {
                        double ret;
                        

                        EvoAlgFunctionSuite.ResetLookupTable();
                        for (int i = 0; i < HunterPopSize; ++i)
                        {
                            Hunter temp = hunterPop[i].TheT;
                            int tempsAnswer = -1;
                            DateTime cutoff = answers.ExtractDateFromString(p.Key);
                            int ans;
                            bool rightAnswer;
                            KeyValuePair<int,int> classAndIntensity =this.getRightAnswer(data, cutoff);
                            ans = classAndIntensity.Value;
                            BitArray classBits = RightAnswerGetter.convertClassToBits(classAndIntensity.Key);
                            if (ans == -1) continue;
                            if (ans <= 3) rightAnswer = true;
                            else rightAnswer = false;
                            tempsAnswer = temp.Vote(data, out ret, cutoff);
                            if (ret == -1)
                                temp.Fitness -= 1.5;
                           /* hunterPop[i].Matrix.RecordEvent(MultidimensionalConfusionMatrix.EventType(rightAnswer, tempsAnswer));
                            if (tempsAnswer == null)
                            {
                                x.Fitness += .5;
                                temp.Fitness += .2;
                            }
                            else if (tempsAnswer == rightAnswer && rightAnswer)
                            {
                                temp.Fitness += 1;
                                x.Fitness -= .5;
                            }
                            else if (tempsAnswer != rightAnswer) x.Fitness += 1;*/
                        }
                    }
                    //EvoAlgFunctionSuite.revertList();
                }

            }
            for (int i = 0; i < HunterPopSize; ++i)
            {
                Hunter temp = hunterPop[i].TheT;
                double factor = (1 - temp.Complexity * EvoGlobals.Alpha) / (PreyPopSize * (dataTable.Count + 1));
                ConfusionMatrix cm = hunterPop[i].Matrix;
                if (cm.MatthewsCorrelationCoefficient <= 0 || temp.Fitness < 0) temp.Fitness = 0.1;
                temp.Fitness *= factor;
                if (i < PreyPopSize)
                {
                    Prey temp2 = preyPop[i].TheT;
                    factor = 1F / HunterPopSize * dataTable.Count;
                    temp2.Fitness *= factor;
                }
            }
        }

        private bool answerExistsInDB(string key)
        {
            return answers.CheckForKey(key);
        }
        private KeyValuePair<int, int> getRightAnswer(Equipment data, DateTime cutoff)
        {
            //First, we need to get the string into the right format: Group: ID - Desc 
            string key = extractKeyFromEquipment(data);
            IItem temp = data.Parent as IItem;

            if (answerExistsInDB(key))
            {

                return answers.GetRightAnswer(key, cutoff);
                
            }

            return new KeyValuePair<int,int>(-1,-1);
        }

        private KeyValuePair<int, int> getRightAnswer(int id)
        {
            //First, we need to get the string into the right format: Group: ID - Desc 
                return answers.GetRightAnswer(id);
        }


        private static string extractKeyFromEquipment(Equipment data)
        {
            string ID = data.Id;
            string desc = data.Desc;
            string group = data.Parent.Desc;
      
      
            string key = string.Format("{0}:{1} - {2}", group, ID, desc);
            return key;
        }

        bool testCasesPopulated = false;
        public bool PopulateTestCases(Database data)
        {
            if (testCasesPopulated) return true;
            ArrayList level1 = data.GetChildren(null);
            ArrayList listOfLists = new ArrayList();
            children = new List<List<IItem>>();
            foreach (IItem x in level1)
            {
                if (x.HasChildren == ChildFlags.Yes)
                {
                    listOfLists.Add(x.GetChildren(null));
                }

            }

            foreach (ArrayList x in listOfLists)
            {
                for (int i = 0; i < x.Count; ++i)
                {
                    Equipment temp = x[i] as Equipment;
                    level1 = null;
                    if (temp == null) continue;
                    dataTable.Add(temp);
                    if (temp.HasChildren == ChildFlags.Yes)
                    {
                        try
                        {
                            while (level1 == null)
                                level1 = temp.GetChildren(null);
                        }
                        catch { }
                        for (int j = 0; j < level1.Count; ++j)
                        {
                            children.Add(new List<IItem>());
                            IItem bort = level1[j] as IItem;
                            if (bort == null) continue;
                            if (i < children.Count) children[i].Add(bort);
                            else children[children.Count - 1].Add(bort);
                        }
                    }
                }
            }
            if (dataTable.Count == 0) return false;
            EvoAlgFunctionSuite.populateDataTable(dataTable);
            EvoAlgFunctionSuite.InitLookupTable();
            return testCasesPopulated = true;
        }


        public string enitreHistoryAsString()
        {
            StringBuilder ret  = new StringBuilder(String.Format("History:{0}", Environment.NewLine));
            for (int i = gen < maxHistory? gen: gen-500; i < gen; ++i)
            {
                ret.AppendFormat("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}{8}", i, History[i][historyHMC], History[i][historyAMC], History[i][historyLMC], History[i][historyHC],
                    History[i][historyAC], History[i][historyPF], History[i][historyHHF], Environment.NewLine);

            }
            return ret.ToString();
        }
        public string HistoryAsString
        {
            get
            {
                int i = History.Count-1;
                string ret = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}{8}", History[i][historyGen], History[i][historyHMC], History[i][historyAMC], History[i][historyLMC], History[i][historyHC],
                    History[i][historyAC], History[i][historyPF], History[i][historyHHF], Environment.NewLine);
                return ret;
            }
        }

        void IDisposable.Dispose()
        {
            historyLog.Dispose();
        }

        public string humanReadableHunters()
        {
            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < HunterPopSize; ++i)
            {
                ConfusionDouble<Hunter> x = hunterPop[i];

                Hunter temp = x.TheT as Hunter;

                ret.AppendLine(temp.HumanReadableHunter);
                ret.AppendLine(x.Matrix.Serialize());

            }
            ret.AppendLine("Bleachers:");
            for (int i = 0; i < bleachers.Count; ++i)
            {
                ConfusionDouble<Hunter> x = bleachers[i];

                Hunter temp = x.TheT as Hunter;

                ret.AppendLine("Bleachers " + i + ":");
                    ret.AppendLine(temp.HumanReadableHunter);
                ret.AppendLine(x.Matrix.Serialize());

            }
            return ret.ToString();
        }

        internal void EnumerateFeatures()
        {
            Dictionary <string, Dictionary <string, int>> nameToID = RightAnswerGetter.getNameToID();
            Dictionary<int, KeyValuePair<int, int>> idToAnswers = RightAnswerGetter.getTheData();
            foreach (KeyValuePair<string, Dictionary<string, int>> pair in nameToID){
                Dictionary<string, int> answers = pair.Value;
                string firstKey = pair.Key;
                int classBitLength = Chromasome.ClassBitLength;
                using (StreamWriter f = new StreamWriter("Classes.csv", true))
                {
                    foreach (KeyValuePair<string, int> p in answers)
                    {

                        StringBuilder line = new StringBuilder((firstKey.Contains(',')?  firstKey.Replace(',', ' ') + "," + p.Key:firstKey + "," + p.Key), 10);
                        line.Append(",");
                        int Classes = 0;
                        KeyValuePair<int, int> classIntensityPair = idToAnswers[p.Value];

                        BitArray classBits = RightAnswerGetter.convertClassToBits(classIntensityPair.Value);
                     
                            
                        foreach (bool x in classBits) if (x) ++Classes;
                        if (classBits[0]) Debug.Assert(Classes == 1); 
                        if (classBits[0]) 
                            line.Append(Chromasome.classStrings[0]);
                        else
                        {

                            for (int i = 0; i < classBitLength; ++i)
                            {
                                if (classBits[i])
                                {
                                    Debug.Assert(i != 0);
                                    line.Append(Chromasome.classStrings[i]);
                                    if (--Classes > 0) line.Append("| ");
                                }
                            }
                        }
                        line.Append(",");
                        String IntensityCode;
                        switch (classIntensityPair.Key)
                        {
                            case -1:
                                IntensityCode = " NT";
                                break;
                            case 0:
                                IntensityCode = " OK";
                                break;
                            default:
                                IntensityCode = " " + classIntensityPair.Key;
                                break;
                        }

                        line.Append("Intensity:" + IntensityCode);
                        f.WriteLine(line.ToString());
                    }

                }
            
            }
        }

        internal void writeHistory(int from, int to, StreamWriter e)
        {

                e.Write("Gen,");
                for (int i = from; i < to; ++i)
                {
                    e.Write(i + ", ");
                }
                e.WriteLine();
            //Record highest MCC, Accuracy, FN Rate and FP Rate for both validation and non-validation data
                e.Write("MCC Regular,");
                dumpOutputLine(e, 0, from, to);

                e.Write("MCC Validation,");
                dumpOutputLine(e, 1, from, to);
                
                e.Write("Accuracy Regular,");
                dumpOutputLine(e, 2, from, to); 
                e.Write("Accuracy Validation,");
                dumpOutputLine(e, 3,from, to);
                e.Write("FPR Regular,");
                dumpOutputLine(e, 4,from, to);
                e.Write("FPR Validation,");
                dumpOutputLine(e, 5,from, to);
                e.Write("FNR Regular,");
                dumpOutputLine(e, 6, from, to);
                e.Write("FNR Validation,");
                dumpOutputLine(e, 7, from, to);
                e.Flush();
            
        }

        private void dumpOutputLine(StreamWriter e, int j, int from, int to)
        {
            for (int i = from; i < to; ++i)
            {
                e.Write(output[i][j] + ",");
            }
            e.WriteLine();
        }
    }
        #endregion
}
