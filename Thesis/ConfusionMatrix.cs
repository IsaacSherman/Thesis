using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyUtils
{
    #region class ConfusionMatrix:
    public class ConfusionMatrix : IComparable<ConfusionMatrix>
    {
        /// <summary>
        /// Core data
        /// </summary>
        private int truePs, trueNs, falsePs, falseNs;
        public enum HitTypes { truePositive, trueNegative, falsePositive, falseNegative };
        public void RecordEvent(HitTypes hit)
        {
            switch (hit)
            {
                case HitTypes.trueNegative:
                    this.trueNs++;
                    break;
                case HitTypes.truePositive:
                    this.truePs++;
                    break;
                case HitTypes.falsePositive:
                    this.falsePs++;
                    break;
                case HitTypes.falseNegative:
                    this.falseNs++;
                    break;
                default:
                    break;
            }
        }
        public static void RecordEvent(HitTypes hit, ConfusionMatrix a)
        {
            switch (hit)
            {
                case HitTypes.trueNegative:
                    a.trueNs++;
                    break;
                case HitTypes.truePositive:
                    a.truePs++;
                    break;
                case HitTypes.falsePositive:
                    a.falsePs++;
                    break;
                case HitTypes.falseNegative:
                    a.falseNs++;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Given two boolean answers, this will return an enum whether event was a true or false positive or negative.
        /// Use as the argument to RecordEvent.
        /// </summary>
        /// <param name="correctAnswer">The intended answer</param>
        /// <param name="givenAnswer">The given answer</param>
        /// <returns>hitTypes corresponding to the type of event calculated</returns>
        public static HitTypes EventType(bool correctAnswer, bool givenAnswer)
        {
            if (correctAnswer == givenAnswer)
            {
                if (correctAnswer == true) return HitTypes.truePositive;
                else return HitTypes.trueNegative;
            }
            else
            {//Answers don't match
                if (correctAnswer == true) return HitTypes.falseNegative;
                else return HitTypes.falsePositive;
            }
        }

        public string Serialize()
        {
            return string.Format("Matrix: {0}, {1}, {2}, {3}|\n", truePs, trueNs, falsePs, falseNs);

        }

        

        public static HitTypes EventType(bool correctAnswer, bool? givenAnswer)
        {
            return ConfusionMatrix.EventType(correctAnswer, givenAnswer ?? false);
        }
        /// <summary>
        /// Accurate prediction, a hit.  Correct identification.
        /// </summary>
        public int TruePositives
        {
            get { return truePs; }
            set { truePs = value; }
        }
        /// <summary>
        /// Accurate rejection, correct rejection.  
        /// </summary>
        public int TrueNegatives
        {
            get { return trueNs; }
            set { trueNs = value; }
        }
        /// <summary>
        /// Miss.  The class it was looking at was a subject it should have identified, and it failed to.
        /// </summary>
        public int FalseNegatives
        {
            get { return falseNs; }
            set { falseNs = value; }
        }
        /// <summary>
        /// False Alarm- there was nothing there, but it attacked anyway.
        /// </summary>
        public int FalsePositives
        {
            get { return falsePs; }
            set { falsePs = value; }
        }
        //Derived Data
        /// <summary>
        /// TP+FN, the number of positives evaluated (correctly or not)
        /// </summary>
        public int totalPositives
        {
            get { return TruePositives + FalseNegatives; }
        }
        /// <summary>
        /// TN+FP, the number of negatives evaluated (correctly or not)
        /// </summary>
        public int totalNegatives
        {
            get { return TrueNegatives + FalsePositives; }
        }
        /// <summary>
        /// TN+FN, the total number of negatives reported
        /// </summary>
        public int totalReportedNegatives
        {
            get { return TrueNegatives + FalseNegatives; }
        }
        /// <summary>
        /// TP+FP, the total number of positives reported
        /// </summary>
        public int totalReportedPositives
        {
            get { return TruePositives + FalsePositives; }
        }
        /// <summary>
        /// True Positive Rate, Aka Hit Rate, TPR, recall (TP/(TP+FN))
        /// </summary>
        public double HitRate
        {
            get { return (double)TruePositives / (double)totalPositives; }
        }
        /// <summary>
        /// True Negative Rate, aka SPC or Specificity
        /// </summary>
        public double Specificity
        {
            get { return (double)TrueNegatives / (double)totalNegatives; }
        }
        /// <summary>
        /// Precision, or Positive Predictive Value
        /// </summary>
        public double Precision
        {
            get { return (double)TruePositives / (double)(totalReportedPositives); }
        }
        /// <summary>
        /// Negative Predictive Value (TN/(TN+FN))
        /// </summary>
        public double NPV
        {
            get { return (double)TrueNegatives / (double)(totalReportedNegatives); }
        }
        /// <summary>
        /// False Positive Rate, fall-out, or FPR (FP/(FP+TN));
        /// </summary>
        public double FallOut
        {
            get { return (double)FalsePositives / totalNegatives; }
        }
        /// <summary>
        /// FalseDiscoveryRate(FP/(FP+TP)=1-PPV)
        /// </summary>
        public double FalseDiscoveryRate
        {
            get { return (double)FalsePositives / (totalReportedPositives); }
        }
        public double MissRate
        {
            get { return (double)FalseNegatives / (totalPositives); }
        }
        /// <summary>
        /// Returns accuracy weighted by relative sizes of 
        /// </summary>
        public double AverageAccuracy
        {
            get { return (HitRate + Specificity) / 2.0; }
        }

        /// <summary>
        /// (TP+TN)/(P+N), rate it got right, overall
        /// </summary>
        public double Accuracy { get { return (double)(truePs + trueNs) / (totalPositives + totalNegatives); } }
        /// <summary>
        /// F1, the harmonic mean of Precision and Sensitivity. 
        /// </summary>
        public double F1 { get { return (double)(truePs * 2) / (2 * truePs + falseNs + falsePs); } }
        /// <summary>
        /// Measure of the quality of binary classifications.  0 is no better than chance, -1 is 100% inaccuracy, 1 is 100% accuracy
        /// Closely related to the chi-squared stat: Abs(MCC) = sqrt(chi^2/n);
        /// </summary>
        public double MatthewsCorrelationCoefficient
        {
            get
            {
                double ret;
                if ((TruePositives + FalsePositives) == 0 || (TruePositives + FalseNegatives) == 0 || (
                    TrueNegatives + FalsePositives) == 0 || (TrueNegatives + FalseNegatives) == 0)  //We'll get an infinity if we divide by zero,
                                                                                                    //which would be out of our range
                    return 0;

                else
                {
                    double num = (double)(TruePositives * TrueNegatives - FalsePositives * FalseNegatives);
                    double denom = (double)(TruePositives + FalsePositives) * (TruePositives + FalseNegatives) * (TrueNegatives + FalsePositives) * (TrueNegatives + FalseNegatives);
                    denom = Math.Sqrt(denom);

                    ret = num / denom;
                }
                return ret;
            }
        }
        public ConfusionMatrix()
        {
            trueNs = truePs = falsePs = falseNs = 0;
        }

        public ConfusionMatrix(int tp, int tn, int fp, int fn)
        {
            truePs = tp;
            trueNs = tn;
            falsePs = fp;
            falseNs = fn;

        }
        /// <summary>
        /// Resets the core variables to 0
        /// </summary>
        public void ResetMatrix()
        {
            trueNs = truePs = falsePs = falseNs = 0;
        }

        /// <summary>
        /// Compares two matrices, using their MCCs.
        /// </summary>
        /// <param name="other">The other other white Matrix</param>
        /// <returns>Inverse of the default double.CompareTo() function</returns>
        int IComparable<ConfusionMatrix>.CompareTo(ConfusionMatrix other)
        {
            return -1 * (MatthewsCorrelationCoefficient.CompareTo(other.MatthewsCorrelationCoefficient));
        }

      
    }
    #endregion
}

