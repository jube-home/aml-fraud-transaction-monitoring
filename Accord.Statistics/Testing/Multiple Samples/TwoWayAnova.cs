// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Accord.Math;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Two-way ANOVA model types.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Analysis_of_variance">
    ///                         Wikipedia, The Free Encyclopedia. Analysis of variance.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public enum TwoWayAnovaModel
    {
        /// <summary>
        ///     Fixed-effects model (Model 1).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The fixed-effects model of analysis of variance, as known as model 1, applies
        ///         to situations in which the experimenter applies one or more treatments to the
        ///         subjects of the experiment to see if the response variable values change.
        ///     </para>
        ///     <para>
        ///         This allows the experimenter to estimate the ranges of response variable values
        ///         that the treatment would generate in the population as a whole.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <a href="http://en.wikipedia.org/wiki/Analysis_of_variance">
        ///                         Wikipedia, The Free Encyclopedia. Analysis of variance.
        ///                     </a>
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        Fixed = 1,

        /// <summary>
        ///     Random-effects model (Model 2).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Random effects models are used when the treatments are not fixed. This occurs when
        ///         the various factor levels are sampled from a larger population. Because the levels
        ///         themselves are random variables, some assumptions and the method of contrasting the
        ///         treatments differ from ANOVA model 1.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <a href="http://en.wikipedia.org/wiki/Analysis_of_variance">
        ///                         Wikipedia, The Free Encyclopedia. Analysis of variance.
        ///                     </a>
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        Random = 2,

        /// <summary>
        ///     Mixed-effects models (Model 3).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         A mixed-effects model contains experimental factors of both fixed and random-effects
        ///         types, with appropriately different interpretations and analysis for the two types.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <a href="http://en.wikipedia.org/wiki/Analysis_of_variance">
        ///                         Wikipedia, The Free Encyclopedia. Analysis of variance.
        ///                     </a>
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        Mixed = 3
    }

    /// <summary>
    ///     Two-way Analysis of Variance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The two-way ANOVA is an extension of the one-way ANOVA for two independent
    ///         variables. There are three classes of models which can also be used in the
    ///         analysis, each of which determining the interpretation of the independent
    ///         variables in the analysis.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Analysis_of_variance">
    ///                         Wikipedia, The Free Encyclopedia. Analysis of variance.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.smi.hst.aau.dk/~cdahl/BiostatPhD/ANOVA.pdf">
    ///                         Carsten Dahl Mørch, ANOVA. Aalborg Universitet. Available on:
    ///                         http://www.smi.hst.aau.dk/~cdahl/BiostatPhD/ANOVA.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <see cref="TwoWayAnovaModel" />
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Testing\TwoWayAnovaTest.cs" region="doc_test" />
    /// </example>
    [Serializable]
    public partial class TwoWayAnova : IAnova
    {
        private double[] aMean;
        private double[] bMean;
        private double[,] cellMeans;
        private double totalMean;

        /// <summary>
        ///     Constructs a new <see cref="TwoWayAnova" />.
        /// </summary>
        /// <param name="samples">The samples.</param>
        /// <param name="firstFactorLabels">The first factor labels.</param>
        /// <param name="secondFactorLabels">The second factor labels.</param>
        /// <param name="type">The type of the analysis.</param>
        public TwoWayAnova(double[] samples, int[] firstFactorLabels, int[] secondFactorLabels,
            TwoWayAnovaModel type = TwoWayAnovaModel.Mixed)
        {
            FirstFactorSamples = firstFactorLabels.Max() + 1;
            SecondFactorSamples = secondFactorLabels.Max() + 1;

            var groups = new List<double>[FirstFactorSamples][];
            for (var i = 0; i < groups.Length; i++)
            {
                groups[i] = new List<double>[SecondFactorSamples];
                for (var j = 0; j < groups[i].Length; j++)
                {
                    groups[i][j] = new List<double>();
                }
            }

            for (var i = 0; i < samples.Length; i++)
            {
                var f = firstFactorLabels[i];
                var s = secondFactorLabels[i];

                groups[f][s].Add(samples[i]);
            }

            // Transform into array
            var sets = new double[FirstFactorSamples][][];
            for (var i = 0; i < sets.Length; i++)
            {
                sets[i] = new double[SecondFactorSamples][];
                for (var j = 0; j < sets[i].Length; j++)
                {
                    sets[i][j] = groups[i][j].ToArray();
                }
            }

            Replications = sets[0][0].Length;

            // Assert equal number of replicates
            for (var i = 0; i < sets.Length; i++)
            {
                for (var j = 0; j < sets[i].Length; j++)
                {
                    if (sets[i][j].Length != Replications)
                    {
                        throw new ArgumentException("Samples do not have the same number of replicates.", "samples");
                    }
                }
            }

            initialize(sets, type);
        }

        /// <summary>
        ///     Constructs a new <see cref="TwoWayAnova" />.
        /// </summary>
        /// <param name="samples">The samples in grouped form.</param>
        /// <param name="type">The type of the analysis.</param>
        public TwoWayAnova(double[,,] samples, TwoWayAnovaModel type = TwoWayAnovaModel.Mixed)
            : this(jagged(samples), type)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="TwoWayAnova" />.
        /// </summary>
        /// <param name="samples">The samples in grouped form.</param>
        /// <param name="type">The type of the analysis.</param>
        public TwoWayAnova(double[][][] samples, TwoWayAnovaModel type = TwoWayAnovaModel.Mixed)
        {
            FirstFactorSamples = samples.Length;
            SecondFactorSamples = samples[0].Length;
            Replications = samples[0][0].Length;
            Observations = FirstFactorSamples * SecondFactorSamples * Replications;

            // Assert equal number of replicates
            for (var i = 0; i < samples.Length; i++)
            {
                for (var j = 0; j < samples[i].Length; j++)
                {
                    if (samples[i][j].Length != Replications)
                    {
                        throw new ArgumentException("Samples do not have the same number of replicates.", "samples");
                    }
                }
            }

            initialize(samples, type);
        }

        /// <summary>
        ///     Gets the number of observations in the sample.
        /// </summary>
        public int Observations { get; private set; }

        /// <summary>
        ///     Gets the number of samples presenting the first factor.
        /// </summary>
        public int FirstFactorSamples { get; private set; }

        /// <summary>
        ///     Gets the number of samples presenting the second factor.
        /// </summary>
        public int SecondFactorSamples { get; private set; }

        /// <summary>
        ///     Gets the number of replications of each factor.
        /// </summary>
        public int Replications { get; private set; }

        /// <summary>
        ///     Gets or sets the variation sources obtained in the analysis.
        /// </summary>
        /// <value>The variation sources for the data.</value>
        public TwoWayAnovaVariationSources Sources { get; private set; }

        /// <summary>
        ///     Gets or sets the type of the model.
        /// </summary>
        /// <value>The type of the model.</value>
        public TwoWayAnovaModel ModelType { get; private set; }

        /// <summary>
        ///     Gets the ANOVA results in the form of a table.
        /// </summary>
        public AnovaSourceCollection Table { get; private set; }

        private void initialize(double[][][] samples, TwoWayAnovaModel type)
        {
            // References:
            // -  http://www.smi.hst.aau.dk/~cdahl/BiostatPhD/ANOVA.pdf

            ModelType = type;
            Observations = FirstFactorSamples * SecondFactorSamples * Replications;


            // Step 1. Initialize all degrees of freedom
            var cellDegreesOfFreedom = FirstFactorSamples * SecondFactorSamples - 1;
            var aDegreesOfFreedom = FirstFactorSamples - 1;
            var bDegreesOfFreedom = SecondFactorSamples - 1;
            var abDegreesOfFreedom = cellDegreesOfFreedom - aDegreesOfFreedom - bDegreesOfFreedom;
            var errorDegreesOfFreedom = FirstFactorSamples * SecondFactorSamples * (Replications - 1);
            var totalDegreesOfFreedom = Observations - 1;


            var sum = computeCellMeans(samples);


            // Step 2. Calculate the total mean (grand mean)
            totalMean = sum / (FirstFactorSamples * SecondFactorSamples);


            computeFactorMeans(samples);


            var totalSumOfSquares = computeTotalSumOfSquares(samples);

            var cellSumOfSquares = computeCellSumOfSquares();

            var errorSumOfSquares = computeErrorSumOfSquares(samples);

            double aSumOfSquares, bSumOfSquares;
            computeFactorSumsOfSquares(out aSumOfSquares, out bSumOfSquares);


            // Step 9. Compute interaction sum of squares
            var abSumOfSquares = cellSumOfSquares - aSumOfSquares - bSumOfSquares;

            // Step 10. Compute mean squares
            var aMeanSquares = aSumOfSquares / aDegreesOfFreedom;
            var bMeanSquares = bSumOfSquares / bDegreesOfFreedom;
            var abMeanSquares = abSumOfSquares / abDegreesOfFreedom;
            var errorMeanSquares = errorSumOfSquares / errorDegreesOfFreedom;

            FTest aSignificance, bSignificance, abSignificance;
            createSignificances(type, aMeanSquares, bMeanSquares, abMeanSquares, errorMeanSquares,
                aDegreesOfFreedom, bDegreesOfFreedom, abDegreesOfFreedom, errorDegreesOfFreedom,
                out aSignificance, out bSignificance, out abSignificance);

            createSources(cellSumOfSquares, aSumOfSquares, bSumOfSquares, abSumOfSquares,
                errorSumOfSquares, totalSumOfSquares,
                cellDegreesOfFreedom, aDegreesOfFreedom, bDegreesOfFreedom, abDegreesOfFreedom,
                errorDegreesOfFreedom, totalDegreesOfFreedom,
                aMeanSquares, bMeanSquares, abMeanSquares, errorMeanSquares,
                aSignificance, bSignificance, abSignificance);
        }

        private static double[][][] jagged(double[,,] samples)
        {
            var m = samples.GetLength(0);
            var n = samples.GetLength(1);
            var p = samples.GetLength(2);
            var jagged = new double[m][][];
            for (var i = 0; i < jagged.Length; i++)
            {
                jagged[i] = new double[n][];
                for (var j = 0; j < jagged[i].Length; j++)
                {
                    jagged[i][j] = new double[p];
                    for (var k = 0; k < jagged[i][j].Length; k++)
                    {
                        jagged[i][j][k] = samples[i, j, k];
                    }
                }
            }

            return jagged;
        }
    }
}