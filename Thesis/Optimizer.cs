using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using MyUtils;
using MathWorks.MATLAB.NET.Arrays;
using Emerson.CSI.Applet.MHM.Internal.EvoAlgApplet;
using System.IO;
namespace EvoOptimization
{
    public abstract class Optimizer : IComparable<Optimizer>
    {
        internal const int firstFeature = 0, featureLength = 238;
        public string GetToken { get { return _optimizerToken; } }
        protected void PrepForSave(string path, System.Security.AccessControl.DirectorySecurity temp)
        {

            Directory.CreateDirectory(path, temp);

            using (StreamWriter folut = new StreamWriter(path + ".csv"))
            {
                DumpLabelsToStream(folut);
            }

           
        }
        public static Boolean Multiclass = false;
        protected void PrepForSave(string path)
        {
            System.Security.AccessControl.DirectorySecurity activeDirSec = Directory.GetAccessControl(@".\");
            PrepForSave(path, activeDirSec);
        }

        protected double[,] reduceMatrix(int cols, List<List<double>> baseMatrix){
            int rows = baseMatrix.Count;
            int colStepper = 0;
            double[,] ret = new double[rows, cols];


            for (int i = firstFeature; i < featureLength+firstFeature; ++i)
            {
                if (_bits[i])
                {
                    for (int j = 0; j < rows; ++j)
                    {
                        ret[j, colStepper] = baseMatrix[j][i];
                    }
                    ++colStepper;
                }
                //for each row, copy column contents
            }
            return ret;
        }

        protected static string functionString;
        protected double[,] myBaseTeX, myBaseTrX;
        protected List<int> SuspectRows;
        public Double[,] GeneratedLabels = null, CVGeneratedLabels = null;
        public object MatLabModel = null;
        public Double[,] myPredictorLabels = null;
        protected BitArray _bits;
        protected double _nLearners = 1;


        protected void NullMatlabModel()
        {
            MatLabModel = null;
        }

        protected void NullGeneratedLabels()
        {
            GeneratedLabels = null;
            CVGeneratedLabels = null;
        }

        public void CompactMemory()
        {
            NullMatlabModel();
            NullGeneratedLabels();
        }


        public int Length { get { return _bits.Length; } }
        public BitArray Bits { get { return _bits; } }
        public Optimizer() : this(featureLength) { }
        public Optimizer(int stringLength)
        {
            _bits = new BitArray(stringLength);
            _confuMat = new ConfusionMatrix();
            for (int i = 0; i < stringLength; ++i)
                _bits[i] = OptoGlobals.RNG.NextDouble() < OptoGlobals.InitialRateOfOnes;
            errorCheck();
        }
        public virtual void AllColumns()
        {
            for (int i = firstFeature; i < featureLength; ++i)
            {
                _bits[i] = false;
            }
            myTeX = OptoGlobals.mwTeX;
            myTrX = OptoGlobals.mwTrX;

        }
        protected ConfusionMatrix _confuMat;
        public ConfusionMatrix GetConfusionMatrix() { return _confuMat; }
        virtual public void DumpSuspectRowsToStream(System.IO.StreamWriter fout)
        {
            if (SuspectRows == null) CompareLabelsToIntensity();
            foreach (int i in SuspectRows)
            {
                foreach (String piece in OptoGlobals.testingYRaw[i])
                {
                    fout.Write(piece + ", ");
                }
                fout.WriteLine();
            }
        }
        protected static string _optimizerToken;
        virtual public void DumpLabelsToStream(System.IO.StreamWriter fout)
        {
            if (Multiclass)
                dumpStringLabelsToStream(fout, GeneratedLabels);
            else
                dumpLabelsToStream(fout, GeneratedLabels);
        }

        virtual public void DumpCVLabelsToStream(System.IO.StreamWriter fout)
        {
            if (Multiclass) 
                dumpStringLabelsToStream(fout, CVGeneratedLabels);
            else 
                dumpLabelsToStream(fout, CVGeneratedLabels);
        }

        /// <summary>
        /// Writes the labels to the supplied stream, interprets them through the ClassList maintained in the globals.
        /// </summary>
        /// <param name="fout"></param>
        /// <param name="Labels"></param>
        protected void dumpStringLabelsToStream(System.IO.StreamWriter fout, List<int> Labels)
        {
            if (Labels == null) return;
            fout.WriteLine(_optimizerToken);
            for (int i = 1; i <= Labels.Count; ++i)
            {
                fout.WriteLine(OptoGlobals.ClassList[Labels[i]]);
            }
        }


        protected void dumpLabelsToStream(System.IO.StreamWriter fout, List<int> Labels)
        {
            if (Labels == null) return;
            fout.WriteLine(_optimizerToken);
            for (int i = 1; i <= Labels.Count; ++i)
            {
                fout.WriteLine(Labels[i]);
            }
        }


        public static String Token { get { return _optimizerToken; } }
        public Optimizer(String bitString)
        {
            _bits = new BitArray(bitString.Length);
            SetBitsToString(bitString);

            Prepare();
        }
        protected double _fitness = -1;
        protected double _mcc = -1;
        protected string[] myBaseLabels;
        public double MCC { get { return _mcc; } set { _mcc = value; } }
        public double Fitness { get { return _fitness; } set { _fitness = value; } }


        protected virtual void NullData()
        {
            myTeX = myTrX = null;
            myBaseLabels = null;
            myBaseTeX = myBaseTrX = null;
            myPredictorLabels = null;
        }

        public virtual void Save(string path)
        {
            PrepForSave(path);//Creates directory
            string fullPath = System.IO.Path.GetFullPath(path) + "\\" + path.Substring(path.LastIndexOf('\\') + 1);

            if (OptoGlobals.UseMWArrayInterface)
            {
                throw new NotImplementedException();
            }
            else
            {
                SaveModelByLongEval(true, fullPath);
            }

        }

                protected static MWArray extractNumericLabels(String blockString)
        {
            
            char[] tokens = { '\n' };
            string[] StrLabels = blockString.Split(tokens, StringSplitOptions.RemoveEmptyEntries);
            int[,] intLabels = new int[StrLabels.Length, 1];
            for (int i = 0; i < StrLabels.Length; ++i)
            {
                intLabels[i, 0] = OptoGlobals.ClassDict[StrLabels[i].Trim()];
            }
            return new MWNumericArray(intLabels);
        }

        /// <summary>
        /// This assumes that we are not using the dll interface, because we can get CV labels and save much more directly.
        /// </summary>
        /// <param name="doSave"></param>
        /// <param name="path"></param>
        public virtual void SaveModelByLongEval(bool doSave, string path)
        {            
            Prepare();
            setFeatures();
            object[] args = getObjArgs();
            object argsOut;
            if(Multiclass){
                            OptoGlobals.executor.Feval("longMCSave", 2, out argsOut, doSave, path, functionString, _nLearners, myBaseTrX, myBaseTeX, 
                OptoGlobals.trainingYString, OptoGlobals.testingYString, myBaseLabels,args);
            }
            else{
            OptoGlobals.executor.Feval("longSave", 2, out argsOut, doSave, path, functionString, _nLearners, myBaseTrX, myBaseTeX, 
                OptoGlobals.trainingYRawLogical, OptoGlobals.testingYRawLogical, myBaseLabels,args);
            }
            object[] parsedArgsOut = (object[])argsOut;
            if (!Multiclass)
            {
                CVGeneratedLabels = new MWLogicalArray((bool[,])parsedArgsOut[0]);
                GeneratedLabels = new MWLogicalArray((bool[,])parsedArgsOut[1]);
            }
            else
            {
                CVGeneratedLabels = extractNumericLabels((string)parsedArgsOut[0]);
                GeneratedLabels = extractNumericLabels((string)parsedArgsOut[1]);
            }
            NullData();
        }

        protected abstract object[] getObjArgs();

        public MWArray GetCVLabels()
        {
            if (OptoGlobals.UseMWArrayInterface)
            {
                throw new NotImplementedException();
                //object[] argsOut = evaluator.getCVLabels(1, (MWArray)MatLabModel);
                //CVGeneratedLabels = (MWArray)argsOut[0];
            }
            else
            {
                if (CVGeneratedLabels == null)
                {
                    //This line should never be executed - cross validating a model is a high-cost activity, should use Save rather than GETCVLabels
                    //but if they don't exist...
                    SaveModelByLongEval(false, "nopath");
                }
            }
            return CVGeneratedLabels;

        }

        /// <summary>
        /// Generates list of suspect rows (where the intensity is watch, but is given a false negative label) for printing in DumpSuspectRowsToStream
        /// </summary>
        public virtual void CompareLabelsToIntensity()
        {
            SuspectRows = new List<int>();
            if (GeneratedLabels == null) return;
            if (!Multiclass)
            {
                for (int i = 1; i <= GeneratedLabels.Dimensions[0]; ++i)
                {
                    if (!((MWLogicalArray)GeneratedLabels)[i, 1] && OptoGlobals.testingYRawLogical[i - 1, 0])
                    {
                        int intensity = Int16.Parse(OptoGlobals.testingYRaw[i - 1][7]);
                        //Predict negative, actually positive
                        if (PassableMisses.ContainsKey(intensity)) PassableMisses[intensity] += 1;
                        else PassableMisses.Add(intensity, 1);
                        if (intensity == 2) SuspectRows.Add(i - 1);
                    }

                }
            }
            else
            {
                for (int i = 1; i <= GeneratedLabels.Dimensions[0]; ++i)
                {
                    int prediction = (int)((MWNumericArray)GeneratedLabels[i, 1]);
                    if (prediction == OptoGlobals.ClassDict["No Fault"] && OptoGlobals.testingYRawLogical[i - 1, 0])
                    {
                        int intensity = Int16.Parse(OptoGlobals.testingYRaw[i - 1][7]);
                        //Predict negative, actually positive
                        if (PassableMisses.ContainsKey(intensity)) PassableMisses[intensity] += 1;
                        else PassableMisses.Add(intensity, 1);
                        if (intensity == 2) SuspectRows.Add(i - 1);
                    }

                }
            }
        }

        /// <summary>
        /// Creates myTex and myTrX for the optimizer from features in bits (compare 
        /// </summary>
        protected virtual void setFeatures()
        {
            int cols = 0;
            foreach (bool bit in _bits.Range((uint)firstFeature, (uint)(firstFeature + featureLength))) { if (bit) cols++; }
            if (cols == 0)
            {
                Fitness = Double.NaN;
                if (OptoGlobals.UseMWArrayInterface)
                {
                    myTeX = OptoGlobals.mwTeX;
                    myTrX = OptoGlobals.mwTrX;
                    myPredictorLabels = new MWCellArray(OptoGlobals.allPredictorNames.ToArray());
                }
                else
                {
                    _bits = _bits.Not();//Flip all the bits, since features are all zero, and then convert matrices to [,]
                    myBaseTeX = reduceMatrix(featureLength, OptoGlobals.testingXRaw);
                    myBaseTrX = reduceMatrix(featureLength, OptoGlobals.trainingXRaw);
                    myBaseLabels = OptoGlobals.allPredictorNames.ToArray();
                    _bits = _bits.Not();//flip bits back
                }
                return;
            }

            List<List<Double>> baseMatrix = OptoGlobals.trainingXRaw;
            if (OptoGlobals.UseMWArrayInterface)
            {
                myTrX = new MWNumericArray(reduceMatrix(cols, OptoGlobals.trainingXRaw));
                myTeX = new MWNumericArray(reduceMatrix(cols, OptoGlobals.testingXRaw));
                myPredictorLabels = new MWCellArray(reduceLabels(cols, OptoGlobals.allPredictorNames));
            }
            else
            {
                myBaseTrX = reduceMatrix(cols, OptoGlobals.trainingXRaw);
                myBaseTeX = reduceMatrix(cols, OptoGlobals.testingXRaw);
                myBaseLabels = reduceLabels(cols, OptoGlobals.allPredictorNames);

            }
        }

        protected string[] reduceLabels(int cols, List<String> labels)
        {
            int [] dims = {1, cols};
            string[] ret = new string[cols];
            int colStepper = 0;
            for (int i = firstFeature; i < featureLength + firstFeature; ++i)
            {
                if (_bits[i])
                {//Matlab arrays are 1 indexed for some reason
                        ret[colStepper++] = labels[i];
                }
            }
            return ret;
        }

        public void SetBitsToString(String bitString)
        {
            System.Diagnostics.Debug.Assert(bitString.Length == Bits.Length);
            for (int i = 0; i < bitString.Length; ++i)
            {
                _bits[i] = bitString[i] == '1';
            }
        }

        /// <summary>
        /// Invokes errorCheck() and interpretVals(), and anything else to make eval a legitimate call.  Publically accessible.
        /// </summary>
        public virtual void Prepare()
        {
            errorCheck();
            interpretVals();
        }
        /// <summary>
        /// Gets arguments for MatLab, returned in an array list.
        /// </summary>
        /// <returns></returns>
        protected abstract MWArray[] getArgs();
        /// <summary>
        /// Corrects internal errors in the optimizer
        /// </summary>
        protected abstract void errorCheck();
        /// <summary>
        /// Assigns internal values
        /// </summary>
        protected abstract void interpretVals();
        /// <summary>
        /// Evaluates the bitstring in matlab
        /// </summary>
        public abstract void Eval();
        public override String ToString() { return _bits.BitsToString(); }
        public void SetFitness()
        {
            this.setFitness();
        }

        public static T And<T>(T a, T b) where T : Optimizer, new()
        {
            T ret = new T();
            BitArray tempBits = (BitArray)a._bits.Clone();
            ret._bits = tempBits.And(b.Bits);
            return ret;
        }

        public static T Or<T>(T a, T b) where T : Optimizer, new()
        {
            T ret = new T();
            BitArray tempBits = (BitArray)a._bits.Clone();
            ret._bits = tempBits.Or(b.Bits);
            return ret;
        }

        public static T Xor<T>(T a, T b) where T : Optimizer, new()
        {
            T ret = new T();
            BitArray tempBits = (BitArray)a._bits.Clone();
            ret._bits = tempBits.Xor(b.Bits);
            return ret;
        }


        int IComparable<Optimizer>.CompareTo(Optimizer other)
        {
            int ret = _fitness.CompareTo(other._fitness);
            if (ret == 0) ret = _mcc.CompareTo(other._mcc);
            return ret;
        }

        internal static void RefreshEvaluator()
        {
            //throw new NotImplementedException();
            //evaluator = new TrainingSuite();
        }

        protected virtual void setFitness()
        {
            if (_confuMat != null)
            {
                _fitness = _confuMat.AverageAccuracy;
                _mcc = _confuMat.MatthewsCorrelationCoefficient;
            }
            else
            {
                _fitness = 0;
                _mcc = -1;
            }
            if (Double.IsNaN(_fitness) || _fitness < 0) _fitness = 0;
        }

        protected virtual bool comEvalFunc(object[] args, out object argsOutObj)
        {
            bool success = true;
            try
            {
                if (!Multiclass)
                {
                    OptoGlobals.executor.Feval(Optimizer.functionString, 3, out argsOutObj, myBaseTrX, myBaseTeX,
                    OptoGlobals.trainingYRawLogical, OptoGlobals.testingYRawLogical, myBaseLabels, _nLearners, args);
                }
                else
                {
                    OptoGlobals.executor.Feval(Optimizer.functionString, 3, out argsOutObj, myBaseTrX, myBaseTeX,
                        OptoGlobals.trainingYString, OptoGlobals.testingYString, myBaseLabels, _nLearners, args);
                }

            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                success = false;
                object[] tempObj = new object[3];
                double[,] badDouble = { { 0, 0 }, { 0, 0 } };

                tempObj[0] = badDouble;
                tempObj[1] = null;
                tempObj[2] = badDouble;
                argsOutObj = tempObj;


            }
            return success;

        }

        protected void rerollBits(int start, int end)
        {
            for (int i = start; i < end; ++i)
            {
                _bits[i] = OptoGlobals.RNG.NextDouble() < .5;
            }
        }


        public static int[,] ScoreMulticlassClassifier(double[,] label)
        {
            int[,] conf = new int[2, 2];
            double wrongPositives=0;//calls that are incorrect, but not calling a false alarm or missed call
            if (OptoGlobals.IsDebugMode)
            {
                for (int i = 0; i < 12; ++i)
                {
                    for (int j = 0; j < 12; ++j)
                    {
                        Console.Write(label[i, j] + ", ");
                        System.Diagnostics.Debug.Write(label[i, j] + ", ");
                    }
                    Console.WriteLine();
                    System.Diagnostics.Debug.WriteLine("");

                }
            }

            for (int row = 0; row < OptoGlobals.NumberOfClasses; ++row)
            {
                for (int col = 0; col < OptoGlobals.NumberOfClasses; ++col)
                {
                    if (row == col)//On diagonal, so true
                    {
                        if (row == OptoGlobals.ClassDict["No Fault"]) conf[0, 0] += (int)label[row, col];//True Negative
                        else
                        {
                            conf[1, 1] += (int)label[row, col];//True Positive
                        }
                    }
                    else
                    {//not on diagonal
                        if (row == OptoGlobals.ClassDict["No Fault"])
                        {//False Positive
                            conf[0, 1] += (int)label[row, col];
                        }
                        else if (col == OptoGlobals.ClassDict["No Fault"])//Column was what the classifier called. False negative
                        {
                            //false Negative
                            conf[1, 0] += (int)label[row, col];
                        }
                        else
                        {
                            wrongPositives += label[row, col];
                        }
                    }

                }
            }
            conf[1, 1] += (int)(wrongPositives * .5);//Partial credit assignment
            return conf;
        }

        internal void MakeEmptyConfumat()
        {
            _confuMat = new ConfusionMatrix();
        }
    }

}
