using System;
using System.Collections.Generic;
using System.Text;

using MyUtils;

namespace EvoOptimization.MulticlassNBOptimizer
{
    class MulticlassNBOptimizer : Optimizer
    {

                static string[] distroNames = { "kernel", "mn", "mvmn", "normal" }, kernelTypes = { "box", "epanechnikov", "normal", "triangle" },
            scoreXform = {"doublelogit", "invlogit", "logit", "none", "sign", "symmetric", "symmetriclogit", "symmetricismax"};
        static int _featureLength, _distroNameLength, _kernelTypeLength, _scoreLength,
            _priorLength, _distroNameStart, _kernelTypeStart, _scoreStart,
            _priorStart, _mcnbBitLength;
        private static int _numClasses { get { return OptoGlobals.ClassList.Count; } }
        double[] _prior = new double[_numClasses];
        String _distName, _kernelType, _scoreTransform;
        private static new string _optimizerToken;
        static MulticlassNBOptimizer()
        {
            Multiclass = true;
            _optimizerToken = "McNB";
        }

        public override string GetToken { get { return "McNB"; } }
        protected override string getFunctionString()
        {
            return "trainMulticlassNB";
        }

//        private override string functionString() {return "trainMulticlassNB";}

        public MulticlassNBOptimizer(): base(_mcnbBitLength)
        {
            Prepare();
        }
        public static void RewriteBits()
        {
            _featureLength = OptoGlobals.NumberOfFeatures;
            _distroNameLength = 2;
            _kernelTypeLength = 2;
            _scoreLength = 3;
            _priorLength = OptoGlobals.ClassList.Count * 3;

            _distroNameStart = _featureLength + Optimizer.firstFeature;
            _kernelTypeStart = _distroNameStart + _distroNameLength;
            _scoreStart = _kernelTypeStart + _kernelTypeLength;

            _priorStart = _kernelTypeStart + _kernelTypeLength;
            _mcnbBitLength = _priorStart + _priorLength;
        }
        public MulticlassNBOptimizer(String bitstring) :
            base(bitstring)
        {
            Prepare();
        }

        protected override object[] getObjArgs()
        {
            if(Bits.Length != _mcnbBitLength)
            {
                int diff = _mcnbBitLength - Bits.Length;
                string initialBits = String.Copy(ToString());
                StringBuilder pad;
                if (diff > 0)
                {
                    pad = new StringBuilder(' ', diff);
                    for(int i = 0; i < diff; ++i)
                    {
                        pad[i] = OptoGlobals.RNG.NextDouble() > .5? '1': '0';
                    }

                    pad = new StringBuilder(initialBits + pad.ToString());
                }
                else
                {
                    pad = new StringBuilder(ToString());
                    pad.Remove(_mcnbBitLength, diff * -1);
                }

                System.Diagnostics.Debug.Assert(pad.Length == _mcnbBitLength);
                _bits = new System.Collections.BitArray(_mcnbBitLength);
                SetBitsToString(pad.ToString());
                Prepare();
                interpretVals();
            }
            List<Object> ret = new List<object>();
            ret.Add("DistributionName");
            ret.Add(_distName);
            if (_distName == "kernel")
            {
                ret.Add("Kernel");
                ret.Add(_kernelType);
            }
            ret.Add("ScoreTransform");
            ret.Add(_scoreTransform);

            ret.Add("Prior");
            ret.Add(_prior);

            return ret.ToArray();
        }


 
        protected override void errorCheck()
        {
            if (interpretDistribution() == "mn")
            {
                rerollBits(_distroNameStart, _distroNameStart + _distroNameLength);
                errorCheck();
            }
        }

        protected override void interpretVals()
        {
            _distName = interpretDistribution();
            _kernelType = interpretKernel();
            _scoreTransform = interpretScore();
            interpretPrior();
        }

        private void interpretPrior()
        {

            for (int i = 0; i < _numClasses; ++i)
            {
                uint start = (uint)(_priorStart + (i * 3));
                uint end = start + 3;
                _prior[i] = 1 + _bits.Range(start, end).BitsToString().BinaryStringToInt();
            }
            
        }

        private string interpretScore()
        {
            int index = _bits.Range((uint)_scoreStart, (uint)(_scoreStart + _scoreLength)).BitsToString().BinaryStringToInt();
            return MulticlassNBOptimizer.scoreXform[index];
        }

        private string interpretKernel()
        {
            int index = _bits.Range((uint)_kernelTypeStart, (uint)(_kernelTypeStart + _kernelTypeLength)).BitsToString().BinaryStringToInt();
            return MulticlassNBOptimizer.kernelTypes[index];
        }

        private string interpretDistribution()
        {
            int index = _bits.Range((uint)_distroNameStart, (uint)(_distroNameStart + _distroNameLength)).BitsToString().BinaryStringToInt();
            return MulticlassNBOptimizer.distroNames[index];
        }

        public override void Eval()
        {
            setFeatures();
            double[] classAcc;
            double[,] label;
            if (OptoGlobals.UseMWArrayInterface)
            {
                throw new NotImplementedException();
            }
            else
            {
                object[] args = getObjArgs(), parsedArgsOut;
                object argsOut;
                bool success = comEvalFunc(args, out argsOut);
                parsedArgsOut = (object[])argsOut;
                label = (double[,])parsedArgsOut[0];
                if (success)
                {
                    GeneratedLabels = new List<int>(Util.Flatten2dArray((int[,])parsedArgsOut[2]));          
                    classAcc = Optimizer.ScoreMulticlassClassifier(label);
                    classAccuracy = new List<double>(classAcc);

                }
                else
                {
                    int ark = 4;
                    ark++;
                }

            }

            setFitness();
            
            NullData();

        }

    }
}
