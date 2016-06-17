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

        internal static void ConfigureForDataset(string globalPath)
        {
            using (StreamReader fin = new StreamReader(new BufferedStream(new FileStream(globalPath, FileMode.Open))))
            {

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
                ///


            }
        }

        public static double ComplexityCap { get; internal set; }
        public static CrossoverModes CrossoverMode { get; internal set; }

        public static Boolean IsDebugMode = false;
        public static int NumberOfClasses = 12;
        public static Random RNG;
        public static int NumberOfFeatures;
        public static double CrossoverChance = .25, ElitismPercent = .20,
            InitialRateOfOnes = .03, MutationChance = .01;

        private const int dataDemarcation = 6;
        public static bool UseMWArrayInterface = false;

        public static Dictionary<String, int> ClassDict;
        public static List<String> ClassList, dataHeaders, yHeaders, allPredictorNames;
        public static List<List<Double>> trainingXRaw, testingXRaw;
        public static List<List<String>> trainingShaftID, testingShaftID;
        public static List<List<String>> trainingYRaw, testingYRaw;
        public static bool[,] trainingYRawLogical, testingYRawLogical;
        public static double[,] TrX, TeX;
        public static String[,] trainingYString, testingYString;
        public static MLApp.MLApp executor = new MLApp.MLApp();
        private static Process executorProcess;
        private const int featureColumn = 6;
        public static bool FaultIsNotNoFault(String fault)
        {
            bool ret = !fault.ToLower().Trim().Equals("no fault");
            return ret;
        }
        public static int totalNumFeatures;




        static OptoGlobals()
        {
#if DEBUG
            IsDebugMode = true;
#endif
            ///This whole thing needs to be moved around
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

            String MATLABpath = @"./../../../Matlab Scripts/";
            MATLABpath = Path.GetFullPath(MATLABpath);

            assignExecutorProcess();


            Console.WriteLine(executor.Execute("cd " + "'" + MATLABpath + "'"));
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
