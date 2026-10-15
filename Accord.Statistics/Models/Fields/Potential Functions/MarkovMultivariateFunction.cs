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
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Fields.Features;
using Accord.Statistics.Models.Fields.Functions.Specialized;
using Accord.Statistics.Models.Markov;

namespace Accord.Statistics.Models.Fields.Functions
{
#pragma warning disable 612, 618
    /// <summary>
    ///     Potential function modeling Hidden Markov Models.
    /// </summary>
    [Serializable]
    public sealed partial class MarkovMultivariateFunction
        : PotentialFunctionBase<double[]>, IPotentialFunction<double[]>
    {
        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        [Obsolete(
            "Obsolete due the use of HiddenMarkovClassifier<MultivariateNormalDistribution>. Please use HiddenMarkovClassifier<MultivariateNormalDistribution, double[]> instead.")]
        public MarkovMultivariateFunction(HiddenMarkovClassifier<MultivariateNormalDistribution> classifier,
            bool includePriors = true)
        {
            // TODO: Mark this function as obsolete

            Outputs = classifier.Classes;
            Dimensions = classifier.Models[0].Dimension;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double[]>>();

            Factors = new FactorPotential<double[]>[Outputs];

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
                var stateFeatures = new List<IFeature<double[]>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<double[]>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<double[]>>();

                var model = classifier[c];

                if (includePriors)
                {
                    // Create features for class labels
                    classParams.Add(System.Math.Log(classifier.Priors[c]));
                    classFeatures.Add(new OutputFeature<double[]>(this, c, c));
                }

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<double[]>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.Transitions[i, j]);
                        edgeFeatures.Add(new TransitionFeature<double[]>(this, c, i, j));
                    }
                }

                // Create features emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var d = 0; d < model.Dimension; d++)
                    {
                        var mean = model.Emissions[i].Mean[d];
                        var var = model.Emissions[i].Variance[d];

                        var u = -0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var);
                        var m1 = mean / var;
                        var m2 = -1.0 / (2.0 * var);

                        // Occupancy
                        stateParams.Add(u);
                        stateFeatures.Add(new OccupancyFeature<double[]>(this, c, i));

                        // 1st Moment (x)
                        stateParams.Add(m1);
                        stateFeatures.Add(new MultivariateFirstMomentFeature(this, c, i, d));

                        // 2nd Moment (x²)
                        stateParams.Add(m2);
                        stateFeatures.Add(new MultivariateSecondMomentFeature(this, c, i, d));
                    }
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

            for (var c = 0; c < classifier.Models.Length; c++)
            {
                Factors[c] = new MarkovMultivariateNormalFactor(this, classifier.Models[c].States, c, Dimensions,
                    classIndex: classOffset[c], classCount: classCount[c], // 1. classes
                    edgeIndex: edgeOffset[c], edgeCount: edgeCount[c], // 2. edges
                    stateIndex: stateOffset[c], stateCount: stateCount[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        public MarkovMultivariateFunction(HiddenMarkovClassifier<MultivariateNormalDistribution, double[]> classifier,
            bool includePriors = true)
        {
            Outputs = classifier.Classes;
            Dimensions = classifier.Models[0].NumberOfInputs;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double[]>>();

            Factors = new FactorPotential<double[]>[Outputs];

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
                var stateFeatures = new List<IFeature<double[]>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<double[]>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<double[]>>();

                var model = classifier[c];

                if (includePriors)
                {
                    // Create features for class labels
                    classParams.Add(System.Math.Log(classifier.Priors[c]));
                    classFeatures.Add(new OutputFeature<double[]>(this, c, c));
                }

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<double[]>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.LogTransitions[i][j]);
                        edgeFeatures.Add(new TransitionFeature<double[]>(this, c, i, j));
                    }
                }

                // Create features emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var d = 0; d < model.NumberOfInputs; d++)
                    {
                        var mean = model.Emissions[i].Mean[d];
                        var var = model.Emissions[i].Variance[d];

                        var u = -0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var);
                        var m1 = mean / var;
                        var m2 = -1.0 / (2.0 * var);

                        // Occupancy
                        stateParams.Add(u);
                        stateFeatures.Add(new OccupancyFeature<double[]>(this, c, i));

                        // 1st Moment (x)
                        stateParams.Add(m1);
                        stateFeatures.Add(new MultivariateFirstMomentFeature(this, c, i, d));

                        // 2nd Moment (x²)
                        stateParams.Add(m2);
                        stateFeatures.Add(new MultivariateSecondMomentFeature(this, c, i, d));
                    }
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

            for (var c = 0; c < classifier.Models.Length; c++)
            {
                Factors[c] = new MarkovMultivariateNormalFactor(this, classifier.Models[c].States, c, Dimensions,
                    classIndex: classOffset[c], classCount: classCount[c], // 1. classes
                    edgeIndex: edgeOffset[c], edgeCount: edgeCount[c], // 2. edges
                    stateIndex: stateOffset[c], stateCount: stateCount[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        [Obsolete(
            "Obsolete due the use of HiddenMarkovClassifier<Independent<NormalDistribution>>. Please use HiddenMarkovClassifier<Independent<NormalDistribution, double>, double[]> instead.")]
        public MarkovMultivariateFunction(HiddenMarkovClassifier<Independent<NormalDistribution>> classifier,
            bool includePriors = true)
        {
            Outputs = classifier.Classes;
            Dimensions = classifier.Models[0].Dimension;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double[]>>();

            Factors = new FactorPotential<double[]>[Outputs];

            var startClassIndex = new int[classifier.Classes];
            var startEdgeIndex = new int[classifier.Classes];
            var startStateIndex = new int[classifier.Classes];
            var endClassIndex = new int[classifier.Classes];
            var endEdgeIndex = new int[classifier.Classes];
            var endStateIndex = new int[classifier.Classes];


            // Create features for initial class probabilities
            for (var c = 0; c < classifier.Classes; c++)
            {
                var stateParams = new List<double>();
                var stateFeatures = new List<IFeature<double[]>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<double[]>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<double[]>>();

                var model = classifier[c];

                if (includePriors)
                {
                    // Create features for class labels
                    classParams.Add(System.Math.Log(classifier.Priors[c]));
                    classFeatures.Add(new OutputFeature<double[]>(this, c, c));
                }

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<double[]>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.Transitions[i, j]);
                        edgeFeatures.Add(new TransitionFeature<double[]>(this, c, i, j));
                    }
                }

                // Create features emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var d = 0; d < model.Emissions[i].Mean.Length; d++)
                    {
                        var mean = model.Emissions[i].Mean[d];
                        var var = model.Emissions[i].Variance[d];

                        var u = -0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var);
                        var m1 = mean / var;
                        var m2 = -1.0 / (2.0 * var);

                        // Occupancy
                        stateParams.Add(u);
                        stateFeatures.Add(new OccupancyFeature<double[]>(this, c, i));

                        // 1st Moment (x)
                        stateParams.Add(m1);
                        stateFeatures.Add(new MultivariateFirstMomentFeature(this, c, i, d));

                        // 2nd Moment (x²)
                        stateParams.Add(m2);
                        stateFeatures.Add(new MultivariateSecondMomentFeature(this, c, i, d));
                    }
                }

                startClassIndex[c] = factorIndex;
                startEdgeIndex[c] = factorIndex + classParams.Count;
                startStateIndex[c] = factorIndex + classParams.Count + edgeParams.Count;

                endClassIndex[c] = classParams.Count;
                endEdgeIndex[c] = edgeParams.Count;
                endStateIndex[c] = stateParams.Count;


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
                Factors[c] = new MarkovMultivariateNormalFactor(this, classifier.Models[c].States, c, Dimensions,
                    classIndex: startClassIndex[c], classCount: endClassIndex[c], // 1. classes
                    edgeIndex: startEdgeIndex[c], edgeCount: endEdgeIndex[c], // 2. edges
                    stateIndex: startStateIndex[c], stateCount: endStateIndex[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        public MarkovMultivariateFunction(
            HiddenMarkovClassifier<Independent<NormalDistribution, double>, double[]> classifier,
            bool includePriors = true)
        {
            Outputs = classifier.Classes;
            Dimensions = classifier.Models[0].NumberOfInputs;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double[]>>();

            Factors = new FactorPotential<double[]>[Outputs];

            var startClassIndex = new int[classifier.Classes];
            var startEdgeIndex = new int[classifier.Classes];
            var startStateIndex = new int[classifier.Classes];
            var endClassIndex = new int[classifier.Classes];
            var endEdgeIndex = new int[classifier.Classes];
            var endStateIndex = new int[classifier.Classes];


            // Create features for initial class probabilities
            for (var c = 0; c < classifier.Classes; c++)
            {
                var stateParams = new List<double>();
                var stateFeatures = new List<IFeature<double[]>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<double[]>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<double[]>>();

                var model = classifier[c];

                if (includePriors)
                {
                    // Create features for class labels
                    classParams.Add(System.Math.Log(classifier.Priors[c]));
                    classFeatures.Add(new OutputFeature<double[]>(this, c, c));
                }

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<double[]>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.LogTransitions[i][j]);
                        edgeFeatures.Add(new TransitionFeature<double[]>(this, c, i, j));
                    }
                }

                // Create features emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var d = 0; d < model.Emissions[i].Mean.Length; d++)
                    {
                        var mean = model.Emissions[i].Mean[d];
                        var var = model.Emissions[i].Variance[d];

                        var u = -0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var);
                        var m1 = mean / var;
                        var m2 = -1.0 / (2.0 * var);

                        // Occupancy
                        stateParams.Add(u);
                        stateFeatures.Add(new OccupancyFeature<double[]>(this, c, i));

                        // 1st Moment (x)
                        stateParams.Add(m1);
                        stateFeatures.Add(new MultivariateFirstMomentFeature(this, c, i, d));

                        // 2nd Moment (x²)
                        stateParams.Add(m2);
                        stateFeatures.Add(new MultivariateSecondMomentFeature(this, c, i, d));
                    }
                }

                startClassIndex[c] = factorIndex;
                startEdgeIndex[c] = factorIndex + classParams.Count;
                startStateIndex[c] = factorIndex + classParams.Count + edgeParams.Count;

                endClassIndex[c] = classParams.Count;
                endEdgeIndex[c] = edgeParams.Count;
                endStateIndex[c] = stateParams.Count;


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
                Factors[c] = new MarkovMultivariateNormalFactor(this, classifier.Models[c].States, c, Dimensions,
                    classIndex: startClassIndex[c], classCount: endClassIndex[c], // 1. classes
                    edgeIndex: startEdgeIndex[c], edgeCount: endEdgeIndex[c], // 2. edges
                    stateIndex: startStateIndex[c], stateCount: endStateIndex[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        public MarkovMultivariateFunction(HiddenMarkovClassifier<Independent<NormalDistribution>, double[]> classifier,
            bool includePriors = true)
        {
            // TODO: Unify the contents of this method with the method above

            Outputs = classifier.Classes;
            Dimensions = classifier.Models[0].NumberOfInputs;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double[]>>();

            Factors = new FactorPotential<double[]>[Outputs];

            var startClassIndex = new int[classifier.Classes];
            var startEdgeIndex = new int[classifier.Classes];
            var startStateIndex = new int[classifier.Classes];
            var endClassIndex = new int[classifier.Classes];
            var endEdgeIndex = new int[classifier.Classes];
            var endStateIndex = new int[classifier.Classes];


            // Create features for initial class probabilities
            for (var c = 0; c < classifier.Classes; c++)
            {
                var stateParams = new List<double>();
                var stateFeatures = new List<IFeature<double[]>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<double[]>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<double[]>>();

                var model = classifier[c];

                if (includePriors)
                {
                    // Create features for class labels
                    classParams.Add(System.Math.Log(classifier.Priors[c]));
                    classFeatures.Add(new OutputFeature<double[]>(this, c, c));
                }

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<double[]>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.LogTransitions[i][j]);
                        edgeFeatures.Add(new TransitionFeature<double[]>(this, c, i, j));
                    }
                }

                // Create features emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var d = 0; d < model.Emissions[i].Mean.Length; d++)
                    {
                        var mean = model.Emissions[i].Mean[d];
                        var var = model.Emissions[i].Variance[d];

                        var u = -0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var);
                        var m1 = mean / var;
                        var m2 = -1.0 / (2.0 * var);

                        // Occupancy
                        stateParams.Add(u);
                        stateFeatures.Add(new OccupancyFeature<double[]>(this, c, i));

                        // 1st Moment (x)
                        stateParams.Add(m1);
                        stateFeatures.Add(new MultivariateFirstMomentFeature(this, c, i, d));

                        // 2nd Moment (x²)
                        stateParams.Add(m2);
                        stateFeatures.Add(new MultivariateSecondMomentFeature(this, c, i, d));
                    }
                }

                startClassIndex[c] = factorIndex;
                startEdgeIndex[c] = factorIndex + classParams.Count;
                startStateIndex[c] = factorIndex + classParams.Count + edgeParams.Count;

                endClassIndex[c] = classParams.Count;
                endEdgeIndex[c] = edgeParams.Count;
                endStateIndex[c] = stateParams.Count;


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
                Factors[c] = new MarkovMultivariateNormalFactor(this, classifier.Models[c].States, c, Dimensions,
                    classIndex: startClassIndex[c], classCount: endClassIndex[c], // 1. classes
                    edgeIndex: startEdgeIndex[c], edgeCount: endEdgeIndex[c], // 2. edges
                    stateIndex: startStateIndex[c], stateCount: endStateIndex[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="model">A normal density hidden Markov.</param>
        [Obsolete(
            "Obsolete due the use of HiddenMarkovModel<MultivariateNormalDistribution>. Please use HiddenMarkovModel<MultivariateNormalDistribution, double[]> instead.")]
        public MarkovMultivariateFunction(HiddenMarkovModel<MultivariateNormalDistribution> model)
        {
            Dimensions = model.Dimension;

            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double[]>>();


            var stateParams = new List<double>();
            var stateFeatures = new List<IFeature<double[]>>();

            var edgeParams = new List<double>();
            var edgeFeatures = new List<IFeature<double[]>>();


            // Create features for initial state probabilities
            for (var i = 0; i < model.States; i++)
            {
                edgeParams.Add(model.LogInitial[i]);
                edgeFeatures.Add(new InitialFeature<double[]>(this, 0, i));
            }

            // Create features for state transition probabilities
            for (var i = 0; i < model.States; i++)
            {
                for (var j = 0; j < model.States; j++)
                {
                    edgeParams.Add(model.Transitions[i, j]);
                    edgeFeatures.Add(new TransitionFeature<double[]>(this, 0, i, j));
                }
            }

            // Create features emission probabilities
            for (var i = 0; i < model.States; i++)
            {
                for (var d = 0; d < model.Dimension; d++)
                {
                    var mean = model.Emissions[i].Mean[d];
                    var var = model.Emissions[i].Variance[d];

                    // Occupancy
                    stateParams.Add(-0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var));
                    stateFeatures.Add(new OccupancyFeature<double[]>(this, 0, i));

                    // 1st Moment (x)
                    stateParams.Add(mean / var);
                    stateFeatures.Add(new MultivariateFirstMomentFeature(this, 0, i, d));

                    // 2nd Moment (x²)
                    stateParams.Add(-1.0 / (2.0 * var));
                    stateFeatures.Add(new MultivariateSecondMomentFeature(this, 0, i, d));
                }
            }


            // 1. edges
            factorFeatures.AddRange(edgeFeatures);
            factorParams.AddRange(edgeParams);

            // 2. states
            factorFeatures.AddRange(stateFeatures);
            factorParams.AddRange(stateParams);

            Weights = factorParams.ToArray();
            Features = factorFeatures.ToArray();


            Factors = new[] // First features and parameters are always belonging to edges
            {
                new MarkovMultivariateNormalFactor(this, model.States, 0, Dimensions,
                    0, edgeParams.Count, // 1. edges
                    edgeParams.Count, stateParams.Count) // 2. states
            };
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="model">A normal density hidden Markov.</param>
        public MarkovMultivariateFunction(HiddenMarkovModel<MultivariateNormalDistribution, double[]> model)
        {
            Dimensions = model.NumberOfInputs;

            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<double[]>>();


            var stateParams = new List<double>();
            var stateFeatures = new List<IFeature<double[]>>();

            var edgeParams = new List<double>();
            var edgeFeatures = new List<IFeature<double[]>>();


            // Create features for initial state probabilities
            for (var i = 0; i < model.States; i++)
            {
                edgeParams.Add(model.LogInitial[i]);
                edgeFeatures.Add(new InitialFeature<double[]>(this, 0, i));
            }

            // Create features for state transition probabilities
            for (var i = 0; i < model.States; i++)
            {
                for (var j = 0; j < model.States; j++)
                {
                    edgeParams.Add(model.LogTransitions[i][j]);
                    edgeFeatures.Add(new TransitionFeature<double[]>(this, 0, i, j));
                }
            }

            // Create features emission probabilities
            for (var i = 0; i < model.States; i++)
            {
                for (var d = 0; d < model.Emissions[i].Mean.Length; d++)
                {
                    var mean = model.Emissions[i].Mean[d];
                    var var = model.Emissions[i].Variance[d];

                    // Occupancy
                    stateParams.Add(-0.5 * (System.Math.Log(2.0 * System.Math.PI * var) + mean * mean / var));
                    stateFeatures.Add(new OccupancyFeature<double[]>(this, 0, i));

                    // 1st Moment (x)
                    stateParams.Add(mean / var);
                    stateFeatures.Add(new MultivariateFirstMomentFeature(this, 0, i, d));

                    // 2nd Moment (x²)
                    stateParams.Add(-1.0 / (2.0 * var));
                    stateFeatures.Add(new MultivariateSecondMomentFeature(this, 0, i, d));
                }
            }


            // 1. edges
            factorFeatures.AddRange(edgeFeatures);
            factorParams.AddRange(edgeParams);

            // 2. states
            factorFeatures.AddRange(stateFeatures);
            factorParams.AddRange(stateParams);

            Weights = factorParams.ToArray();
            Features = factorFeatures.ToArray();


            Factors = new[] // First features and parameters are always belonging to edges
            {
                new MarkovMultivariateNormalFactor(this, model.States, 0, Dimensions,
                    0, edgeParams.Count, // 1. edges
                    edgeParams.Count, stateParams.Count) // 2. states
            };
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        [Obsolete(
            "Obsolete due the use of HiddenMarkovClassifier<Independent>. Please use HiddenMarkovClassifier<Independent, double[]> instead.")]
        public MarkovMultivariateFunction(HiddenMarkovClassifier<Independent> classifier, bool includePriors = true)
        {
            // TODO: Mark this function as obsolete

            Outputs = classifier.Classes;

            var layout = new IndependentLayout(classifier.Classes);

            Factors = new FactorPotential<double[]>[Outputs];


            // Create features for initial class probabilities
            for (var c = 0; c < classifier.Classes; c++)
            {
                var features = new ClassFeatureSet();

                var model = classifier[c];

                var lookupTable = createLookupTable(model.States, model.Dimension);

                if (includePriors)
                {
                    addClassPrior(features, c, classifier.Priors[c]);
                }

                addInitialStateFeatures(features, c, model.States, model.LogInitial);

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        features.EdgeParams.Add(model.Transitions[i, j]);
                        features.EdgeFeatures.Add(new TransitionFeature<double[]>(this, c, i, j));
                    }
                }

                addIndependentEmissionFeatures(features, c, model.States, model.Emissions, lookupTable);

                layout.AddClass(c, features, lookupTable);
            }

            assignWeightsAndFeatures(layout);


            for (var c = 0; c < classifier.Models.Length; c++)
            {
                createIndependentFactor(layout, c, classifier.Models[c].States);
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">A hidden Markov sequence classifier.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        public MarkovMultivariateFunction(HiddenMarkovClassifier<Independent, double[]> classifier,
            bool includePriors = true)
        {
            Outputs = classifier.Classes;

            var layout = new IndependentLayout(classifier.Classes);

            Factors = new FactorPotential<double[]>[Outputs];


            // Create features for initial class probabilities
            for (var c = 0; c < classifier.Classes; c++)
            {
                var features = new ClassFeatureSet();

                var model = classifier[c];

                var lookupTable = createLookupTable(model.States, model.NumberOfInputs);

                if (includePriors)
                {
                    addClassPrior(features, c, classifier.Priors[c]);
                }

                addInitialStateFeatures(features, c, model.States, model.LogInitial);

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        features.EdgeParams.Add(model.LogTransitions[i][j]);
                        features.EdgeFeatures.Add(new TransitionFeature<double[]>(this, c, i, j));
                    }
                }

                addIndependentEmissionFeatures(features, c, model.States, model.Emissions, lookupTable);

                layout.AddClass(c, features, lookupTable);
            }

            assignWeightsAndFeatures(layout);


            for (var c = 0; c < classifier.Models.Length; c++)
            {
                createIndependentFactor(layout, c, classifier.Models[c].States);
            }
        }

        /// <summary>
        ///     Gets the total number of dimensions for
        ///     this multivariate potential function.
        /// </summary>
        public int Dimensions { get; private set; }

        #region ICloneable Members

        private MarkovMultivariateFunction()
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
            var clone = new MarkovMultivariateFunction();

            clone.Factors = new FactorPotential<double[]>[Factors.Length];
            for (var i = 0; i < Factors.Length; i++)
            {
                clone.Factors[i] = Factors[i].Clone(clone);
            }

            clone.Features = new IFeature<double[]>[Features.Length];
            for (var i = 0; i < Features.Length; i++)
            {
                clone.Features[i] = Features[i].Clone(clone);
            }

            clone.Outputs = Outputs;
            clone.Weights = (double[])Weights.Clone();

            clone.Dimensions = Dimensions;

            return clone;
        }

        #endregion
    }
}