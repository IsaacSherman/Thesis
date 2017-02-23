using MyUtils;
using System;
using System.Collections.Generic;


namespace EvoOptimization.CTreeOptimizer
{
    class CTreeOptimizer : Optimizer
    {

        //bits: 0-236: Features; 237-244: nLearnersLength(8 bits + 100); 245-252, marginalPrecision (8bits / 256), 253-256 (cost, 2+3bits)
        //mergeLeaves (on or off), 'MaxNumSplits' and integer, 'MinLeafSize' and int, 'SplitCriterion' = 'gdi', 'twoing', or 'deviance', 

        static int firstFeature = 0, featureLength = OptoGlobals.NumberOfFeatures, mergeLeavesLength = 1, maxNumSplitsLength = 6,
            minLeafSizeLength=5, splitCriterionLength = 2,
            mergeLeavesStart = firstFeature + featureLength, maxNumSplitsStart = mergeLeavesStart + mergeLeavesLength, minLeafSizeStart = maxNumSplitsLength + 
            maxNumSplitsStart, splitCriterionStart = minLeafSizeStart+minLeafSizeLength,
            totalCTreeLength = featureLength + mergeLeavesLength + maxNumSplitsLength + minLeafSizeLength + splitCriterionLength;

        public static void RewriteBitLengths()
        {
            firstFeature = 0; featureLength = OptoGlobals.NumberOfFeatures; mergeLeavesLength = 1; maxNumSplitsLength = 6;
            minLeafSizeLength=5; splitCriterionLength = 2; 
            mergeLeavesStart = firstFeature + featureLength;
            maxNumSplitsStart = mergeLeavesStart + mergeLeavesLength; 
            minLeafSizeStart = maxNumSplitsLength + maxNumSplitsStart;
            splitCriterionStart = minLeafSizeStart+minLeafSizeLength;
            totalCTreeLength = featureLength + mergeLeavesLength + maxNumSplitsLength + minLeafSizeLength + splitCriterionLength;
        }

        private string splitCriterion, mergeLeaves;
        private int maxSplits, minLeafSize; 

        protected override void interpretVals()
        {
            AssignSplitCriterion();
            mergeLeaves = Bits[mergeLeavesStart] ? "on" : "off"; 
            maxSplits = 3 + Bits.Range((uint)maxNumSplitsStart, (uint)minLeafSizeStart).BitsToString().BinaryStringToInt();
            minLeafSize = 1 + Bits.Range((uint)minLeafSizeStart, (uint)splitCriterionStart).BitsToString().BinaryStringToInt();

        }

        static private new string _optimizerToken;

        private void AssignSplitCriterion()
        {
            string temp = "";
            switch (Bits.Range((uint)splitCriterionStart, (uint)(splitCriterionStart + splitCriterionLength)).BitsToString().BinaryStringToInt())
            {
                case 0:
                    temp = "gdi";
                    break;
                case 1:
                    temp = "twoing";
                    break;
                case 2:
                    temp = "deviance";
                    break;
                default:
                    rerollBits(splitCriterionStart, splitCriterionStart + splitCriterionLength);
                    AssignSplitCriterion();
                    break;
            }
            splitCriterion = temp;
            if (splitCriterion == "") AssignSplitCriterion();

        }
        


        double _precisionVar, _cost;
        const double _precisionConstant = 258;
        public CTreeOptimizer()
            : base(totalCTreeLength)
        {
        }
        public CTreeOptimizer(string bitstring)
            : base(bitstring)
        {
            Prepare();
        }
        static CTreeOptimizer()
        {
            _optimizerToken = "CTree";
        }

        protected override string getFunctionString()
        {
            return "trainCTree";
        }
           

        protected override object[] getObjArgs()
        {
            interpretVals();
            List<Object> ret = new List<object>();
            ret.Add("MergeLeaves");
            ret.Add(mergeLeaves);
            ret.Add("SplitCriterion")
          ; ret.Add(splitCriterion);
            ret.Add("MinLeafSize");
            ret.Add(minLeafSize);
            ret.Add("MaxNumSplits");
            ret.Add(maxSplits);
            
            return ret.ToArray();
        }

        protected override void errorCheck()
        {
            AssignSplitCriterion();//if there are any errors, they're in there
        }



        public override void Eval()
        {
            setFeatures();
            interpretVals();
            double[] classAcc;
            double[,] label = null;
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

            }               
            _confuMat = new ConfusionMatrix((int)label[1, 1], (int)label[0, 0], (int)label[0, 1], (int)label[1, 0]);
            setFitness();
            NullData();


        }

        public override string GetToken { get { return _optimizerToken; } }
    }
}
