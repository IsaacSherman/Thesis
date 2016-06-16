using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using MLApp;
namespace EvoOptimization
{
    public class OptoGlobals
    {
        public enum CrossoverModes { Uniform, SinglePointChromasome, TwoPointChromasome, SinglePointHunter, TwoPointHunter, RandomHunter, RandomChromasome };
        private static int _seed = (int)DateTime.Now.Ticks;
        public static int GetSeed { get { return _seed; } }

        public static double ComplexityCap { get; internal set; }
        public static CrossoverModes CrossoverMode { get; internal set; }

        public static Boolean IsDebugMode = false;
        public static int NumberOfClasses = 12;
        public static Random RNG;
        public static int NumberOfFeatures;
        public static double CrossoverChance = .05, ElitismPercent = .20,
            InitialRateOfOnes = .03, MutationChance = .01;
        public static double[,] trX = new double[5, 5] { { 1, 3, 3, 5, 5 }, { 2, 4, 2, 4, 4 }, { 3, -5, 23, -43, 43 }, { 40, 4, 44, 34, 24 }, { -15, -53, -55, -53, 53 } },
            trY = new double[5, 1] { { 1 }, { 2 }, { 1 }, { 2 }, { 1 } },
            teX = new double[3, 5] { { 2, 4, 4, 4, 8 }, { 10, 30, 20, 30, 30 }, { 3, -3, 23, -43, 43 } },
            teY = new double[3, 1] { { 2 }, { 2 }, { 1 } };
        private const int dataDemarcation = 6;
        public static bool UseMWArrayInterface = false;

        public static Dictionary<String, int> ClassDict;
        public static List<String> ClassList, dataHeaders, yHeaders, allPredictorNames;
        public static List<List<Double>> trainingXRaw, testingXRaw;
        public static List<List<String>> trainingShaftID, testingShaftID;
        public static List<List<String>> trainingYRaw, testingYRaw;
        public static bool[,] trainingYRawLogical, testingYRawLogical;
        public static double[,] mwTrX, mwTeX;
        public static String[,] trainingYString, testingYString;
        public static MLApp.MLApp executor = new MLApp.MLApp();
        private static System.Diagnostics.Process executorProcess;
        private const int featureColumn = 6;
        public static bool FaultIsNotNoFault(String fault)
        {
            bool ret = !fault.ToLower().Trim().Equals("no fault");
            return ret;
        }
        public static int totalNumFeatures;



        //TODO:
        //Things Optoglobals needs to function:
        //number of features, needs to be read in.  Required by Optimizers.
        //list of strings, indexed, which contains the classes in the dataset.  Easily generalizable.  Order needs to be read in. 
        //File paths to look for the data!  Need to handle different datasets - list columns to include, omit, and which are classes.  Import everything as strings.

        static OptoGlobals()
        {
#if DEBUG
            IsDebugMode = true;
#endif

            Console.WriteLine("Entering OptoGlobals STATIC constructor");
            RNG = new Random(_seed);
            String baseFilePath = @"C:\Users\isaac.sherman\Documents\TFS\Isaac Sherman\EvoAlgApplet\SVMOptimization\Data\";
            trainingShaftID = new List<List<string>>();
            testingShaftID = new List<List<string>>();

            trainingXRaw = OptoGlobals.loadData(baseFilePath + "trainingDataMatrix.csv", true, out trainingShaftID);
            testingXRaw = OptoGlobals.loadData(baseFilePath + "testingDataMatrix.csv", false, out testingShaftID);
            testingYRaw = OptoGlobals.loadLabels(baseFilePath + "testingClassLabels.csv");
            trainingYRaw = OptoGlobals.loadLabels(baseFilePath + "trainingClassLabels.csv");
            trainingYString = pullColumnFromArray(trainingYRaw, 6);
            testingYString = pullColumnFromArray(testingYRaw, 6);
            trainingYRawLogical = ConvertStringsToLogicalArray(trainingYRaw, featureColumn);
            testingYRawLogical = ConvertStringsToLogicalArray(testingYRaw, featureColumn);
            allPredictorNames = GetPredictorNames();
            totalNumFeatures = trainingXRaw[0].Count;

            ClassDict = new Dictionary<string, int>(12);
            int tempCl = 0;
            string[] tempCats = {"Bearing Defect","Belt Defect","Coupling Defect","Electrical Defect","Gear Defect",
                                    "Imbalance","Looseness","Lubrication","Misalignment","No Fault","Rotor Bar Defect",
                                    "Vane Pass"};
            ClassList = new List<string>(tempCats);
            foreach (String cat in tempCats)
            {
                ClassDict.Add(cat, tempCl++);

            }

            String path = @"./../../../Matlab Scripts/";
            path = Path.GetFullPath(path);

            assignExecutorProcess();


            Console.WriteLine(executor.Execute("cd " + "'" + path + "'"));
            Console.WriteLine(executor.ToString());


            Console.WriteLine("Leaving Static Constructor");
        }

        private static void assignExecutorProcess()
        {
            Process[] procs = Process.GetProcesses();
            foreach (Process x in Process.GetProcesses())
            {
                if (x.MainWindowTitle.Equals("MATLAB Command Window"))
                {
                    executorProcess = x;
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
                    System.Diagnostics.Debugger.Break();
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

        private static List<String> GetPredictorNames()
        {
            List<String> ret;
            ret = new List<String>(NumberOfFeatures);
            int offset = dataDemarcation;
            for (int i = 0; i < NumberOfFeatures; ++i)
            {
                ret.Add(dataHeaders[i + offset]);
            }
            return ret;
        }

        private static bool[,] ConvertStringsToLogicalArray(List<List<String>> inArray, int col)
        {
            //int[] dims = { inArray.Count, 1 };//Column vector
            //MWLogicalArray ret = new MWLogicalArray(dims);
            bool[,] ret = new bool[inArray.Count, 1];
            for (int i = 0; i < inArray.Count; ++i)
            {
                ret[i, 0] = FaultIsNotNoFault(inArray[i][col]);
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
                if (getHeaders) dataHeaders = extractHeadersFromFirstLine(line);

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
                    if (ret.Count > 1) System.Diagnostics.Debug.Assert(ret[ret.Count - 1].Count == dataRow.Count);
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
