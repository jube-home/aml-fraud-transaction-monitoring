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
using System.Threading;
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.Math;

namespace Accord.MachineLearning
{
#if !NET35 && !NET40
#else
    using Accord.Collections;
#endif

    /// <summary>
    ///     Weighting schemes for term-frequency (TF).
    /// </summary>
    public enum TermFrequency
    {
        /// <summary>
        ///     Binary TF variant (0, 1).
        /// </summary>
        Binary,

        /// <summary>
        ///     Raw frequency variant (f_{t,d}).
        /// </summary>
        Default,

        /// <summary>
        ///     Log normalization (1 + log(f_{t,d})).
        /// </summary>
        Log,

        /// <summary>
        ///     Double normalization (0.5 + 0.5 { f_{t,d} / max_t'{ f_{t',d} } }
        /// </summary>
        DoubleNormalization
    }

    /// <summary>
    ///     Weighting schemes for Inverse Document Frequency (IDF).
    /// </summary>
    public enum InverseDocumentFrequency
    {
        /// <summary>
        ///     Unary (1).
        /// </summary>
        Unary,

        /// <summary>
        ///     Inverse document frequency, log(N / n_t).
        /// </summary>
        Default,

        /// <summary>
        ///     Smooth inverse document frequency, log(N / (1 + n_t)).
        /// </summary>
        Smooth,

        /// <summary>
        ///     Max inverse document frequency, log( max_t'{n_t} / n_t).
        /// </summary>
        Max,

        /// <summary>
        ///     Probabilistic inverse document frequency, log((N -n_t) / n_t).
        /// </summary>
        Probabilistic
    }

    /// <summary>
    ///     Term Frequency - Inverse Term Frequency.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
    public class TFIDF : ParallelLearningBase,
        ITransform<string[], double[]>,
        ITransform<string[], Sparse<double>>,
        IUnsupervisedLearning<TFIDF, string[], double[]>
    {
        private BagOfWords bow;
        private InverseDocumentFrequency idf = MachineLearning.InverseDocumentFrequency.Default;

        private TermFrequency tf = TermFrequency.Default;

        /// <summary>
        ///     Constructs a new <see cref="TFIDF" />.
        /// </summary>
        public TFIDF()
        {
            bow = new BagOfWords
            {
                MaximumOccurance = int.MaxValue
            };

            UpdateDictionary = true;
        }

        /// <summary>
        ///     Constructs a new <see cref="TFIDF" />.
        /// </summary>
        public TFIDF(params string[][] texts)
            : this()
        {
            bow.Learn(texts);
            UpdateDictionary = false;
        }

        /// <summary>
        ///     Gets the number of documents that contain each code word. Each element
        ///     is associated with a word, and the value of the element gives the number
        ///     of documents that contain this word.
        /// </summary>
        public int[] Counts { get; private set; }

        /// <summary>
        ///     Gets the total number of documents considered by this TF-IDF.
        /// </summary>
        public int NumberOfDocuments { get; private set; }

        /// <summary>
        ///     Gets the number of words in this codebook.
        /// </summary>
        public int NumberOfWords => bow.NumberOfWords;

        /// <summary>
        ///     Gets or sets the inverse document frequency (IDF) definition to be used.
        /// </summary>
        public InverseDocumentFrequency Idf
        {
            get => idf;
            set => idf = value;
        }

        /// <summary>
        ///     Gets or sets the term frequency (TF) definition to be used.
        /// </summary>
        public TermFrequency Tf
        {
            get => tf;
            set => tf = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether new words should be added to the
        ///     dictionary in the next call to <see cref="Learn(string[][], double[])" />.
        /// </summary>
        public bool UpdateDictionary { get; set; }

        /// <summary>
        ///     Gets the inverse document frequency vector used to scale term-frequency vectors.
        /// </summary>
        public double[] InverseDocumentFrequency { get; private set; }

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
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public Sparse<double>[] Transform(string[][] input, Sparse<double>[] result)
        {
            Parallel.For(0, input.Length, ParallelOptions,
                () => new double[NumberOfWords],
                (i, state, work) =>
                {
                    Transform(input[i], out result[i], work);
                    Array.Clear(work, 0, work.Length);
                    return work;
                },
                work => { });

            return result;
        }

        Sparse<double> ICovariantTransform<string[], Sparse<double>>.Transform(string[] input)
        {
            return Transform(input, out _);
        }

        Sparse<double>[] ICovariantTransform<string[], Sparse<double>>.Transform(string[][] input)
        {
            return Transform(input, new Sparse<double>[input.Length]);
        }

        /// <summary>
        ///     Gets the number of inputs accepted by the model.
        /// </summary>
        /// <value>The number of inputs.</value>
        public int NumberOfInputs
        {
            get => bow.NumberOfInputs;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Gets the number of outputs generated by the model.
        /// </summary>
        /// <value>The number of outputs.</value>
        public int NumberOfOutputs
        {
            get => bow.NumberOfOutputs;
            set => throw new InvalidOperationException("This property is read-only.");
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
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public double[][] Transform(string[][] input, double[][] result)
        {
            Parallel.For(0, input.Length, ParallelOptions,
                () => new double[NumberOfWords],
                (i, state, work) =>
                {
                    Sparse<double> r;
                    Transform(input[i], out r, work);
                    Array.Clear(work, 0, work.Length);
                    r.CopyTo(result[i], 0);
                    return work;
                },
                work => { });

            return result;
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>The output generated by applying this transformation to the given input.</returns>
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
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
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public double[][] Transform(string[][] input)
        {
            return Transform(input, Jagged.Zeros(input.Length, NumberOfWords));
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
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public TFIDF Learn(string[][] x, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException("Not Supported Weights", "weights");
            }

            if (UpdateDictionary)
            {
                bow.Learn(x);
            }

            var codebook = bow.StringToCode;
            var totalCounts = new int[NumberOfWords];

            Parallel.For(0, x.Length, ParallelOptions,
                () => Tuple.Create(new int[NumberOfWords], new bool[NumberOfWords]),
                (i, state, work) =>
                {
                    var partialCount = work.Item1;
                    var instanceCount = work.Item2;
                    var words = x[i];

                    for (var j = 0; j < words.Length; j++)
                    {
                        int k;
                        if (!codebook.TryGetValue(words[j], out k))
                        {
                            continue;
                        }

                        instanceCount[k] = true;
                    }

                    for (var j = 0; j < instanceCount.Length; j++)
                    {
                        if (instanceCount[j])
                        {
                            partialCount[j]++;
                        }
                    }

                    Array.Clear(instanceCount, 0, instanceCount.Length);
                    return Tuple.Create(partialCount, instanceCount);
                },
                work =>
                {
                    var partialCount = work.Item1;
                    for (var i = 0; i < totalCounts.Length; i++)
                    {
                        Interlocked.Add(ref totalCounts[i], partialCount[i]);
                    }
                });

            Counts = totalCounts;
            NumberOfDocuments = x.Length;
            InverseDocumentFrequency = new double[NumberOfWords];

            switch (idf)
            {
                case MachineLearning.InverseDocumentFrequency.Unary:
                    for (var i = 0; i < Counts.Length; i++)
                    {
                        InverseDocumentFrequency[i] = 1;
                    }

                    break;
                case MachineLearning.InverseDocumentFrequency.Default:
                    for (var i = 0; i < Counts.Length; i++)
                    {
                        if (Counts[i] != 0)
                        {
                            InverseDocumentFrequency[i] = System.Math.Log(x.Length / (double)Counts[i]);
                        }
                    }

                    break;
                case MachineLearning.InverseDocumentFrequency.Smooth:
                    for (var i = 0; i < Counts.Length; i++)
                    {
                        InverseDocumentFrequency[i] = System.Math.Log(x.Length / (1.0 + Counts[i]));
                    }

                    break;
                case MachineLearning.InverseDocumentFrequency.Max:
                    double max = Counts.Max();
                    for (var i = 0; i < Counts.Length; i++)
                    {
                        InverseDocumentFrequency[i] = System.Math.Log(max / (1.0 + Counts[i]));
                    }

                    break;
                case MachineLearning.InverseDocumentFrequency.Probabilistic:
                    for (var i = 0; i < Counts.Length; i++)
                    {
                        if (Counts[i] != 0)
                        {
                            InverseDocumentFrequency[i] = System.Math.Log((x.Length - Counts[i]) / (double)Counts[i]);
                        }
                    }

                    break;
                default:
                    throw new InvalidOperationException("Unknown InverseDocumentFrequency: {0}".Format(idf));
            }

            Debug.Assert(!InverseDocumentFrequency.HasNaN());
            Debug.Assert(!InverseDocumentFrequency.HasInfinity());

            return this;
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
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public Sparse<double>[] Transform(string[][] input, out Sparse<double>[] result)
        {
            return result = Transform(input, new Sparse<double>[input.Length]);
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
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public double[] Transform(string[] input, double[] result)
        {
            Sparse<double> r;
            Transform(input, out r, new double[NumberOfWords]);
            r.CopyTo(result, 0);
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
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public Sparse<double> Transform(string[] input, Sparse<double> result)
        {
            return Transform(input, out result, new double[NumberOfWords]);
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
        /// <example>
        ///     <para>
        ///         Examples are available in the main documentation page for
        ///         <see cref="TFIDF" />. One of those examples is reproduced below:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\TFIDFTest.cs" region="doc_learn" />
        /// </example>
        public Sparse<double> Transform(string[] input, out Sparse<double> result)
        {
            return Transform(input, out result, new double[NumberOfWords]);
        }

        private Sparse<double> Transform(string[] x, out Sparse<double> sparse, double[] work)
        {
            var codebook = bow.StringToCode;

            for (var j = 0; j < x.Length; j++)
            {
                int k;
                if (!codebook.TryGetValue(x[j], out k))
                {
                    continue;
                }

                work[k]++;
            }

            sparse = Sparse.FromDense(work);

            switch (tf)
            {
                case TermFrequency.Binary:
                    for (var j = 0; j < sparse.Values.Length; j++)
                    {
                        sparse.Values[j] = sparse.Values[j] = 1;
                    }

                    break;
                case TermFrequency.Default:
                    break;
                case TermFrequency.Log:
                    for (var j = 0; j < sparse.Values.Length; j++)
                    {
                        sparse.Values[j] = 1 + System.Math.Log(sparse.Values[j]);
                    }

                    break;
                case TermFrequency.DoubleNormalization:
                    var max = sparse.Values.Max();
                    for (var j = 0; j < sparse.Values.Length; j++)
                    {
                        sparse.Values[j] = 0.5 + 0.5 * (sparse.Values[j] / max);
                    }

                    break;
                default:
                    throw new InvalidOperationException("Unknown TermFrequency: {0}".Format(tf));
            }

            // Divide by the inverse document frequency
            for (var j = 0; j < sparse.Values.Length; j++)
            {
                var a = sparse.Values[j];
                var k = sparse.Indices[j];
                var v = a * InverseDocumentFrequency[k];

#if DEBUG
                if (double.IsNaN(v) || double.IsInfinity(v))
                {
                    throw new Exception();
                }
#endif
                sparse.Values[j] = v;
            }

            return sparse;
        }
    }
}