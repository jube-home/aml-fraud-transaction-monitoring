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

using System.Threading;
using System.Threading.Tasks;

namespace Accord.MachineLearning.Bayes
{
    public class HiddenNaiveBayesLearning :
        ISupervisedLearning<HiddenNaiveBayes, double[], int>,
        IParallel
    {
        public HiddenNaiveBayesLearning()
        {
            ParallelOptions = new ParallelOptions();
            Bins = 5;
            Smoothing = 1;
        }

        public HiddenNaiveBayes Model { get; private set; }

        public int Bins { get; set; }

        public double Smoothing { get; set; }
        public ParallelOptions ParallelOptions { get; set; }

        public CancellationToken Token
        {
            get => ParallelOptions.CancellationToken;
            set => ParallelOptions.CancellationToken = value;
        }

        public HiddenNaiveBayes Learn(double[][] x, int[] y, double[] weights = null)
        {
            int inputs, classes;
            DiscreteBayesTools.CheckArguments(x, y, weights, out inputs, out classes);
            DiscreteBayesTools.Validate(Bins, Smoothing);

            var options = ParallelOptions;
            var token = options.CancellationToken;
            var alpha = Smoothing;

            var w = DiscreteBayesTools.Weights(weights, x.Length);
            var edges = DiscreteBayesTools.LearnBinEdges(x, inputs, Bins, options);
            var states = DiscreteBayesTools.NumberOfStates(edges);
            var columns = DiscreteBayesTools.DiscretizeColumns(x, edges);
            token.ThrowIfCancellationRequested();

            var priors = DiscreteBayesTools.SmoothedPriors(DiscreteBayesTools.ClassCounts(y, w, classes), alpha);

            var conditionals = new double[inputs][];
            DiscreteBayesTools.For(inputs, options, i =>
                conditionals[i] = DiscreteBayesTools.ClassConditional(columns[i], states[i], y, w, classes, alpha));

            var information = new double[inputs][];
            for (var i = 0; i < inputs; i++)
            {
                information[i] = new double[inputs];
            }

            DiscreteBayesTools.For(inputs, options, i =>
            {
                for (var j = i + 1; j < inputs; j++)
                {
                    token.ThrowIfCancellationRequested();
                    var cmi = DiscreteBayesTools.ConditionalMutualInformation(
                        columns[i], states[i], columns[j], states[j], y, classes, w);
                    information[i][j] = cmi;
                    information[j][i] = cmi;
                }
            });

            var parentWeights = new double[inputs][];
            for (var i = 0; i < inputs; i++)
            {
                var row = new double[inputs];
                double sum = 0;
                for (var j = 0; j < inputs; j++)
                {
                    if (j != i)
                    {
                        sum += information[i][j];
                    }
                }

                for (var j = 0; j < inputs; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    row[j] = sum > 0 ? information[i][j] / sum : 1.0 / (inputs - 1);
                }

                parentWeights[i] = row;
            }

            var pairs = new double[inputs][][];
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
                        "feature " + i + " and hidden parent " + j);
                    var table = new double[size];
                    var ci = columns[i];
                    var cj = columns[j];
                    for (var r = 0; r < x.Length; r++)
                    {
                        table[((long)y[r] * states[j] + cj[r]) * states[i] + ci[r]] += w[r];
                    }

                    DiscreteBayesTools.Normalize(table, states[i], alpha);
                    row[j] = table;
                }

                pairs[i] = row;
            });

            token.ThrowIfCancellationRequested();
            Model = new HiddenNaiveBayes(classes, edges, states, priors, conditionals, pairs, parentWeights);
            return Model;
        }
    }
}