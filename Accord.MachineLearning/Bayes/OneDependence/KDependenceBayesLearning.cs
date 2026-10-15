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
    public class KDependenceBayesLearning :
        ISupervisedLearning<KDependenceBayes, double[], int>,
        IParallel
    {
        public KDependenceBayesLearning()
        {
            ParallelOptions = new ParallelOptions();
            K = 2;
            Bins = 5;
            Smoothing = 1;
        }

        public KDependenceBayes Model { get; private set; }

        public int K { get; set; }

        public int Bins { get; set; }

        public double Smoothing { get; set; }
        public ParallelOptions ParallelOptions { get; set; }

        public CancellationToken Token
        {
            get => ParallelOptions.CancellationToken;
            set => ParallelOptions.CancellationToken = value;
        }

        public KDependenceBayes Learn(double[][] x, int[] y, double[] weights = null)
        {
            int inputs, classes;
            DiscreteBayesTools.CheckArguments(x, y, weights, out inputs, out classes);
            DiscreteBayesTools.Validate(Bins, Smoothing);
            if (K < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(K), "The number of parents K must not be negative.");
            }

            var options = ParallelOptions;
            var token = options.CancellationToken;
            var alpha = Smoothing;
            var k = K;

            var w = DiscreteBayesTools.Weights(weights, x.Length);
            var edges = DiscreteBayesTools.LearnBinEdges(x, inputs, Bins, options);
            var states = DiscreteBayesTools.NumberOfStates(edges);
            var columns = DiscreteBayesTools.DiscretizeColumns(x, edges);
            token.ThrowIfCancellationRequested();

            var priors = DiscreteBayesTools.SmoothedPriors(DiscreteBayesTools.ClassCounts(y, w, classes), alpha);

            var classInformation = new double[inputs];
            DiscreteBayesTools.For(inputs, options, j =>
                classInformation[j] = DiscreteBayesTools.MutualInformation(columns[j], states[j], y, classes, w));

            var order = new int[inputs];
            for (var j = 0; j < inputs; j++)
            {
                order[j] = j;
            }

            Array.Sort(order, (a, b) =>
            {
                var compare = classInformation[b].CompareTo(classInformation[a]);
                return compare != 0 ? compare : a.CompareTo(b);
            });

            var position = new int[inputs];
            for (var p = 0; p < inputs; p++)
            {
                position[order[p]] = p;
            }

            var parents = new int[inputs][];
            DiscreteBayesTools.For(inputs, options, p =>
            {
                var j = order[p];
                var count = System.Math.Min(k, p);
                if (count == 0)
                {
                    parents[j] = new int[0];
                    return;
                }

                var candidates = new int[p];
                var information = new double[inputs];
                for (var q = 0; q < p; q++)
                {
                    var i = order[q];
                    candidates[q] = i;
                    information[i] = DiscreteBayesTools.ConditionalMutualInformation(
                        columns[j], states[j], columns[i], states[i], y, classes, w);
                }

                Array.Sort(candidates, (a, b) =>
                {
                    var compare = information[b].CompareTo(information[a]);
                    return compare != 0 ? compare : position[a].CompareTo(position[b]);
                });

                var chosen = new int[count];
                Array.Copy(candidates, chosen, count);
                parents[j] = chosen;
            });

            var conditionals = new double[inputs][];
            DiscreteBayesTools.For(inputs, options, j =>
            {
                var parentList = parents[j];
                long combinations = 1;
                for (var p = 0; p < parentList.Length; p++)
                {
                    combinations = DiscreteBayesTools.CheckedSize(combinations * states[parentList[p]], "feature " + j);
                }

                var size = DiscreteBayesTools.CheckedSize(classes * combinations * states[j], "feature " + j);
                var table = new double[size];
                for (var i = 0; i < x.Length; i++)
                {
                    long parentIndex = 0;
                    for (var p = 0; p < parentList.Length; p++)
                    {
                        parentIndex = parentIndex * states[parentList[p]] + columns[parentList[p]][i];
                    }

                    table[(y[i] * combinations + parentIndex) * states[j] + columns[j][i]] += w[i];
                }

                DiscreteBayesTools.Normalize(table, states[j], alpha);
                conditionals[j] = table;
            });

            token.ThrowIfCancellationRequested();
            Model = new KDependenceBayes(k, classes, edges, states, order, parents, priors, conditionals);
            return Model;
        }
    }
}