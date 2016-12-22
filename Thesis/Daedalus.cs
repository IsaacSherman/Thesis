using System;
using EvoOptimization;
namespace MyCellNet
{
    internal class Daedalus
    {

        public int MaxGen { get; internal set; }
        public int PopSize { get; internal set; }
        public int RecordInterval { get; internal set; }
        public int InitialComplexityUpperBound { get; internal set; }
        public int MaxCellComplexity { get
            {
                return (int)OptoGlobals.ComplexityCap;
            }
            internal set
            {
                OptoGlobals.ComplexityCap = value;
            }
        }

        public Daedalus()
        {
            MaxGen = 100;
            PopSize = 50;
            RecordInterval = 10;
            InitialComplexityUpperBound = 1;
            MaxCellComplexity = 100;
        }

        internal void ConfigureCellDelegatesForDatabase()
        {
            Cell.SetNumberOfFeatures();
            Chromosome.SetNumberOfClasses();
        }
    }
}