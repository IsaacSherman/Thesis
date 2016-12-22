using EvoOptimization;
namespace MyCellNet
{
    internal static class DelegateBuilder
    {
        public static Cell.DataDelegate SimpleLookup(int index)
        {
            Cell.DataDelegate ret = (object data) => ((double[])data)[index];
            return ret;
        }
        public static double QuickTest()
        {
            double[] data = { 1, 2, 3, 4, 5 };
            object bort = data;
            Cell.DataDelegate foo = DelegateBuilder.SimpleLookup(3);
            double result = foo(bort);
            return result;
        }
    }
}