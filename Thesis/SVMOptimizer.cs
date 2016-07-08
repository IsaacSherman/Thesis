using System;
using System.Collections;
using System.Collections.Generic;
using MyUtils;


namespace EvoOptimization.SVMOptimizationNET40
{
    /// <summary>
    /// Fields are laid out like this: 
    /// Features|functionSpecificData
    /// Function Specific Data, for SVM, are Function (2 bits),  power(4 bits), kfold validation (4 bits)
    /// Function is 00 for Linear, 01 for rbf, 10 is polynomial, 11 is rerolled randomly, 
    /// if function is polynomial, additional argument polyPower is calculated as the int value of the power bits + 1, 
    /// 
    /// </summary>
    class SVMOptimizer : Optimizer
    {
        //kernelFunc is also a string, can be "rbf" or "linear" or "polynomial".  If polynomial, specify the order with 'polynomialOrder' and polyOrder  
        private enum FunctionType { rbf, linear, polynomial };
        private FunctionType _ft;
        protected int _polyPower, _kfold;
        //Let's say we had 4 pieces, each 2 bits long, so 00, 11, 22, 33 0-1, 2-3, 4-5, 6-7
        static int functionLength = 2, powerLength = 4, kfoldLength = 1, functionStart = firstFeature + OptoGlobals.NumberOfFeatures,  powerStart = functionStart + functionLength,
             kfoldStart = powerStart + powerLength, SVMOptimizerConstLength = OptoGlobals.NumberOfFeatures + functionLength+ powerLength + kfoldLength;
        public SVMOptimizer()
            : base(SVMOptimizerConstLength)
        {
            errorCheck();
        }
        public SVMOptimizer(String bitString)
            : base(bitString)
        {
            if (bitString.Length != SVMOptimizerConstLength) throw new InvalidCastException("Bitstring incorrect length");
            errorCheck();
        }

        static SVMOptimizer()
        {
            _optimizerToken = "SVM";
            functionString = "svmTrainPoly";
        }

        static public double[,] costMatrix = { { 0, 1 }, { 10, 0 } };
        public BitArray FeatureSet
        {
            get
            {
                return _bits.Range((uint)firstFeature, (uint)(firstFeature + OptoGlobals.NumberOfFeatures));
            }
        }

        protected override void errorCheck()
        {
            if (_bits[functionStart] && _bits[functionStart + 1])
            {
                _bits[functionStart] = OptoGlobals.RNG.NextDouble() < .5;
                _bits[functionStart + 1] = OptoGlobals.RNG.NextDouble() < .5;
                errorCheck();
                return;
            }
            setFT();
        }

        override public void AllColumns()
        {
            for (int i = firstFeature; i < OptoGlobals.NumberOfFeatures; ++i)
            {
                _bits[i] = false;
            }
        }
        protected void setFT()
        {
            if (_bits[functionStart]) _ft = FunctionType.polynomial;
            else if (_bits[functionStart + 1]) _ft = FunctionType.linear;
            else _ft = FunctionType.rbf;
        }
        public String GetFunction
        {
            get
            {
                switch (_ft)
                {
                    case FunctionType.rbf: return "rbf";

                    case FunctionType.linear: return "linear";
                    default: return "polynomial";
                }
            }

        }



        protected override void interpretVals()
        {
            String powerBitString = _bits.BitsToString((uint)powerStart, (uint)kfoldStart),
    kfoldBitString = _bits.BitsToString((uint)kfoldStart, (uint)SVMOptimizerConstLength);
            _polyPower = 1 + powerBitString.BinaryStringToInt();
            _kfold = 3 + kfoldBitString.BinaryStringToInt();
            errorCheck();

        }

        public override void Prepare()
        {
            errorCheck();
            interpretVals();
        }
        public override void Eval()
        {
            setFeatures();
            double[,] conf;                

            if (OptoGlobals.UseMWArrayInterface)
            {
                throw new NotImplementedException();

            }
            else
            {
                object[] args = getObjArgs(), parsedObjOut;
                object argsOutObj = null;
                comEvalFunc(args, out argsOutObj);
                parsedObjOut = (object[])argsOutObj;
                conf = (double[,])parsedObjOut[0];
                GeneratedLabels =new List<int>(Util.Flatten2dArray((int[,])parsedObjOut[2]));
            }
                
                _confuMat = new ConfusionMatrix((int)conf[1, 1], (int)conf[0, 0], (int)conf[0, 1], (int)conf[1, 0]);
                setFitness();
                
                NullData();
        }

        protected override object[] getObjArgs()
        {
            interpretVals();
            List<Object> ret = new List<object>();
            String kernelStr = "KernelFunction", kernel = GetFunction;
            ret.Add(kernelStr);
            ret.Add(kernel);


            if (_ft == FunctionType.polynomial)
            {
                ret.Add("PolynomialOrder");
            }
            else
            {
                ret.Add("KernelOffset");
            }
            ret.Add((double)_polyPower);


            ret.Add("KernelScale");
            ret.Add("auto");

            ret.Add("cost");
            ret.Add(SVMOptimizer.costMatrix);


            object[] realRet = ret.ToArray();

            return realRet;
        }




    }
}
