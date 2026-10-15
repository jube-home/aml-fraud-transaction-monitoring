// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Jube Holdings Limited, 2026
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Accord.MachineLearning.Bayes
{
    public class AveragedOneDependenceEstimatorsLearning :
        ISupervisedLearning<AveragedOneDependenceEstimators, double[], int>,
        IParallel
    {
        public AveragedOneDependenceEstimatorsLearning()
        {
            ParallelOptions = new ParallelOptions();
            Bins = 5;
            Smoothing = 1;
            MinimumSupport = 1;
            Weighted = false;
        }

        public AveragedOneDependenceEstimators Model { get; private set; }

        public int Bins { get; set; }

        public double Smoothing { get; set; }

        public double MinimumSupport { get; set; }

        public bool Weighted { get; set; }
        public ParallelOptions ParallelOptions { get; set; }

        public CancellationToken Token
        {
            get => ParallelOptions.CancellationToken;
            set => ParallelOptions.CancellationToken = value;
        }

        public AveragedOneDependenceEstimators Learn(double[][] x, int[] y, double[] weights = null)
        {
            int inputs, classes;
            DiscreteBayesTools.CheckArguments(x, y, weights, out inputs, out classes);
            DiscreteBayesTools.Validate(Bins, Smoothing);
            if (double.IsNaN(MinimumSupport) || MinimumSupport < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(MinimumSupport),
                    "The minimum support must be a number that is not negative.");
            }

            var options = ParallelOptions;
            var token = options.CancellationToken;
            var alpha = Smoothing;
            var weighted = Weighted;

            var w = DiscreteBayesTools.Weights(weights, x.Length);
            var edges = DiscreteBayesTools.LearnBinEdges(x, inputs, Bins, options);
            var states = DiscreteBayesTools.NumberOfStates(edges);
            var columns = DiscreteBayesTools.DiscretizeColumns(x, edges);
            token.ThrowIfCancellationRequested();

            var priors = DiscreteBayesTools.SmoothedPriors(DiscreteBayesTools.ClassCounts(y, w, classes), alpha);

            var conditionals = new double[inputs][];
            var support = new double[inputs][];
            var estimatorWeights = new double[inputs];
            var pairs = new double[inputs][][];

            DiscreteBayesTools.For(inputs, options, i =>
            {
                conditionals[i] = DiscreteBayesTools.ClassConditional(columns[i], states[i], y, w, classes, alpha);

                var valueSupport = new double[states[i]];
                for (var r = 0; r < x.Length; r++)
                {
                    valueSupport[columns[i][r]] += w[r];
                }

                support[i] = valueSupport;

                estimatorWeights[i] = weighted
                    ? DiscreteBayesTools.MutualInformation(columns[i], states[i], y, classes, w)
                    : 1;
            });

            DiscreteBayesTools.For(inputs, options, i =>
            {
                var row = new double[inputs][];
                for (var j = 0; j < inputs; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    token.ThrowIfCancellationRequested();
                    var size = DiscreteBayesTools.CheckedSize((long)classes * states[i] * states[j],
                        "super-parent " + i + " and feature " + j);
                    var table = new double[size];
                    var ci = columns[i];
                    var cj = columns[j];
                    for (var r = 0; r < x.Length; r++)
                    {
                        table[((long)y[r] * states[i] + ci[r]) * states[j] + cj[r]] += w[r];
                    }

                    DiscreteBayesTools.Normalize(table, states[j], alpha);
                    row[j] = table;
                }

                pairs[i] = row;
            });

            token.ThrowIfCancellationRequested();
            Model = new AveragedOneDependenceEstimators(classes, MinimumSupport, weighted, edges, states, priors,
                conditionals, pairs, support, estimatorWeights);
            return Model;
        }
    }
}