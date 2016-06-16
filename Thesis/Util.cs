using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MyUtils
{

    /// <summary>
    /// A collection of extensions to various .Net structures, primarily BitArray for genes.
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Returns a smooth array (all internal lists have the same number of elements- check before calling)
        /// </summary>
        /// <typeparam name="T">Any class</typeparam>
        /// <param name="list">A list of lists</param>
        /// <returns></returns>
        public static T[,] TwoDimListToSmoothArray<T>(List<List<T>> list)
        {
            T[,] ret = new T[list.Count, list[0].Count];
            for(int i = 0; i < list.Count; ++i)
            {
                for(int j = 0; j < list[0].Count; ++j)
                {
                    ret[i, j] = list[i][j];
                }
            }
            return ret;
        }
        /// <summary>
        /// Returns a jagged array from a 2d list, should be safer and probably faster than the smooth version
        /// </summary>
        /// <typeparam name="T">Any class</typeparam>
        /// <param name="list">a list of lists</param>
        /// <returns></returns>
        public static T[][] TwoDimListToJaggedArray<T>(List<List<T>> list)
        {
            T[][] ret = new T[list.Count][];
            for(int i = 0; i < list.Count; ++i)
            {
                ret[i] = list[i].ToArray();
            }
            return ret;
        }

        public static List<List<T>> ArrayTo2dList<T>(T[][] array)
        {
            List<List<T>> ret = new List<List<T>>();
            for(int i = 0; i < array.GetUpperBound(0); ++i)
            {
                ret.Add(new List<T>(array[i]));
            }
            return ret;
        }
        public static List<List<T>> ArrayTo2dList<T>(T[,] array)
        {
            List<List<T>> ret = new List<List<T>>();
            for(int i = 0; i < array.GetUpperBound(0); ++i)
            {
                List<T> temp = new List<T>();
                for(int j = 0; j < array.GetUpperBound(1); ++j)
                {
                    temp.Add(array[i, j]);
                }
                ret.Add(temp);
            }
            return ret;
        }

        public static T[] Flatten2dArray<T>(T[,] array)
        {
            T[] ret;
            int dim0Upper = array.GetUpperBound(0), dim1Upper = array.GetUpperBound(1), mainDim, flatDim, len;
            if (dim0Upper == 1 || dim1Upper == 1)//at least one dimension is 1
            {
                if (dim1Upper == 1)
                {
                    mainDim = 0;
                    flatDim = 1;
                }
                else
                {
                    mainDim = 1;
                    flatDim = 0;
                }
                len = array.GetUpperBound(mainDim);
                ret = new T[len];
                if (dim1Upper == 1)
                { //Column Vector
                    for (int i = 0; i < len; ++i)
                    {
                        ret[i] = array[i, 0];
                    }
                }
                else//row vector
                {
                    for (int i = 0; i < len; ++i)
                    {
                        ret[i] = array[0, i];
                    }
                }
                return ret;
            }
            else throw new ArgumentException("Neither dimension of the array has length of 1");


        }

        public static T[] Range<T>(this T[] a, int start, int end)
        {
            if (start < 0 || end >= a.Length)
            {
                throw (new ArgumentException("start is sub-zero or end is out of bounds"));

            }
            else
            {
                T[] ret = new T[end - start];
                int len = end - start;
                for (int i = 0; i < len; i++)
                {
                    ret[i] = a[i + start];
                }
                return ret;
            }
        }
        /// <summary>
        /// Returns the subarray between start and end.  ints start and end must be 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static BitArray Range(this BitArray a, uint start, uint end)
        {
            if (end > a.Length || start >= a.Length || end < start)
            {
                throw (new ArgumentException("Either start and/or end is out of bounds"));
            }
            else
            {
                BitArray ret = new BitArray((int)(end - start));
                int len = (int)(end - start);
                for (int i = 0; i < len; i++)
                {
                    ret[i] = a[i + (int)start];
                }
                return ret;
            }
        }
        public static string BitsToString(this BitArray a)
        {
            StringBuilder ret = new StringBuilder(a.Length);
            for (int i = 0; i < a.Length; i++)
                ret.Append(a[i] ? "1" : "0");
            return ret.ToString();
        }
        public static string BitsToString(this BitArray a, uint start, uint end)
        {
            BitArray ret = Range(a, start, end);
            return ret.BitsToString();
        }
        /// <summary>
        /// Recursively generates an int from a binary string
        /// Bin must be formatted entirely of '1's and '0's or an ArgumentException will be raised.  
        /// </summary>
        /// <param name="bin">a string using only 1 or 0 </param>
        /// <returns> an int corresponding to the value of bin</returns>
        public static int BinaryStringToInt(this string bin)
        {

            return BinaryStringToInt(bin, 0);

        }
        public static int BinaryStringToInt(this string bin, int power)
        {
            checkIfBinaryString(bin);
            return binaryStringToInt(bin, power);

        }
        private static int binaryStringToInt(string bin, int power)
        {

            int end = bin.Length - 1;
            int value = bin[end] == '1' ? 1 : 0;
            value <<= power;

            if (end == 0)//no more chars to eat
            {
                return value;
            }
            else
            {
                string newBin = bin.Substring(0, bin.Length - 1);//eat the char we just used
                return (value + binaryStringToInt(newBin, power + 1));
            }
        }
        private static bool checkIfBinaryString(this string bin)
        {
            if (bin == null) throw new ArgumentException("null string passed");
            for (int i = 0; i < bin.Length; i++)
                if (!(bin[i] == '1' || bin[i] == '0'))
                    throw new ArgumentException("Not a binary string. Non binary char found at position " + i.ToString());
            return true;
        }
        /// <summary>
        /// Recursively generates a double from a binary string, with the LSB taking on a value of 2^0.
        /// Bin must be formatted entirely of '1's and '0's or an ArgumentException will be raised.  
        /// </summary>
        /// <param name="bin"></param>
        /// <returns></returns>
        public static double BinaryStringToDouble(this string bin)
        {
            return BinaryStringToDouble(bin, 0);
        }
        /// <summary>
        /// Takes a binary string, and reads from right to left, converting it recursively to double.
        /// Bin must be formatted entirely of '1's and '0's or an ArgumentException will be raised.  
        /// </summary>
        /// <param name="bin">The string which m</param>
        /// <param name="power">The power to raise the least significant bit to.</param>
        /// <returns></returns>
        public static double BinaryStringToDouble(this string bin, int power)
        {
            checkIfBinaryString(bin);
            return binaryStringToDouble(bin, power);
        }

        private static double binaryStringToDouble(this string bin, int power)
        {
            int end = bin.Length - 1;
            double value = bin[end] == '1' ? Math.Pow(2.0d, power) : 0.0d;
            if (end == 0) return value;
            else
                return value + binaryStringToDouble(bin.Substring(0, bin.Length - 1), power + 1);
        }
        private static BitArray diff2Lines(this string line1, string line2)
        {
            int min = Math.Min(line1.Length, line2.Length);
            BitArray ret = new BitArray(min);
            for (int i = 0; i < min; ++i)
            {
                if (line1[i] != line2[i]) ret[i] = true;
                else ret[i] = false;
            }
            return ret;
        }

        public static void switchTargets<T>(T a, T b, ref T target, ref T notTarget)
        {
            if (target.Equals(a))
            {
                target = b;
                notTarget = a;
            }
            else
            {
                target = a;
                notTarget = b;
            }
        }

        public static double AverageIgnoringNAN(this double[] d)
        {
            double sum = 0, count = 0;
            foreach (double dub in d)
            {
                if (Double.IsNaN(dub)) continue;
                sum += dub;
                count += 1;
            }
            return sum / count;
        }


        public static double AverageIgnoringNAN(this float[] d)
        {
            double sum = 0, count = 0;
            foreach (double dub in d)
            {
                if (Double.IsNaN(dub)) continue;
                sum += dub;
                count += 1;
            }
            return sum / count;
        }

        public static double StandardDeviationIgnoringNAN(this double[] d)
        {

            double ret = 0, sum = 0, count = 0;
            double average = d.AverageIgnoringNAN();
            for (int i = 0; i < d.Length; ++i)
            {
                if (Double.IsNaN( d[i])) 
                    continue;
                sum += ((d[i] - average) * (d[i] - average));
                count += 1;
            }
            if (sum == 0 && count == 0) ret = 0;
            else
            {

                ret = Math.Sqrt(sum/count);
            }
            if (Double.IsNaN(ret)) System.Diagnostics.Debugger.Break();
            return ret;
        }


        public static double StandardDeviationIgnoringNAN(this float[] d)
        {

            double ret = 0, sum = 0, count = 0;
            double average = d.AverageIgnoringNAN();
            for (int i = 0; i < d.Length; ++i)
            {
                if (Double.IsNaN(d[i]))
                    continue;
                sum += ((d[i] - average) * (d[i] - average));
                count += 1;
            }
            sum /= count;
            ret = Math.Sqrt(sum);
            if (Double.IsNaN(ret)) System.Diagnostics.Debugger.Break();
            return ret;
        }


        public static double StandardDeviation(this double[] d)
        {
            double ret = 0, sum = 0;
            double average = d.Average();
            for (int i = 0; i < d.Length; ++i)
            {
                sum += ((d[i] - average) * (d[i] - average));
            }
            sum /= d.Length;
            ret = Math.Sqrt(sum);
            return ret;
        }


        public static double StandardDeviation(this float[] d)
        {
            double ret = 0, sum = 0;
            double average = d.Average();
            for (int i = 0; i < d.Length; ++i)
            {
                sum += ((d[i] - average) * (d[i] - average));
            }
            sum /= d.Length;
            ret = Math.Sqrt(sum);
            return ret;

        }
        /// <summary>
        /// Returns the union of sets a and b.  If both sets are empty, the empty set is returned.
        /// </summary>
        /// <param name="a">Set A</param>
        /// <param name="b">Set B</param>
        /// <returns>ArrayList representing the union of a and b.</returns>
        public static ArrayList Union(this ArrayList a, ArrayList b)
        {
            ArrayList ret = new ArrayList();
            for (int i = 0; i < a.Count; ++i) ret.Add(a[i]);//if(!ret.Contains(a[i])) 
            for (int i = 0; i < b.Count; ++i)
            {
                if (!a.Contains(b[i]))
                    ret.Add(b[i]);
            }
            return ret;
        }
        /// <summary>
        /// Intersects two sets.  In set theory, if you intersect with the empty set, you end up with
        /// the universal set.  Since a high-order infinite set is hardly useful in programming,
        /// if you intersect with an empty set, you can expect everything in the non-empty set to
        /// be returned.  If both are empty, the empty set is returned.
        /// </summary>
        /// <param name="a">Set A</param>
        /// <param name="b">Set B</param>
        /// <returns>ArrayList representing the interesection of a and b.</returns>
        public static ArrayList Intersect(this ArrayList a, ArrayList b)
        {
            ArrayList ret = new ArrayList();
            if (b.Count == 0) ret.AddRange(a);
            if (a.Count == 0) ret.AddRange(b);
            for (int i = 0; i < b.Count; ++i)
            {
                if (a.Contains(b[i])) ret.Add(b[i]);
            }
            return ret;
        }
        public static Regex MultiRegex(this Regex r, string[] seeds, RegexOptions options = RegexOptions.None)
        {
            int n = seeds.Length;

            string seed = @"";
            for (int i = 0; i < n; i++)
            {
                seed += seeds[i];
                seed += " | ";
            }

            char[] trim = new char[2] { '|', ' ' };
            seed = seed.TrimEnd(trim);
            return new Regex(seed, options);
        }

        public static void Swap<T>(this IList<T> l, int a, int b)
        {
            T temp = l[b];
            l[b] = l[a];
            l[a] = temp;
        }

        public static void FisherYatesShuffle<T>(this IList<T> source)
        {
            int n = source.Count;
            Random RNG = new Random();
            List<T> l = new List<T>(n);
            for (int i = 0; i < n; ++i)
            {
                int j = RNG.Next(i, n);
                Swap(source, i, j);
            }
        }
        public static BitArray BitsFromInt(int size, int num)
        {
            BitArray ret = new BitArray(size);
            String bits = StringFromInt(size, num);
            for (int i = 0; i < size; ++i)
            {
                ret[i] = (bits[i] == '1' ? true : false);
            }
            return ret;


        }

        private static string StringFromInt(int size, int num)
        {
            StringBuilder ret = new StringBuilder(new String('0', size), size);
            
            int temp = 1 << --size;
            for(int i = size; i >= 0; --i){
                ret[size - i]= (num >= temp? '1':'0');
                num -= temp * (ret[size - i] - '0');
                temp >>= 1;
            }
        
            
            return ret.ToString();
        }


        /// <summary>
        /// Takes a list of strings and joins them together with a delimeter.  If trim is true, the final delimeter will 
        /// be omitted.
        /// </summary>
        /// <param name="Delim">The separating character</param>
        /// <param name="trim">Whether to omit the last delimeter</param>
        /// <param name="l"> The list of strings</param>
        /// <returns>null if passed an empty list, otherwise l[0]+Delim+l[1]+Delim+...</returns>
        internal static string Stitch(string Delim, bool trim = true, params string[] l)
        {
            if (l == null) return null;
            StringBuilder ret = new StringBuilder(l.Length * 2);
            int stop = l.Length;
            if(trim) --stop;
            for (int i = 0; i < stop; ++i)
            {
                if (l[i] == null) continue;
                ret.Append(l[i]);
                ret.Append(Delim);
            }
            if (trim) ret.Append(l[stop]);
            return ret.ToString();
        }


        internal static string IntersectStrings(string s1, string s2)
        {
            StringBuilder result = new StringBuilder();
            int len = Math.Min(s1.Length, s2.Length);
            for (int i = 0; i < len; ++i)
            {
                if (s1[i] == s2[i]) result.Append(s1[i]);
            }
            return result.ToString();
        }


    }
}
