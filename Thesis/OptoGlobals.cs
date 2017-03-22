using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using MLApp;
using MyUtils;
namespace EvoOptimization
{
    public class OptoGlobals
    {
        public enum CrossoverModes { Uniform, SinglePointHunter, TwoPointHunter, RandomHunter, TwoPointChromosome, SinglePointChromosome, RandomChromosome };
        private static int _seed = (int)DateTime.Now.Ticks;
        public static int GetSeed { get { return _seed; } }
        public const int Means = 0, StdDevs = 1, Mins = 2, Maxes = 3, StatSize = 4;
        static String trXPath, trYPath, teXPath, teYPath, classNamesPath, datasetName;
        public static String DataSetName { get { return datasetName; } }
        static HashSet<int> xIgnore, yIgnore, xCols, yCols;
        public static HashSet<int> CategoricalColumns, NumericalColumns, BooleanColumns; 

        static bool xBlacklist, yBlacklist;
        internal static void ConfigureForDataset(string globalPath)
        {
            bool catBlackList = false, boolBlackList = false;
            Console.WriteLine(Path.GetFullPath(globalPath));
            globalPath = Path.GetFullPath(globalPath);
            using (StreamReader fin = new StreamReader(new BufferedStream(new FileStream(globalPath, FileMode.Open))))
            {
                datasetName = GetNextNonCommentedLine(fin).Trim();

                classNamesPath = GetNextNonCommentedLine(fin).Trim();
                trXPath = GetNextNonCommentedLine(fin).Trim();
                trYPath = GetNextNonCommentedLine(fin).Trim();
                teXPath = GetNextNonCommentedLine(fin).Trim();
                teYPath = GetNextNonCommentedLine(fin).Trim();
                CategoricalColumns = new HashSet<int>();
                BooleanColumns = new HashSet<int>();
                GenerateIgnoreList(GetNextNonCommentedLine(fin).Trim(), ref xIgnore, ref xBlacklist);
                GenerateIgnoreList(GetNextNonCommentedLine(fin).Trim(), ref yIgnore, ref yBlacklist);
                GenerateIgnoreList(GetNextNonCommentedLine(fin).Trim(), ref CategoricalColumns, ref catBlackList);
                GenerateIgnoreList(GetNextNonCommentedLine(fin).Trim(), ref BooleanColumns, ref boolBlackList);

                //TODO:
                //What needs to be in the file?
                //Class definition filepath
                //location of training, testing sets (these give us the number of features etc., almost- need to know which columns to ignore
                //So there should be 2 lists- X ignore Columns, Y ignore Columns (could be for the same file, for that matter)
                //Also, for x ignore and y ignore, an option specifying whether the ignore list is actually an include list (shorter for Y if in master file)
                //
                //Format: one variable per line, except for ignore columns.  So it should go like this:
                ///ignore all lines beginning with #
                ///Dataset Name
                ///Class Names File
                ///DaedalusTrainingSet X Path
                ///DaedalusTrainingSet Y Path
                ///TestingSet  X Path
                ///TestingSet  Y Path
                ///X ignore list, comma separated and starting with w if it's a whitelist (otherwise, blacklist)
                ///Y ignore list, as above
                ///After that, we should be able to refer to the variables generated to do the work.

                ///Now, load the datasets:


            }
            int len;
            using (StreamReader fin = new StreamReader(new BufferedStream(new FileStream(trXPath, FileMode.Open))))
            {
                char[] tokens = { ',' };
                string firstLine = fin.ReadLine();
                string[] headers = firstLine.Split(tokens, StringSplitOptions.RemoveEmptyEntries);
                len = headers.Length;
            }

            getColumnList(catBlackList, CategoricalColumns, out CategoricalColumns, len);
            getColumnList(boolBlackList, BooleanColumns, out BooleanColumns, len);
            Object[] tempX = readInDataset(ref xCols, ref xIgnore, xBlacklist, ref BooleanColumns, CategoricalColumns, trXPath, catBlackList, boolBlackList, true, false) as Object[];
            TrainingXRaw = tempX[0] as List<List<Double>>;
            TrainingXBools = tempX[1] as List<List<Boolean>>;
            TrainingXCats = tempX[2] as List<List<String>>;
            NumberOfFeatures = xCols.Count;
            TrainingYRaw = readInDataset(ref yCols, ref yIgnore, yBlacklist, ref BooleanColumns, CategoricalColumns, trYPath, catBlackList, boolBlackList, false, false) as List<List<String>>;
            TrainingYString = Util.TwoDimListToSmoothArray(TrainingYRaw);
            tempX = readInDataset(ref xCols, ref xIgnore, xBlacklist, ref BooleanColumns, CategoricalColumns, trXPath, catBlackList, boolBlackList, true, true) as Object[];
            TestingXRaw = tempX[0] as List<List<Double>>;
            TestingXBools = tempX[1] as List<List<Boolean>>;
            TestingXCats = tempX[2] as List<List<String>>;

            TestingYRaw = readInDataset(ref yCols, ref yIgnore, yBlacklist, ref BooleanColumns, CategoricalColumns, trYPath, catBlackList, boolBlackList, false, true) as List<List<String>>;
            TestingYString = Util.TwoDimListToSmoothArray(TestingYRaw);


            TrainingXNormed = NormalizeArray(TrainingXRaw, SqueezedMinMaxNorm, true);
            TestingXNormed = NormalizeArray(TestingXRaw, SqueezedMinMaxNorm, false);

            Dictionary<String, List<Double>> meanSumDict = new Dictionary<string, List<double>>();
            gatherCategoricalMeans(TrainingXNormed, TrainingXCats, meanSumDict);
            gatherCategoricalMeans(TestingXNormed, TestingXCats, meanSumDict);
            meanSumsToCatValues(meanSumDict);
            DaedalusTrainingSet = OptoGlobals.setFromCollection(OptoGlobals.TrainingXNormed, OptoGlobals.TrainingXCats, OptoGlobals.TrainingXBools);

            DaedalusValidationSet = OptoGlobals.setFromCollection(OptoGlobals.TestingXNormed, OptoGlobals.TestingXCats, OptoGlobals.TestingXBools);


            OptoGlobals.TrainingXNormed = Util.ListArrayToListList(DaedalusTrainingSet);
            OptoGlobals.TestingXNormed = Util.ListArrayToListList(DaedalusValidationSet);

            ClassDict = new Dictionary<string, int>();
            ClassList = new List<string>();
            int tempCl = 0;

            //ClassDict is a translator to convert string classes to integers.  ClassList is a list to do the same thing with integers.
            //ClassList[ClassDict["className"]] is will yield "className", if it is in the dictionary.
            //Datasets are loaded... what's next?  
            tempCl = buildClassListAndDict(tempCl, TrainingYRaw);//If Training and Testing sets are configured correctly, the next line is pointless.
            buildClassListAndDict(tempCl, TestingYRaw);


            NumberOfClasses = ClassList.Count;

            NumericalColumns = xCols.SetDifference(CategoricalColumns);
            NumericalColumns = NumericalColumns.SetDifference(BooleanColumns);


            testingYIntArray = intArrayFromStringList(TestingYRaw);
            trainingYIntArray = intArrayFromStringList(TrainingYRaw);


            DaedalusTrainingY = new List<int>(MyUtils.Util.Flatten2dArray(OptoGlobals.trainingYIntArray));
            DaedalusValidationY = new List<int>(MyUtils.Util.Flatten2dArray(OptoGlobals.testingYIntArray));
           

            AllPredictorNames = GetPredictorNames(xCols, trXPath);
            if (TrainingXRaw == null || TestingXRaw == null || TrainingYRaw == null || TestingYRaw == null)
            {
                Console.WriteLine("Something went horribly wrong loading data, one or more of the datasets is null.  Could be a bad path.");
                throw new InvalidCastException();
            }
            }

        internal delegate List<Double> NormFunction(List<Double> x, List<List<Double>> stats);

        public static List<Double> SqueezedMinMaxNorm(List<Double> x, List<List<Double>> stats)
        {
            const double bottom= .1, top = .9;
            List<Double> ret = new List<double>(x.Count);
            for (int i = 0; i < x.Count; ++i)
            {
                double temp = (bottom + (top-bottom)*((x[i] - stats[i][Mins]) / (stats[i][Maxes] - stats[i][Mins])));
                ret.Add(double.IsNaN(temp) ? bottom : temp);
            }
            return ret;
        }

        public static List<Double> MinMaxNorm(List<Double> x, List<List<Double>> stats)
        {
            List<Double> ret = new List<double>(x.Count);
            for (int i = 0; i < x.Count; ++i)
            {
                ret.Add((x[i] - stats[i][Mins]) / (stats[i][Maxes] - stats[i][Mins]));
            }
            return ret;
        }

        public static List<Double> StdDevNorm(List<Double> x, List<List<Double>> stats)
        {
            List<Double> ret = new List<double>(x.Count);
            for (int i = 0; i < x.Count; ++i)
            {
                ret.Add((x[i] - stats[i][Means]) / stats[i][StdDevs]);
            }
            return ret;
            }

        
        internal static List<List<double>> NormalizeArray(List<List<double>> inArray, NormFunction func, bool DaedalusTrainingSet = true)
        {
            if (func == null) func = MinMaxNorm;
            List<List<Double>> ret = new List<List<double>>(inArray.Count);
            if (DaedalusTrainingSet || Stats == null)///get the stats from the array, and use it for all resulting data (eg, get stats from training set and then apply it to testing set)
            {
                Stats = new List<List<double>>();
                for (int i = 0; i < inArray[0].Count; ++i)
                {
                    double[] column = Util.Flatten2dArray(pullColumnFromArray(inArray, i));
                    Stats.Add(GetStats(column));
                }

            }
            else
            {
                //Testing set- get min and max from testing set
                for (int i = 0; i < inArray[0].Count; ++i)
                {
                    double[] column = Util.Flatten2dArray(pullColumnFromArray(inArray, i));
                    List<Double> temp = GetStats(column);
                    Stats[i][Mins] = Math.Min(temp[Mins], Stats[i][Mins]);
                    Stats[i][Maxes] = Math.Max(temp[Maxes], Stats[i][Maxes]);
                }
            }
            
            foreach(List<Double> l in inArray)
            {
                ret.Add(func(l, Stats));
            }

            return ret;
        }

        public static List<double> GetStats(double[] col)
        {
            
            List<Double> ret = new List<double>(StatSize);
            for (int i = 0; i < StatSize; ++i) ret.Add(0);
            ret[Mins] = col.Min();
            ret[Maxes] = col.Max();
            ret[Means] = col.AverageIgnoringNAN();
            ret[StdDevs] = col.StandardDeviationIgnoringNAN();
            return ret;
            
        }

        private static int[,] intArrayFromStringList(List<List<String>> inList, int col = 0)
        {
            int[,] ret = new int[inList.Count, 1];
            for (int i = 0; i < inList.Count; ++i)
            {
                ret[i, 0] = ClassDict[inList[i][col]];
            }
            return ret;
        }

        /// <summary>
        /// Reads a dataset according to params.  Returns a boxed List<list type="List<T>"></list>
        /// </summary>
        /// <param name="cols">Which zero-indexed columns we're pulling in from the file (we'll define this is ignoreFirstLine is false).</param>
        /// <param name="ignoreList">Which columns should be ignored or included</param>
        /// <param name="blackList">Boolean based on whether ignoreList is a black- or white- list</param>
        /// <param name="filePath">file To read</param>
        /// <param name="isXDataset">If an X dataset, will parse data to doubles before returning.  Otherwise, will return as strings</param>
        /// <param name="ignoreFirstLine"></param>
        /// <returns>boxed 2d list, either strings or doubles</returns>
        private static Object readInDataset(ref HashSet<int> cols, ref HashSet<int> ignoreList, bool blackList, ref HashSet<int> BoolCols, HashSet<int> CatCols, string filePath, bool catBlacklist, bool boolBlacklist, bool isXDataset, bool ignoreFirstLine)
        {
            using (StreamReader fin = new StreamReader(new BufferedStream(new FileStream(filePath, FileMode.Open))))
            {
                String firstLine = fin.ReadLine();//Column headers
                char[] tokens = { ',' };
                if (!ignoreFirstLine)
                {
                    string[] headers = firstLine.Split(tokens, StringSplitOptions.RemoveEmptyEntries);
                    getColumnList(blackList, ignoreList, out cols, headers.Length);


                }
                if (isXDataset)
                {
                    //return value is List<List<Double>>
                    List<List<Boolean>> bools = new List<List<Boolean>>();
                    List<List<String>> cats = new List<List<string>>();
                    List<List<Double>> ret = new List<List<Double>>();
                    while (!fin.EndOfStream)
                    {
                        List<String> tempCats = new List<string>();
                        List<Double> temp = new List<double>();
                        List<Boolean> tempBools = new List<Boolean>();
                        String line = fin.ReadLine();
                        string[] data = line.Split(tokens, StringSplitOptions.RemoveEmptyEntries);
                        foreach (int i in cols)
                        {
                            double nom;
                            
                            if(!CatCols.Contains(i) && ! BoolCols.Contains(i))
                            {
                                if (Double.TryParse(data[i].Trim(), out nom))
                                {
                                temp.Add(nom);
                                }
                                else{
                                    
                                Console.WriteLine("Error parsing " + data[i].Trim() + " expected a double, got something else, adding NaN to list");
                                temp.Add(Double.NaN);
                                }
                            }
                            else if (CatCols.Contains(i))
                                {
                                    tempCats.Add(data[i].Trim());
                                }
                            else if(BoolCols.Contains(i))
                                {
                                    bool foo;
                                    if (Boolean.TryParse(data[i].Trim(), out foo))
                                    {
                                        tempBools.Add(foo);
                                    }
                                    else
                                    {
                                        short foo2;
                                        if(Int16.TryParse(data[i].Trim(), out foo2)){
                                            tempBools.Add(foo2 != 0);
                                        }
                                    }
                                }
                        
                            }
                        
                        ret.Add(temp);
                        bools.Add(tempBools);
                        cats.Add(tempCats);
                    }
                    object[] retArray = { ret, bools, cats };
                    return retArray as Object;

                }
                else {//Y set
                    List<List<String>> ret = new List<List<String>>();

                    while (!fin.EndOfStream)
                    {
                        List<String> temp = new List<String>();
                        String line = fin.ReadLine();
                        string[] data = line.Split(tokens, StringSplitOptions.RemoveEmptyEntries);
                        foreach (int i in cols)
                        {
                            temp.Add(data[i].Trim());
                        }
                        ret.Add(temp);
                    }
                    return ret as Object;

                }
            }
            Console.WriteLine("Something went horribly wrong in readInDataset... returning null");
            return null;
        }
        

        /// <summary>
        /// Take args and set variables appropriately
        /// </summary>
        /// <param name="blackList">whether the ignore list is a black list or a white list</param>
        /// <param name="ignoreList">set of unique indices which should be black or white listed</param>
        /// <param name="set">the set into which the values should be white- or black- listed</param>
        /// <param name="headerLength">max of 0 indexed range in case of black list</param>
        private static void getColumnList(bool blackList, HashSet<int> ignoreList, out HashSet<int> set, int headerLength)
        {
            if (blackList)
            {
                HashSet<int> temp = new HashSet<int>();
                for (int i = 0; i < headerLength; ++i)
                {
                    temp.Add(i);//Start with all columns
                }
                set = temp.SetDifference(ignoreList);//Subtract ignored columns
            }
            else
            {
                set = new HashSet<int>(ignoreList);
            }
        }

        private static void GenerateIgnoreList(string v, ref HashSet<int> ignoreList, ref bool blackList)
        {
            char[] sep = { ',' };
            String [] Tokens = v.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            blackList = Tokens[0].Trim().Equals("b", StringComparison.CurrentCultureIgnoreCase);
            ignoreList = new HashSet<int>();
            for(int i= 1; i < Tokens.Length; ++i)
            {
                int res;
                if (Int32.TryParse(Tokens[i].Trim(), out res))
                {
                    ignoreList.Add(res);
                }
                else
                {
                    Console.WriteLine("Error in ignore list, failed to parse " + Tokens[i].Trim() + "into an integer.");
                }
                
            }

        }

        private static string GetNextNonCommentedLine(StreamReader fin)
        {
            String ret = fin.ReadLine();
            while (ret.StartsWith("#"))
            {
                ret = fin.ReadLine();
            }
            return ret;
        }
        public static String EnvironmentTag { get; private set; }
        public static double ComplexityCap { get; internal set; }
        public static CrossoverModes CrossoverMode { get; internal set; }

        public static Boolean IsDebugMode = false;
        public static int NumberOfClasses;
        public static Random RNG;
        public static int NumberOfFeatures;
        public static double CrossoverChance = .25, ElitismPercent = .20,
            InitialRateOfOnes = .5, MutationChance = .01, MergerPercent = .05;

        private const int dataDemarcation = 6;
        public static bool UseMWArrayInterface = false;

        public static Dictionary<String, int> ClassDict;
        public static List<String> ClassList, DataHeaders, yHeaders, AllPredictorNames;
        public static List<List<Double>> TrainingXRaw, TestingXRaw, TrainingXNormed, TestingXNormed, Stats;
        public static List<List<String>> TrainingYRaw, TestingYRaw, TrainingXCats, TestingXCats;
        public static List<List<Boolean>> TestingXBools, TrainingXBools;
        public static bool[,] TrainingYRawLogical, TestingYRawLogical;
        public static double[,] TrX, TeX;
        public static String[,] TrainingYString, TestingYString;
        public static MLApp.MLApp executor = new MLApp.MLApp();
        private static Process ExecutorProcess;
        internal static int[,] testingYIntArray, trainingYIntArray;
        public static Dictionary<String, Double> CategoryValues;
        internal static List<double[]> setFromCollection(List<List<double>> set, List<List<String>> catSet, 
            List<List<Boolean>> boolSet)
        {
            List<List<Double>> ret = new List<List<Double>>(set.Count);
            
            for (int i = 0; i < set.Count; ++i)
            {
                List<Double> x = set[i];
                for (int j = 0; j < catSet[i].Count; ++j)
                {
                    x.Add(CategoryValues[catSet[i][j]]);//Append normed Cats and Bools to the end here and below
                }
                for (int j = 0; j < boolSet[i].Count; ++j)
                {
                    x.Add(boolSet[i][j] ? OptoGlobals.FalseDoubleVal : OptoGlobals.TrueDoubleVal);
                }

                ret.Add(x);
            }
            ret = OptoGlobals.NormalizeArray(ret, OptoGlobals.SqueezedMinMaxNorm, true);
            List<Double[]> realRet = new List<Double[]>();
            foreach (List<Double> r in ret)
            {
                realRet.Add(r.ToArray());
            }
            return realRet;

        }

        private static void meanSumsToCatValues(Dictionary<String, List<Double>> meanSumDict)
        {

            CategoryValues = new Dictionary<string, double>();
            foreach (String key in meanSumDict.Keys)
            {
                double val = meanSumDict[key][0] / meanSumDict[key][1];
                if (double.IsNaN(val)) val = OptoGlobals.RNG.NextDouble()*.8+.1;
                CategoryValues.Add(key, val);//Now, we have means for every categorical
            }
        }

        private static void gatherCategoricalMeans(List<List<double>> set, List<List<String>> catSet, Dictionary<String, List<Double>> meanSumDict)
        {
            for (int i = 0; i < set.Count; ++i)
            {
                List<Double> x = set[i];
                for (int j = 0; j < catSet[i].Count; ++j)
                {
                    if (!meanSumDict.ContainsKey(catSet[i][j]))
                    {
                        double[] empty = { 0, 0 };
                        meanSumDict.Add(catSet[i][j], new List<double>(empty));
                    }
                    List<Double> sc = meanSumDict[catSet[i][j]];
                    foreach (double d in x)
                    {
                        sc[0] += d;
                        sc[1] += 1;
                    }
                }
            }//We have now calculated sums and counts for every categorical
        }


        static OptoGlobals()
        {
            ComplexityCap = 2000;
#if DEBUG
            IsDebugMode = true;
#endif
            ///This whole thing needs to be moved around
            Console.WriteLine("Entering OptoGlobals STATIC constructor");
            RNG = new Random(_seed);




            String MATLABpath = @"./../../../Matlab Scripts/";
            MATLABpath = Path.GetFullPath(MATLABpath);

            assignExecutorProcess();


            Console.WriteLine(executor.Execute("cd " + "'" + MATLABpath + "'"));
            Console.WriteLine(executor.ToString());


            Console.WriteLine("Leaving Static Constructor");
        }

        private static int buildClassListAndDict(int tempCl, List<List<string>> listlist)
        {
            foreach (List<String> list in listlist)
            {
                foreach (String cat in list)
                {
                    if (ClassDict.ContainsKey(cat)) continue;
                    ClassDict.Add(cat, tempCl++);
                    ClassList.Add(cat);
                }
            }
           
            return tempCl;
        }

        private static void assignExecutorProcess()
        {
            Process[] procs = Process.GetProcesses();
            foreach (Process x in Process.GetProcesses())
            {
                if (x.MainWindowTitle.Equals("MATLAB Command Window"))
                {
                    ExecutorProcess = x;
                    break;
                }
            }
        }






        private static T[,] pullColumnFromArray<T>(List<List<T>> array, int column)
        {
            T[,] ret = new T[1, array.Count];
            for (int i = 0; i < array.Count; ++i)
            {
                ret[0, i] = array[i][column];
            }
            return ret;
        }

        private static List<String> GetPredictorNames(HashSet<int> columns, string FilePath)
        {
            List<String> ret = new List<string>();
            string[] headers;
                char[] tokens = { ',' };
            string line;
            using (StreamReader fin = new StreamReader(new FileStream(FilePath, FileMode.Open)))
            {
                line = fin.ReadLine();
                headers = line.Split(tokens,StringSplitOptions.RemoveEmptyEntries );
            }

            foreach (int i in columns) {
                    ret.Add(headers[i].Trim());
            }
            return ret;
        }

        

        private static List<List<string>> loadLabels(string p)
        {
            List<List<String>> ret = new List<List<String>>();
            char[] token = { ',' };

            using (StreamReader fin = new StreamReader(new BufferedStream(new FileStream(p, FileMode.Open))))
            {
                String line = fin.ReadLine();
                if (yHeaders == null) yHeaders = extractHeadersFromFirstLine(line);
                while (!fin.EndOfStream)
                {
                    line = fin.ReadLine();
                    List<String> temp = new List<string>();

                    String[] dataArray = line.Split(token);
                    for (int i = 0; i < dataArray.Length; ++i)
                    {
                        temp.Add(dataArray[i]);
                    }
                    ret.Add(temp);
                }

            }
            return ret;



        }

        private static List<List<double>> loadData(string p, bool getHeaders, out List<List<String>> outLabels)
        {
            List<List<Double>> ret = new List<List<double>>();
            outLabels = new List<List<string>>();
            char[] token = { ',' };
            using (StreamReader fin = new StreamReader(new BufferedStream(new FileStream(p, FileMode.Open))))
            {
                String line = fin.ReadLine();
                if (getHeaders) DataHeaders = extractHeadersFromFirstLine(line);

                while (!fin.EndOfStream)
                {

                    line = fin.ReadLine();
                    String[] stringArray = line.Split(token);
                    List<String> dataIdentifyingInfo = new List<string>();
                    List<Double> dataRow = new List<double>();

                    for (int i = 0; i < dataDemarcation; ++i)
                    {
                        dataIdentifyingInfo.Add(stringArray[i]);
                    }
                    outLabels.Add(dataIdentifyingInfo);
                    for (int i = dataDemarcation; i < stringArray.Length; ++i)
                    {
                        String temp = stringArray[i];
                        try
                        {
                            dataRow.Add(Double.Parse(temp));
                        }
                        catch (FormatException ignore)
                        {
                            dataRow.Add(double.NaN);

                        }
                    }
                    if (ret.Count > 1) Debug.Assert(ret[ret.Count - 1].Count == dataRow.Count);
                    ret.Add(dataRow);
                }

            }

            return ret;
        }

        private static List<string> extractHeadersFromFirstLine(String line)
        {
            char[] token = { ',' };
            return new List<String>(line.Split(token));

        }


        internal static void Initialize()
        {
            RNG = new Random(_seed);
        }


        internal static void LoadTagFromFile(string compTagFile)
        {
            using (StreamReader fin = new StreamReader(compTagFile))
            {
                if (!fin.EndOfStream)
                {
                    EnvironmentTag = fin.ReadLine().Trim();
                }
                else EnvironmentTag = "NoTag"; 
            }
        }

        public static void CreateDirectoryAndThenFile(string path)
        {
            char[] tokens = { '/', '\\' };
            string temp = path.Substring(0, path.LastIndexOfAny(tokens));
            if (!Directory.Exists(temp)) Directory.CreateDirectory(temp);
            File.Create(path).Close();

        }





        public const double FalseDoubleVal = .25;

        public const double TrueDoubleVal = .75;
        public static List<double[]> DaedalusTrainingSet, DaedalusValidationSet;
        public static List<int> DaedalusTrainingY, DaedalusValidationY;

    }
}
