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

        static void reapplyFeatureLengths()
        {
            firstFeature = 0; featureLength = OptoGlobals.NumberOfFeatures; mergeLeavesLength = 1; maxNumSplitsLength = 6;
            minLeafSizeLength=5; splitCriterionLength = 2; 
            mergeLeavesStart = firstFeature + featureLength;
            maxNumSplitsStart = mergeLeavesStart + mergeLeavesLength; 
            minLeafSizeStart = maxNumSplitsLength + maxNumSplitsStart;
            splitCriterionStart = minLeafSizeStart+minLeafSizeLength;
            totalCTreeLength = featureLength + mergeLeavesLength + maxNumSplitsLength + minLeafSizeLength + splitCriterionLength;
        }

        string splitCriterion, mergeLeaves;
        int maxSplits, minLeafSize; 

        protected override void interpretVals()
        {
            AssignSplitCriterion();
            mergeLeaves = Bits[mergeLeavesStart] ? "on" : "off"; 
            maxSplits = 3 + Bits.Range((uint)maxNumSplitsStart, (uint)minLeafSizeStart).BitsToString().BinaryStringToInt();
            minLeafSize = 1 + Bits.Range((uint)minLeafSizeStart, (uint)splitCriterionStart).BitsToString().BinaryStringToInt();

        }


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
            functionString = "trainCTree";
        }

        protected override object[] getObjArgs()
        {
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
            //No errors
        }



        public override void Eval()
        {
            setFeatures();
            interpretVals();
            double[,] label = null;
            if (OptoGlobals.UseMWArrayInterface)
            {
                throw new NotImplementedException();
                //TrainingSuite Evaluator = Optimizer.evaluator;
                //MWArray[] args = getArgs();
                //bool failed = false;
                //MWArray[] argsOut = null;
                //try
                //{
                //    argsOut = Evaluator.trainRUSBoost(3, myTrX, myTeX, OptoGlobals.mwTrYLog, OptoGlobals.mwTeYLog, _nLearners, myPredictorLabels, args);
                //}
                //catch (Exception e)
                //{
                //    failed = true;
                //}
                //finally
                //{
                //    if (!failed)
                //    {
                //        MWNumericArray demilabel = (MWNumericArray)argsOut[0];
                //        label = (double[,])demilabel.ToArray();
                //        GeneratedLabels = argsOut[2];
                //        MatLabModel = argsOut[1];
                //    }
                //}
            }
            else
            {
                object[] args = getObjArgs(), parsedObjOut;
                object argsOutObj;


                comEvalFunc(args, out argsOutObj);
                parsedObjOut = (object[])argsOutObj;

                label = (double[,])parsedObjOut[0];

                GeneratedLabels = new MWLogicalArray((bool[,])parsedObjOut[2]);

            }               
            _confuMat = new ConfusionMatrix((int)label[1, 1], (int)label[0, 0], (int)label[0, 1], (int)label[1, 0]);
            setFitness();
            CompareLabelsToIntensity();
            NullData();


        }
    }
}
