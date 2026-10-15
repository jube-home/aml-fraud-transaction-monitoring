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
using Accord.Diagnostics;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Fields.Features;
using Accord.Statistics.Models.Fields.Functions.Specialized;
using Accord.Statistics.Models.Markov;

namespace Accord.Statistics.Models.Fields.Functions
{
#pragma warning disable 612, 618
    /// <summary>
    ///     Potential function modeling <see cref="HiddenMarkovClassifier">Hidden Markov Classifiers</see>.
    /// </summary>
    [Serializable]
    public sealed class MarkovContinuousFunction : PotentialFunctionBase<double>, IPotentialFunction<double>
    {
        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        [Obsolete(
            "Obsolete due the use of HiddenMarkovClassifier<NormalDistribution>. Please use HiddenMarkovClassifier<NormalDistribution, double> instead.")]
        public MarkovContinuousFunction(HiddenMarkovClassifier<NormalDistribution> classifier)
        {
            Outputs = classifier.Classes;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double>>();

            Factors = new FactorPotential<double>[Outputs];

            var classOffset = new int[classifier.Classes];
            var edgeOffset = new int[classifier.Classes];
            var stateOffset = new int[classifier.Classes];
            var classCount = new int[classifier.Classes];
            var edgeCount = new int[classifier.Classes];
            var stateCount = new int[classifier.Classes];


            // Create features for initial class probabilities
            for (var c = 0; c < classifier.Classes; c++)
            {
                var stateParams = new List<double>();
                var stateFeatures = new List<IFeature<double>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<double>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<double>>();

                var model = classifier[c];

                // Create features for class labels
                classParams.Add(System.Math.Log(classifier.Priors[c]));
                classFeatures.Add(new OutputFeature<double>(this, c, c));

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.Probabilities[i]);
                    edgeFeatures.Add(new InitialFeature<double>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.Transitions[i, j]);
                        edgeFeatures.Add(new TransitionFeature<double>(this, c, i, j));
                    }
                }

                // Create features emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    var mean = model.Emissions[i].Mean;
                    var var = model.Emissions[i].Variance;

                    // Occupancy
                    stateParams.Add(-0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var));
                    stateFeatures.Add(new OccupancyFeature<double>(this, c, i));

                    // 1st Moment (x)
                    stateParams.Add(mean / var);
                    stateFeatures.Add(new FirstMomentFeature(this, c, i));

                    // 2nd Moment (x²)
                    stateParams.Add(-1.0 / (2.0 * var));
                    stateFeatures.Add(new SecondMomentFeature(this, c, i));
                }


                classOffset[c] = factorIndex;
                edgeOffset[c] = factorIndex + classParams.Count;
                stateOffset[c] = factorIndex + classParams.Count + edgeParams.Count;

                classCount[c] = classParams.Count;
                edgeCount[c] = edgeParams.Count;
                stateCount[c] = stateParams.Count;


                // 1. classes
                factorFeatures.AddRange(classFeatures);
                factorParams.AddRange(classParams);

                // 2. edges
                factorFeatures.AddRange(edgeFeatures);
                factorParams.AddRange(edgeParams);

                // 3. states
                factorFeatures.AddRange(stateFeatures);
                factorParams.AddRange(stateParams);

                factorIndex += classParams.Count + stateParams.Count + edgeParams.Count;
            }

            Debug.Assert(factorIndex == factorParams.Count);
            Debug.Assert(factorIndex == factorFeatures.Count);

            Weights = factorParams.ToArray();
            Features = factorFeatures.ToArray();


            for (var c = 0; c < classifier.Classes; c++)
            {
                Factors[c] = new MarkovNormalFactor(this, classifier.Models[c].States, c,
                    classIndex: classOffset[c], classCount: classCount[c], // 1. classes
                    edgeIndex: edgeOffset[c], edgeCount: edgeCount[c], // 2. edges
                    stateIndex: stateOffset[c], stateCount: stateCount[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        public MarkovContinuousFunction(HiddenMarkovClassifier<NormalDistribution, double> classifier)
        {
            Outputs = classifier.Classes;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double>>();

            Factors = new FactorPotential<double>[Outputs];

            var classOffset = new int[classifier.Classes];
            var edgeOffset = new int[classifier.Classes];
            var stateOffset = new int[classifier.Classes];
            var classCount = new int[classifier.Classes];
            var edgeCount = new int[classifier.Classes];
            var stateCount = new int[classifier.Classes];


            // Create features for initial class probabilities
            for (var c = 0; c < classifier.Classes; c++)
            {
                var stateParams = new List<double>();
                var stateFeatures = new List<IFeature<double>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<double>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<double>>();

                var model = classifier[c];

                // Create features for class labels
                classParams.Add(System.Math.Log(classifier.Priors[c]));
                classFeatures.Add(new OutputFeature<double>(this, c, c));

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<double>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.LogTransitions[i][j]);
                        edgeFeatures.Add(new TransitionFeature<double>(this, c, i, j));
                    }
                }

                // Create features emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    var mean = model.Emissions[i].Mean;
                    var var = model.Emissions[i].Variance;

                    // Occupancy
                    stateParams.Add(-0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var));
                    stateFeatures.Add(new OccupancyFeature<double>(this, c, i));

                    // 1st Moment (x)
                    stateParams.Add(mean / var);
                    stateFeatures.Add(new FirstMomentFeature(this, c, i));

                    // 2nd Moment (x²)
                    stateParams.Add(-1.0 / (2.0 * var));
                    stateFeatures.Add(new SecondMomentFeature(this, c, i));
                }


                classOffset[c] = factorIndex;
                edgeOffset[c] = factorIndex + classParams.Count;
                stateOffset[c] = factorIndex + classParams.Count + edgeParams.Count;

                classCount[c] = classParams.Count;
                edgeCount[c] = edgeParams.Count;
                stateCount[c] = stateParams.Count;


                // 1. classes
                factorFeatures.AddRange(classFeatures);
                factorParams.AddRange(classParams);

                // 2. edges
                factorFeatures.AddRange(edgeFeatures);
                factorParams.AddRange(edgeParams);

                // 3. states
                factorFeatures.AddRange(stateFeatures);
                factorParams.AddRange(stateParams);

                factorIndex += classParams.Count + stateParams.Count + edgeParams.Count;
            }

            Debug.Assert(factorIndex == factorParams.Count);
            Debug.Assert(factorIndex == factorFeatures.Count);

            Weights = factorParams.ToArray();
            Features = factorFeatures.ToArray();


            for (var c = 0; c < classifier.Classes; c++)
            {
                Factors[c] = new MarkovNormalFactor(this, classifier.Models[c].States, c,
                    classIndex: classOffset[c], classCount: classCount[c], // 1. classes
                    edgeIndex: edgeOffset[c], edgeCount: edgeCount[c], // 2. edges
                    stateIndex: stateOffset[c], stateCount: stateCount[c]); // 3. states
            }
        }

        #region ICloneable Members

        private MarkovContinuousFunction()
        {
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new MarkovContinuousFunction();

            clone.Factors = new FactorPotential<double>[Factors.Length];
            for (var i = 0; i < Factors.Length; i++)
            {
                clone.Factors[i] = Factors[i].Clone(clone);
            }

            clone.Features = new IFeature<double>[Features.Length];
            for (var i = 0; i < Features.Length; i++)
            {
                clone.Features[i] = Features[i].Clone(clone);
            }

            clone.Outputs = Outputs;
            clone.Weights = (double[])Weights.Clone();

            return clone;
        }

        #endregion
    }
}