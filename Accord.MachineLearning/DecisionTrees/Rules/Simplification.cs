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

using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Accord.Statistics.Analysis;
using Accord.Statistics.Testing;

namespace Accord.MachineLearning.DecisionTrees.Rules
{
    /// <summary>
    ///     Decision rule simplification algorithm.
    /// </summary>
    public class Simplification
    {
        private readonly DecisionSet decisionList;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Simplification" /> class.
        /// </summary>
        /// <param name="list">The decision set to be simplified.</param>
        public Simplification(DecisionSet list)
        {
            decisionList = list;
        }

        /// <summary>
        ///     Gets or sets the underlying hypothesis test
        ///     size parameter used to reject hypothesis.
        /// </summary>
        public double Alpha { get; set; } = 0.05;

        /// <summary>
        ///     Computes the reduction algorithm.
        /// </summary>
        /// <param name="inputs">A set of training inputs.</param>
        /// <param name="outputs">The outputs corresponding to each of the inputs.</param>
        /// <returns>The average error after the reduction.</returns>
        public double Compute(double[][] inputs, int[] outputs)
        {
            var samples = outputs.Length;

            var actual = new bool[samples];
            var expected = new bool[samples];

            var list = decisionList.ToArray();

            var antecedents = new HashSet<Antecedent>();


            foreach (var rule in list)
            {
                foreach (var antecedent in rule)
                {
                    antecedents.Add(antecedent);
                }
            }


            // 1. Eliminate unnecessary antecedents
            for (var y = 0; y < decisionList.NumberOfClasses; y++)
            {
                for (var i = 0; i < outputs.Length; i++)
                {
                    expected[i] = outputs[i] == y;
                }

                var unnecessary = new HashSet<Antecedent>();

                /*foreach (var rule in list)
                {
                    if (rule.Output != y)
                        continue;
                */
                foreach (var antecedent in antecedents)
                {
                    for (var i = 0; i < inputs.Length; i++)
                    {
                        actual[i] = antecedent.Match(inputs[i]);
                    }

                    if (CanEliminate(actual, expected))
                    {
                        unnecessary.Add(antecedent);
                    }
                }
                //}

                foreach (var antecedent in unnecessary)
                {
                    foreach (var rule in list)
                    {
                        if (rule.Output == y)
                        {
                            rule.Antecedents.Remove(antecedent);
                        }
                    }
                }
            }

            var matches = new bool[list.Length][];
            var counts = new int[list.Length];
            for (var i = 0; i < matches.Length; i++)
            {
                var rule = list[i];

                matches[i] = new bool[outputs.Length];
                for (var j = 0; j < inputs.Length; j++)
                {
                    matches[i][j] = rule.Match(inputs[j]);

                    if (matches[i][j])
                    {
                        counts[i]++;
                    }
                }
            }

            // double start = computeError(inputs, outputs, list);

            for (var i = 0; i < list.Length; i++)
            {
                if (list[i] == null)
                {
                    continue;
                }

                for (var j = 0; j < list.Length; j++)
                {
                    if (list[i] == null)
                    {
                        break;
                    }

                    if (list[j] == null)
                    {
                        continue;
                    }

                    if (list[i].IsInconsistentWith(list[j]))
                    {
                        if (counts[i] > counts[j])
                        {
                            list[j] = null;
                            counts[j] = 0;
                        }
                        else
                        {
                            list[i] = null;
                            counts[i] = 0;
                        }
                    }
                }
            }

            list = list.Distinct(false);

            var newList = new List<DecisionRule>(list);

            // double middle = computeError(inputs, outputs, list);

            // 2. Eliminate redundant rules from the set

            for (var y = 0; y < decisionList.NumberOfClasses; y++)
            {
                for (var i = 0; i < outputs.Length; i++)
                {
                    expected[i] = outputs[i] == y;
                }

                var unnecessary = new HashSet<DecisionRule>();

                foreach (var rule in newList)
                {
                    if (rule.Output != y)
                    {
                        continue;
                    }

                    for (var i = 0; i < inputs.Length; i++)
                    {
                        actual[i] = rule.Match(inputs[i]);
                    }

                    if (CanEliminate(actual, expected))
                    {
                        unnecessary.Add(rule);
                    }
                }

                foreach (var rule in unnecessary)
                {
                    newList.Remove(rule);
                }
            }


            // double final = computeError(inputs, outputs, newList);

            decisionList.Clear();
            decisionList.AddRange(newList);

            // Compute new decision error
            var newError = ComputeError(inputs, outputs);

            return newError;
        }

        /// <summary>
        ///     Computes the average decision error.
        /// </summary>
        /// <param name="inputs">A set of input vectors.</param>
        /// <param name="outputs">A set of corresponding output vectors.</param>
        /// <returns>The average misclassification rate.</returns>
        public double ComputeError(double[][] inputs, int[] outputs)
        {
            return computeError(inputs, outputs, decisionList);
        }

        private static double computeError(double[][] inputs, int[] outputs, IEnumerable<DecisionRule> rules)
        {
            var ruleList = rules as ICollection<DecisionRule> ?? rules.ToList();

            var errors = 0;
            for (var i = 0; i < outputs.Length; i++)
            {
                double expected = outputs[i];
                var actual = match(ruleList, inputs[i]);

                if (expected != actual)
                {
                    errors++;
                }
            }

            return errors / (double)inputs.Length;
        }

        /// <summary>
        ///     Checks if two variables can be eliminated.
        /// </summary>
        public bool CanEliminate(bool[] actual, bool[] expected)
        {
            return CanEliminate(actual, expected, Alpha);
        }

        /// <summary>
        ///     Checks if two variables can be eliminated.
        /// </summary>
        public static bool CanEliminate(bool[] actual, bool[] expected, double alpha)
        {
            var matrix = new ConfusionMatrix(actual, expected);

            var maxExpectedFrequency = matrix.ExpectedValues.Max();

            IHypothesisTest test;

            if (maxExpectedFrequency > 10)
            {
                test = new ChiSquareTest(matrix) { Size = alpha };
            }

            else if (maxExpectedFrequency >= 5)
            {
                test = new ChiSquareTest(matrix, true) { Size = alpha };
            }

            else
            {
                test = new FisherExactTest(matrix) { Size = alpha };
            }

            return !test.Significant;
        }

        private static double? match(IEnumerable<DecisionRule> rules, double[] input)
        {
            foreach (var rule in rules)
            {
                if (rule.Match(input))
                {
                    return rule.Output;
                }
            }

            return null;
        }
    }
}