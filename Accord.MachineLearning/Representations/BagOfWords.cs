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
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines;
using Accord.Math;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Bag of words.
    /// </summary>
    /// <remarks>
    ///     The bag-of-words (BoW) model can be used to extract finite
    ///     length features from otherwise varying length representations.
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\BagOfWordsTest.cs" region="doc_learn" />
    ///     <para>
    ///         The following example shows how to use Bag-of-Words to convert other kinds of sequences
    ///         into fixed-length representations. In particular, we apply Bag-of-Words to convert data
    ///         from the PENDIGITS handwritten digit recognition dataset and afterwards convert their
    ///         representations using a <see cref="SupportVectorMachine" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\BagOfWordsTest.cs" region="doc_learn_pendigits" />
    /// </example>
    [Serializable]
    public class BagOfWords : ParallelLearningBase, IBagOfWords<string[]>,
        ITransform<string[], Sparse<double>>,
        IUnsupervisedLearning<BagOfWords, string[], int[]>
    {
        private Dictionary<int, string> codeToString;

        [NonSerialized] private IDictionary<int, string> readOnlyCodeToString;

        [NonSerialized] private IDictionary<string, int> readOnlyStringToCode;
        // TODO: Inherit from BaseBagOfWords

        // TODO: Replace by TwoWayDictionary
        private Dictionary<string, int> stringToCode;

        /// <summary>
        ///     Constructs a new <see cref="BagOfWords" />.
        /// </summary>
        /// <param name="texts">The texts to build the bag of words model from.</param>
        [Obsolete("Please use the default constructor and pass the texts to the Learn() method.")]
        public BagOfWords(params string[][] texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException("texts");
            }

            initialize(texts);
        }

        /// <summary>
        ///     Constructs a new <see cref="BagOfWords" />.
        /// </summary>
        /// <param name="texts">The texts to build the bag of words model from.</param>
        [Obsolete("Please use the default constructor and pass the texts to the Learn() method.")]
        public BagOfWords(params string[] texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException("texts");
            }

            initialize(new[] { texts });
        }

        /// <summary>
        ///     Constructs a new <see cref="BagOfWords" />.
        /// </summary>
        public BagOfWords()
        {
            initialize(null);
        }

        /// <summary>
        ///     Gets the forward dictionary which translates
        ///     string tokens to integer labels.
        /// </summary>
        public IDictionary<string, int> StringToCode => readOnlyStringToCode;

        /// <summary>
        ///     Gets the reverse dictionary which translates
        ///     integer labels into string tokens.
        /// </summary>
        public IDictionary<int, string> CodeToString => readOnlyCodeToString;

        /// <summary>
        ///     Gets or sets the maximum number of occurrences of a word which
        ///     should be registered in the feature vector. Default is 1 (if a
        ///     word occurs, corresponding feature is set to 1).
        /// </summary>
        public int MaximumOccurance { get; set; }

        /// <summary>
        ///     Gets the number of words in this codebook.
        /// </summary>
        public int NumberOfWords => stringToCode.Count;

        /// <summary>
        ///     Gets the number of outputs generated by the model.
        /// </summary>
        /// <value>The number of outputs.</value>
        public int NumberOfOutputs
        {
            get => NumberOfWords;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Gets the number of inputs accepted by the model.
        /// </summary>
        /// <value>The number of inputs.</value>
        public int NumberOfInputs
        {
            get => -1;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        double[] IBagOfWords<string[]>.GetFeatureVector(string[] value)
        {
            return ((ITransform<string[], double[]>)this).Transform(value);
        }

        int[] ICovariantTransform<string[], int[]>.Transform(string[] input)
        {
            return Transform(input, new int[NumberOfWords]);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>The output generated by applying this transformation to the given input.</returns>
        public double[] Transform(string[] input)
        {
            return Transform(input, new double[NumberOfWords]);
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public int[][] Transform(string[][] input, int[][] result)
        {
            Parallel.For(0, input.Length, ParallelOptions, i =>
                Transform(input[i], result[i]));
            return result;
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public double[][] Transform(string[][] input, double[][] result)
        {
            Parallel.For(0, input.Length, ParallelOptions, i =>
                Transform(input[i], result[i]));
            return result;
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public double[][] Transform(string[][] input)
        {
            return Transform(input, Jagged.Zeros<double>(input.Length, NumberOfWords));
        }

        int[][] ICovariantTransform<string[], int[]>.Transform(string[][] input)
        {
            return Transform(input, Jagged.Zeros<int>(input.Length, NumberOfWords));
        }

        Sparse<double> ICovariantTransform<string[], Sparse<double>>.Transform(string[] input)
        {
            return Sparse.FromDense(Transform(input));
        }

        Sparse<double>[] ICovariantTransform<string[], Sparse<double>>.Transform(string[][] input)
        {
            return Transform(input, new Sparse<double>[input.Length]);
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public Sparse<double>[] Transform(string[][] input, Sparse<double>[] result)
        {
            var t = (ITransform<string[], Sparse<double>>)this;
            Parallel.For(0, input.Length, ParallelOptions, i => { result[i] = t.Transform(input[i]); });
            return result;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public BagOfWords Learn(string[][] x, double[] weights = null)
        {
            checkArgs(x, weights);

            // TODO: Use Parallel.For
            var symbol = 0;
            foreach (var text in x)
            {
                foreach (var word in text)
                {
                    if (!stringToCode.ContainsKey(word))
                    {
                        stringToCode[word] = symbol;
                        codeToString[symbol] = word;
                        symbol++;
                    }
                }
            }

            return this;
        }

        private void initialize(string[][] texts)
        {
            stringToCode = new Dictionary<string, int>();
            codeToString = new Dictionary<int, string>();

            readOnlyStringToCode = new ReadOnlyDictionary<string, int>(stringToCode);
            readOnlyCodeToString = new ReadOnlyDictionary<int, string>(codeToString);

            MaximumOccurance = 1;

            if (texts != null)
            {
                Learn(texts);
            }
        }

        /// <summary>
        ///     Computes the Bag of Words model.
        /// </summary>
        [Obsolete("Please use the Learn() method instead.")]
        public void Compute(string[][] texts)
        {
            Learn(texts);
        }

        private static void checkArgs(string[][] texts, double[] weights)
        {
            if (weights != null)
            {
                throw new ArgumentException("Not Supported Weights", "weights");
            }

            if (texts == null)
            {
                throw new ArgumentNullException("texts");
            }

            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                {
                    throw new ArgumentNullException("texts",
                        "Text " + i + "cannot be null.");
                }

                for (var j = 0; j < texts[i].Length; j++)
                {
                    if (texts[i][j] == null)
                    {
                        throw new ArgumentNullException("texts",
                            "Token at text " + i + ", position " + i + " cannot be null.");
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the codeword representation of a given text.
        /// </summary>
        /// <param name="text">The text to be processed.</param>
        /// <returns>
        ///     An integer vector with the same length as words
        ///     in the code book.
        /// </returns>
        [Obsolete("Please use Transform() instead.")]
        public int[] GetFeatureVector(params string[] text)
        {
            return ((ITransform<string[], int[]>)this).Transform(text);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            readOnlyStringToCode = new ReadOnlyDictionary<string, int>(stringToCode);
            readOnlyCodeToString = new ReadOnlyDictionary<int, string>(codeToString);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public double[] Transform(string[] input, double[] result)
        {
            // Detect all feature words
            foreach (var word in input)
            {
                int j;
                if (!stringToCode.TryGetValue(word, out j))
                {
                    continue;
                }

                if (result[j] < MaximumOccurance)
                {
                    result[j]++;
                }
            }

            return result;
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public int[] Transform(string[] input, int[] result)
        {
            // Detect all feature words
            foreach (var word in input)
            {
                int j;
                if (!stringToCode.TryGetValue(word, out j))
                {
                    continue;
                }

                if (result[j] < MaximumOccurance)
                {
                    result[j]++;
                }
            }

            return result;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public BagOfWords Learn(string[] x, double[] weights = null)
        {
            return Learn(x.Tokenize(), weights);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public Sparse<double> Transform(string[] input, Sparse<double> result)
        {
            // Detect all feature words
            foreach (var word in input)
            {
                int j;
                if (!stringToCode.TryGetValue(word, out j))
                {
                    continue;
                }

                if (result[j] < MaximumOccurance)
                {
                    result[j]++;
                }
            }

            return result;
        }

        /// <summary>
        ///     Creates the default clustering algorithm for Bag-of-Words models (<see cref="KMeans" />).
        /// </summary>
        /// <param name="numberOfWords">The number of clusters for k-means.</param>
        public static KMeans GetDefaultClusteringAlgorithm(int numberOfWords)
        {
            return new KMeans(numberOfWords)
            {
                ComputeCovariances = false,
                UseSeeding = Seeding.KMeansPlusPlus
            };
        }
    }
}