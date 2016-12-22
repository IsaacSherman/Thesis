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
        public enum CrossoverModes { Uniform, SinglePointChromasome, TwoPointChromasome, SinglePointHunter, TwoPointHunter, RandomHunter, RandomChromasome };
        private static int _seed = (int)DateTime.Now.Ticks;
        public static int GetSeed { get { return _seed; } }
        static String trXPath, trYPath, teXPath, teYPath, classNamesPath, datasetName;
        static HashSet<int> xIgnore, yIgnore, xCols, yCols;
        static bool xBlacklist, yBlacklist;
        internal static void ConfigureForDataset(string globalPath)
        {

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
                GenerateIgnoreList(GetNextNonCommentedLine(fin).Trim(), ref xIgnore, ref xBlacklist);
                GenerateIgnoreList(GetNextNonCommentedLine(fin).Trim(), ref yIgnore, ref yBlacklist);

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
                ///TrainingSet X Path
                ///TrainingSet Y Path
                ///TestingSet  X Path
                ///TestingSet  Y Path
                ///X ignore list, comma separated and starting with w if it's a whitelist (otherwise, blacklist)
                ///Y ignore list, as above
                ///After that, we should be able to refer to the variables generated to do the work.

                ///Now, load the datasets:


            }
            TrainingXRaw = readInDataset(ref xCols, ref xIgnore, xBlacklist, trXPath, true, false) as List<List<Double>>;
            NumberOfFeatures = xCols.Count;
            TrainingYRaw = readInDataset(ref yCols, ref yIgnore, yBlacklist, trYPath, false, false) as List<List<String>>;
            TrainingYString = Util.TwoDimListToSmoothArray(TrainingYRaw);
            TestingXRaw = readInDataset(ref xCols, ref xIgnore, xBlacklist, trXPath, true, true) as List<List<Double>>;
            TestingYRaw = readInDataset(ref yCols, ref yIgnore, yBlacklist, trYPath, false, true) as List<List<String>>;
            TestingYString = Util.TwoDimListToSmoothArray(TestingYRaw);

            AllPredictorNames = GetPredictorNames(xCols, trXPath);
            if (TrainingXRaw == null || TestingXRaw == null || TrainingYRaw == null || TestingYRaw == null)
            {
                Console.WriteLine("Something went horribly wrong loading data, one or more of the datasets is null.  Could be a bad path.");
                throw new InvalidCastException();
            }
            int tempCl = 0;
            NumberOfFeatures = TrainingXRaw[0].Count;
            ClassDict = new Dictionary<string, int>();
            ClassList = new List<string>();
            tempCl = buildClassListAndDict(tempCl, TrainingYRaw);//If Training and Testing sets are configured correctly, the next line is pointless.
            buildClassListAndDict(tempCl, TestingYRaw);

            testingYIntArray =  intArrayFromStringList(TestingYRaw);

            trainingYIntArray = intArrayFromStringList(TrainingYRaw);

            NumberOfClasses = ClassList.Count;

            //ClassDict is a translator to convert string classes to integers.  ClassList is a list to do the same thing with integers.
            //ClassList[ClassDict["className"]] is will yield "className", if it is in the dictionary.
            //Datasets are loaded... what's next?  
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
        private static Object readInDataset(ref HashSet<int> cols, ref HashSet<int> ignoreList, bool blackList, string filePath, bool isXDataset, bool ignoreFirstLine)
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
                    List<List<Double>> ret = new List<List<Double>>();
                    while (!fin.EndOfStream)
                    {
                        List<Double> temp = new List<double>();
                        String line = fin.ReadLine();
                        string[] data = line.Split(tokens, StringSplitOptions.RemoveEmptyEntries);
                        foreach (int i in cols)
                        {
                            double nom;
                            if (Double.TryParse(data[i].Trim(), out nom))
                            {
                                temp.Add(nom);
                            }
                            else
                            {
                                Console.WriteLine("Error parsing " + data[i].Trim() + " expected a double, got something else, adding NaN to list");
                                temp.Add(Double.NaN);
                            }
                        }
                        ret.Add(temp);
                    }
                    return ret as Object;

                }
                else {
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

        public static double ComplexityCap { get; internal set; }
        public static CrossoverModes CrossoverMode { get; internal set; }

        public static Boolean IsDebugMode = false;
        public static int NumberOfClasses;
        public static Random RNG;
        public static int NumberOfFeatures;
        public static double CrossoverChance = .25, ElitismPercent = .20,
            InitialRateOfOnes = .5, MutationChance = .01;

        private const int dataDemarcation = 6;
        public static bool UseMWArrayInterface = false;

        public static Dictionary<String, int> ClassDict;
        public static List<String> ClassList, DataHeaders, yHeaders, AllPredictorNames;
        public static List<List<Double>> TrainingXRaw, TestingXRaw;
        public static List<List<String>> TrainingYRaw, TestingYRaw;
        public static bool[,] TrainingYRawLogical, TestingYRawLogical;
        public static double[,] TrX, TeX;
        public static String[,] TrainingYString, TestingYString;
        public static MLApp.MLApp executor = new MLApp.MLApp();
        private static Process ExecutorProcess;
        internal static int[,] testingYIntArray, trainingYIntArray;

        static OptoGlobals()
        {
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




        private static double[,] remapToIntensity(string[,] tempIntensity)
        {
            int rows = tempIntensity.GetUpperBound(1) + 1;

            double[,] ret = new double[rows, 1];
            for (int i = 0; i < rows; ++i)
            {
                Double temp;
                if (Double.TryParse(tempIntensity[0, i], out temp))
                {
                    temp = (temp + 1) * .2;//Maps 4s to 1, and 2s to .5.  Less than .5 should be noise... may adjust depending on actual numbers generated by alg.
                    ret[i, 0] = temp;
                }
                else
                {
                   Debugger.Break();
                }

            }
            return ret;
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

    }
}
