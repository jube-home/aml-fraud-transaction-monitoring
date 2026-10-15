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
using Accord.Math.Random;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Fields.Features;
using Accord.Statistics.Models.Fields.Functions.Specialized;
using Accord.Statistics.Models.Markov;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Fields.Functions
{
    /// <summary>
    ///     Potential function modeling Hidden Markov Models.
    /// </summary>
    [Serializable]
    public sealed class MarkovDiscreteFunction : PotentialFunctionBase<int>, IPotentialFunction<int>, ICloneable
    {
        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="states">The number of states.</param>
        /// <param name="symbols">The number of symbols.</param>
        /// <param name="outputClasses">The number of output classes.</param>
        public MarkovDiscreteFunction(int states, int symbols, int outputClasses)
        {
            Outputs = outputClasses;
            Symbols = symbols;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<int>>();

            Factors = new FactorPotential<int>[Outputs];

            var classOffset = new int[outputClasses];
            var edgeOffset = new int[outputClasses];
            var stateOffset = new int[outputClasses];
            var classCount = new int[outputClasses];
            var edgeCount = new int[outputClasses];
            var stateCount = new int[outputClasses];


            // Create features for initial class probabilities
            for (var c = 0; c < outputClasses; c++)
            {
                var stateParams = new List<double>();
                var stateFeatures = new List<IFeature<int>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<int>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<int>>();


                // Create features for class labels
                classParams.Add(System.Math.Log(1.0 / outputClasses));
                classFeatures.Add(new OutputFeature<int>(this, c, c));

                // Create features for initial state probabilities
                for (var i = 0; i < states; i++)
                {
                    edgeParams.Add(i == 0 ? System.Math.Log(1.0) : System.Math.Log(0.0));
                    edgeFeatures.Add(new InitialFeature<int>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < states; i++)
                {
                    for (var j = 0; j < states; j++)
                    {
                        edgeParams.Add(System.Math.Log(1.0 / states));
                        edgeFeatures.Add(new TransitionFeature<int>(this, c, i, j));
                    }
                }

                // Create features for symbol emission probabilities
                for (var i = 0; i < states; i++)
                {
                    for (var k = 0; k < symbols; k++)
                    {
                        stateParams.Add(System.Math.Log(1.0 / symbols));
                        stateFeatures.Add(new EmissionFeature(this, c, i, k));
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

            for (var c = 0; c < outputClasses; c++)
            {
                Factors[c] = new MarkovDiscreteFactor(this, states, c, symbols,
                    classIndex: classOffset[c], classCount: classCount[c], // 1. classes
                    edgeIndex: edgeOffset[c], edgeCount: edgeCount[c], // 2. edges
                    stateIndex: stateOffset[c], stateCount: stateCount[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">The classifier model.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        public MarkovDiscreteFunction(HiddenMarkovClassifier classifier, bool includePriors = true)
        {
            Symbols = classifier.Symbols;
            Outputs = classifier.Classes;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<int>>();

            Factors = new FactorPotential<int>[Outputs];

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
                var stateFeatures = new List<IFeature<int>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<int>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<int>>();

                var model = classifier[c];

                if (includePriors)
                {
                    // Create features for class labels
                    classParams.Add(System.Math.Log(classifier.Priors[c]));
                    classFeatures.Add(new OutputFeature<int>(this, c, c));
                }

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<int>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.Transitions[i, j]);
                        edgeFeatures.Add(new TransitionFeature<int>(this, c, i, j));
                    }
                }

                // Create features for symbol emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var k = 0; k < model.Symbols; k++)
                    {
                        stateParams.Add(model.Emissions[i, k]);
                        stateFeatures.Add(new EmissionFeature(this, c, i, k));
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
                Factors[c] = new MarkovDiscreteFactor(this, classifier.Models[c].States, c, classifier.Symbols,
                    classIndex: classOffset[c], classCount: classCount[c], // 1. classes
                    edgeIndex: edgeOffset[c], edgeCount: edgeCount[c], // 2. edges
                    stateIndex: stateOffset[c], stateCount: stateCount[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="classifier">The classifier model.</param>
        /// <param name="includePriors">True to include class features (priors), false otherwise.</param>
        public MarkovDiscreteFunction(HiddenMarkovClassifier<GeneralDiscreteDistribution, int> classifier,
            bool includePriors = true)
        {
            Symbols = classifier.Models[0].Emissions[0].Length;
            Outputs = classifier.Classes;

            var factorIndex = 0;
            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<int>>();

            Factors = new FactorPotential<int>[Outputs];

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
                var stateFeatures = new List<IFeature<int>>();

                var edgeParams = new List<double>();
                var edgeFeatures = new List<IFeature<int>>();

                var classParams = new List<double>();
                var classFeatures = new List<IFeature<int>>();

                var model = classifier[c];

                if (includePriors)
                {
                    // Create features for class labels
                    classParams.Add(System.Math.Log(classifier.Priors[c]));
                    classFeatures.Add(new OutputFeature<int>(this, c, c));
                }

                // Create features for initial state probabilities
                for (var i = 0; i < model.States; i++)
                {
                    edgeParams.Add(model.LogInitial[i]);
                    edgeFeatures.Add(new InitialFeature<int>(this, c, i));
                }

                // Create features for state transition probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var j = 0; j < model.States; j++)
                    {
                        edgeParams.Add(model.LogTransitions[i][j]);
                        edgeFeatures.Add(new TransitionFeature<int>(this, c, i, j));
                    }
                }

                // Create features for symbol emission probabilities
                for (var i = 0; i < model.States; i++)
                {
                    for (var k = 0; k < model.Emissions[i].Length; k++)
                    {
                        stateParams.Add(model.Emissions[i][k]);
                        stateFeatures.Add(new EmissionFeature(this, c, i, k));
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
                Factors[c] = new MarkovDiscreteFactor(this, classifier.Models[c].States, c, Symbols,
                    classIndex: classOffset[c], classCount: classCount[c], // 1. classes
                    edgeIndex: edgeOffset[c], edgeCount: edgeCount[c], // 2. edges
                    stateIndex: stateOffset[c], stateCount: stateCount[c]); // 3. states
            }
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="states">The number of states.</param>
        /// <param name="symbols">The number of symbols.</param>
        /// <param name="initialization">The random number generator to use when initializing weights.</param>
        public MarkovDiscreteFunction(int states, int symbols, IRandomNumberGenerator<double> initialization = null)
        {
            if (initialization == null)
            {
                initialization = new ConstantGenerator(0);
            }

            Symbols = symbols;

            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<int>>();

            var stateParams = new List<double>();
            var stateFeatures = new List<IFeature<int>>();

            var edgeParams = new List<double>();
            var edgeFeatures = new List<IFeature<int>>();


            // Create features for initial state probabilities
            for (var i = 0; i < states; i++)
            {
                edgeParams.Add(initialization.Generate());
                edgeFeatures.Add(new InitialFeature<int>(this, 0, i));
            }

            // Create features for state transition probabilities
            for (var i = 0; i < states; i++)
            {
                for (var j = 0; j < states; j++)
                {
                    edgeParams.Add(initialization.Generate());
                    edgeFeatures.Add(new TransitionFeature<int>(this, 0, i, j));
                }
            }

            // Create features for symbol emission probabilities
            for (var i = 0; i < states; i++)
            {
                for (var k = 0; k < symbols; k++)
                {
                    stateParams.Add(initialization.Generate());
                    stateFeatures.Add(new EmissionFeature(this, 0, i, k));
                }
            }


            // 1. edges
            factorFeatures.AddRange(edgeFeatures);
            factorParams.AddRange(edgeParams);

            // 2. states
            factorFeatures.AddRange(stateFeatures);
            factorParams.AddRange(stateParams);

            Features = factorFeatures.ToArray();
            Weights = factorParams.ToArray();


            Factors = new[] // First features and parameters always belong to edges
            {
                new MarkovDiscreteFactor(this, states, 0, symbols,
                    0, edgeParams.Count, // 1. edges
                    edgeParams.Count, stateParams.Count) // 2. states
            };
        }

        /// <summary>
        ///     Constructs a new potential function modeling Hidden Markov Models.
        /// </summary>
        /// <param name="model">The hidden Markov model.</param>
        public MarkovDiscreteFunction(HiddenMarkovModel model)
        {
            var states = model.States;
            Symbols = model.Symbols;

            var factorParams = new List<double>();
            var factorFeatures = new List<IFeature<int>>();

            var stateParams = new List<double>();
            var stateFeatures = new List<IFeature<int>>();

            var edgeParams = new List<double>();
            var edgeFeatures = new List<IFeature<int>>();


            // Create features for initial state probabilities
            for (var i = 0; i < states; i++)
            {
                edgeParams.Add(model.LogInitial[i]);
                edgeFeatures.Add(new InitialFeature<int>(this, 0, i));
            }

            // Create features for state transition probabilities
            for (var i = 0; i < states; i++)
            {
                for (var j = 0; j < states; j++)
                {
                    edgeParams.Add(model.Transitions[i, j]);
                    edgeFeatures.Add(new TransitionFeature<int>(this, 0, i, j));
                }
            }

            // Create features for symbol emission probabilities
            for (var i = 0; i < states; i++)
            {
                for (var k = 0; k < Symbols; k++)
                {
                    stateParams.Add(model.Emissions[i, k]);
                    stateFeatures.Add(new EmissionFeature(this, 0, i, k));
                }
            }


            // 1. edges
            factorFeatures.AddRange(edgeFeatures);
            factorParams.AddRange(edgeParams);

            // 2. states
            factorFeatures.AddRange(stateFeatures);
            factorParams.AddRange(stateParams);

            Features = factorFeatures.ToArray();
            Weights = factorParams.ToArray();


            Factors = new[]
            {
                new MarkovDiscreteFactor(this, states, 0, Symbols,
                    0, edgeParams.Count, // 1. edges
                    edgeParams.Count, stateParams.Count) // 2. states
            };
        }

        /// <summary>
        ///     Gets the number of symbols assumed by this function.
        /// </summary>
        public int Symbols { get; private set; }

        #region ICloneable Members

        private MarkovDiscreteFunction()
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
            var clone = new MarkovDiscreteFunction();

            clone.Factors = new FactorPotential<int>[Factors.Length];
            for (var i = 0; i < Factors.Length; i++)
            {
                clone.Factors[i] = Factors[i].Clone(clone);
            }

            clone.Features = new IFeature<int>[Features.Length];
            for (var i = 0; i < Features.Length; i++)
            {
                clone.Features[i] = Features[i].Clone(clone);
            }

            clone.Outputs = Outputs;
            clone.Symbols = Symbols;

            clone.Weights = (double[])Weights.Clone();

            return clone;
        }

        #endregion
    }
}