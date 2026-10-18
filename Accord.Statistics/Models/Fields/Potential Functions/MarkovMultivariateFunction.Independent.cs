using System.Collections.Generic;
using Accord.Diagnostics;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Fields.Features;
using Accord.Statistics.Models.Fields.Functions.Specialized;

namespace Accord.Statistics.Models.Fields.Functions
{
#pragma warning disable 612, 618
    public sealed partial class MarkovMultivariateFunction
    {
        private static int[][] createLookupTable(int states, int dimension)
        {
            var lookupTable = new int[states][];
            for (var i = 0; i < lookupTable.Length; i++)
            {
                lookupTable[i] = new int[dimension];
            }

            return lookupTable;
        }

        private void addClassPrior(ClassFeatureSet features, int c, double prior)
        {
            // Create features for class labels
            features.ClassParams.Add(System.Math.Log(prior));
            features.ClassFeatures.Add(new OutputFeature<double[]>(this, c, c));
        }

        private void addInitialStateFeatures(ClassFeatureSet features, int c, int states, double[] logInitial)
        {
            // Create features for initial state probabilities
            for (var i = 0; i < states; i++)
            {
                features.EdgeParams.Add(logInitial[i]);
                features.EdgeFeatures.Add(new InitialFeature<double[]>(this, c, i));
            }
        }

        private void addIndependentEmissionFeatures(ClassFeatureSet features, int c, int states,
            Independent[] emissions, int[][] lookupTable)
        {
            var stateParams = features.StateParams;
            var stateFeatures = features.StateFeatures;

            var position = 0;

            // Create features emission probabilities
            for (var i = 0; i < states; i++)
            {
                for (var d = 0; d < emissions[i].Components.Length; d++)
                {
                    var distribution = emissions[i].Components[d];

                    var normal = distribution as NormalDistribution;
                    if (normal != null)
                    {
                        var var = normal.Variance;
                        var mean = normal.Mean;

                        // Occupancy
                        stateParams.Add(-0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var));
                        stateFeatures.Add(new OccupancyFeature<double[]>(this, c, i));
                        lookupTable[i][d] = position;
                        position++;

                        // 1st Moment (x)
                        stateParams.Add(mean / var);
                        stateFeatures.Add(new MultivariateFirstMomentFeature(this, c, i, d));
                        position++;

                        // 2nd Moment (x²)
                        stateParams.Add(-1.0 / (2.0 * var));
                        stateFeatures.Add(new MultivariateSecondMomentFeature(this, c, i, d));
                        position++;

                        continue;
                    }

                    var discrete = distribution as GeneralDiscreteDistribution;

                    if (discrete != null)
                    {
                        lookupTable[i][d] = position;

                        for (var k = 0; k < discrete.Frequencies.Length; k++)
                        {
                            stateParams.Add(System.Math.Log(discrete.Frequencies[k]));
                            stateFeatures.Add(new MultivariateEmissionFeature(this, c, i, k, d));
                            position++;
                        }
                    }
                }
            }
        }

        private void assignWeightsAndFeatures(IndependentLayout layout)
        {
            Debug.Assert(layout.FactorIndex == layout.FactorParams.Count);
            Debug.Assert(layout.FactorIndex == layout.FactorFeatures.Count);

            Weights = layout.FactorParams.ToArray();
            Features = layout.FactorFeatures.ToArray();
        }

        private void createIndependentFactor(IndependentLayout layout, int c, int states)
        {
            Factors[c] = new MarkovIndependentFactor(this, states, c, layout.LookupTables[c],
                classIndex: layout.ClassOffset[c], classCount: layout.ClassCount[c], // 1. classes
                edgeIndex: layout.EdgeOffset[c], edgeCount: layout.EdgeCount[c], // 2. edges
                stateIndex: layout.StateOffset[c], stateCount: layout.StateCount[c]); // 3. states
        }

        private sealed class ClassFeatureSet
        {
            public readonly List<IFeature<double[]>> ClassFeatures = new();

            public readonly List<double> ClassParams = new();
            public readonly List<IFeature<double[]>> EdgeFeatures = new();

            public readonly List<double> EdgeParams = new();
            public readonly List<IFeature<double[]>> StateFeatures = new();
            public readonly List<double> StateParams = new();
        }

        private sealed class IndependentLayout
        {
            public readonly int[] ClassCount;

            public readonly int[] ClassOffset;
            public readonly int[] EdgeCount;
            public readonly int[] EdgeOffset;
            public readonly List<IFeature<double[]>> FactorFeatures = new();
            public readonly List<double> FactorParams = new();

            public readonly int[][][] LookupTables;
            public readonly int[] StateCount;
            public readonly int[] StateOffset;
            public int FactorIndex;

            public IndependentLayout(int classes)
            {
                ClassOffset = new int[classes];
                EdgeOffset = new int[classes];
                StateOffset = new int[classes];
                ClassCount = new int[classes];
                EdgeCount = new int[classes];
                StateCount = new int[classes];

                LookupTables = new int[classes][][];
            }

            public void AddClass(int c, ClassFeatureSet features, int[][] lookupTable)
            {
                ClassOffset[c] = FactorIndex;
                EdgeOffset[c] = FactorIndex + features.ClassParams.Count;
                StateOffset[c] = FactorIndex + features.ClassParams.Count + features.EdgeParams.Count;

                ClassCount[c] = features.ClassParams.Count;
                EdgeCount[c] = features.EdgeParams.Count;
                StateCount[c] = features.StateParams.Count;

                LookupTables[c] = lookupTable;


                // 1. classes
                FactorFeatures.AddRange(features.ClassFeatures);
                FactorParams.AddRange(features.ClassParams);

                // 2. edges
                FactorFeatures.AddRange(features.EdgeFeatures);
                FactorParams.AddRange(features.EdgeParams);

                // 3. states
                FactorFeatures.AddRange(features.StateFeatures);
                FactorParams.AddRange(features.StateParams);

                FactorIndex += features.ClassParams.Count + features.StateParams.Count + features.EdgeParams.Count;
            }
        }
    }
}