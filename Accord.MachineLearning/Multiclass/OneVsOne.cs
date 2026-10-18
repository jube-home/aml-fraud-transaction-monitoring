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

// TODO: Move out of this namespace, as those methods are not restricted to SVMs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines;
using Accord.Math;

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     Decision strategies for
    ///     <see cref="MulticlassSupportVectorMachine{TKernel}">
    ///         Multi-class Support Vector Machines
    ///     </see>
    ///     .
    /// </summary>
    public enum MulticlassComputeMethod
    {
        /// <summary>
        ///     Max-voting method (also known as 1-vs-1 decision).
        /// </summary>
        Voting,

        /// <summary>
        ///     Elimination method (also known as DAG decision).
        /// </summary>
        Elimination
    }
}

namespace Accord.MachineLearning
{
    /// <summary>
    ///     One-Vs-One construction for solving multi-class
    ///     classification using a set of binary classifiers.
    /// </summary>
    /// <typeparam name="TBinary">The type for the binary classifier to be used.</typeparam>
    /// <typeparam name="TInput">The type for the classifier inputs. Default is double[].</typeparam>
    /// <seealso cref="MulticlassSupportVectorMachine{TKernel}" />
    [Serializable]
    public class OneVsOne<TBinary, TInput> :
        MulticlassLikelihoodClassifierBase<TInput>,
        IEnumerable<KeyValuePair<ClassPair, TBinary>>, IParallel
        where TBinary : class, IClassifier<TInput, bool>, ICloneable
    {
        [NonSerialized] private ParallelOptions options = new();

        [NonSerialized] private ThreadLocal<Decision[]> lastDecisionPath;

        private MulticlassComputeMethod method = MulticlassComputeMethod.Elimination;

        /// <summary>
        ///     Gets the pair of class indices handled by each inner binary classification model.
        /// </summary>
        protected ClassPair[] Indices { get; private set; }

        /// <summary>
        ///     Gets the inner binary classification models.
        /// </summary>
        public TBinary[][] Models { get; private set; }

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        public ParallelOptions ParallelOptions
        {
            get => options;
            set => options = value;
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used
        ///     to cancel the algorithm while it is running.
        /// </summary>
        public CancellationToken Token
        {
            get => options.CancellationToken;
            set => options.CancellationToken = value;
        }

        /// <summary>
        ///     Gets or sets the multi-class classification method to be
        ///     used when deciding for the class of a given input vector.
        ///     Default is <see cref="MulticlassComputeMethod.Elimination" />.
        /// </summary>
        public MulticlassComputeMethod Method
        {
            get => method;
            set => method = value;
        }

        /// <summary>
        ///     Gets or sets whether to track the decision path associated
        ///     with each decision. The track will be available through the
        ///     <see cref="GetLastDecisionPath()" /> method. Default is true.
        /// </summary>
        public bool Track { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OneVsOne{TBinary, TInput}" /> class.
        /// </summary>
        /// <param name="classes">The number of classes in the multi-class classification problem.</param>
        /// <param name="initializer">A function to create the inner binary classifiers.</param>
        public OneVsOne(int classes, Func<TBinary> initializer)
        {
            Initialize(classes, initializer);
        }

        /// <summary>
        ///     Gets the last decision path used during the last call to any of the
        ///     model evaluation (Decide, Distance, LogLikelihood, Probability) methods
        ///     in the current thread. This method is thread-safe and returns the value
        ///     obtained in the last call on the current thread.
        /// </summary>
        public Decision[] GetLastDecisionPath()
        {
            return (Decision[])lastDecisionPath.Value.Clone();
        }

        /// <summary>
        ///     Gets the last decision path without cloning.
        /// </summary>
        protected Decision[] LastDecisionPath => lastDecisionPath.Value;

        /// <summary>
        ///     Gets the inner binary classification model used to distinguish between
        ///     the given pair of classes.
        /// </summary>
        /// <param name="classA">The class index for the first class.</param>
        /// <param name="classB">The class index for the second class.</param>
        /// <returns>A binary classifier that can distinguish between the given classes.</returns>
        public TBinary GetClassifierForClassPair(int classA, int classB)
        {
            if (classA == classB)
            {
                throw new ArgumentException();
            }

            if (classA > classB)
            {
                return Models[classA - 1][classB];
            }

            //else
            //return models[classB - 1][classA];
            throw new ArgumentException();
        }

        /// <summary>
        ///     Gets or sets the inner binary classification model used
        ///     to distinguish between the given pair of classes.
        /// </summary>
        /// <param name="classA">The class index for the first class.</param>
        /// <param name="classB">The class index for the second class.</param>
        /// <returns>A binary classifier that can distinguish between the given classes.</returns>
        public TBinary this[int classA, int classB]
        {
            get
            {
                if (classA == classB)
                {
                    return null;
                }

                if (classA > classB)
                {
                    return Models[classA - 1][classB];
                }

                return Models[classB - 1][classA];
            }
            set
            {
                if (classA <= classB)
                {
                    throw new ArgumentOutOfRangeException();
                }

                Models[classA - 1][classB] = value;
            }
        }

        /// <summary>
        ///     Gets a inner binary classification model inside this <see cref="OneVsOne{T}" />
        ///     classifier, together with the pair of classes that it has been designed to
        ///     distinguish.
        /// </summary>
        /// <param name="index">The index of the model (up to <see cref="Count" />).</param>
        public KeyValuePair<ClassPair, TBinary> this[int index] => this.ToArray()[index];

        private void Initialize(int classes, Func<TBinary> initializer)
        {
            if (classes <= 1)
            {
                throw new ArgumentException("Number of classes must be higher than 1.", "classes");
            }

            NumberOfOutputs = classes;
            NumberOfClasses = classes;
            var total = classes * (classes - 1) / 2;

            Models = new TBinary[classes - 1][];
            Indices = new ClassPair[total];
            var k = 0;
            for (var i = 0; i < Models.Length; i++)
            {
                Models[i] = new TBinary[i + 1];
                for (var j = 0; j < Models[i].Length; j++)
                {
                    Indices[k++] = new ClassPair(i + 1, j);
                    var model = initializer();
                    Models[i][j] = model;
                    if (model != null)
                    {
                        NumberOfInputs = model.NumberOfInputs;
                    }
                }
            }

            lastDecisionPath = new ThreadLocal<Decision[]>(() => new Decision[NumberOfOutputs - 1]);
            Track = true;
        }

        [OnDeserialized]
        private void onDeserialized(StreamingContext context)
        {
            lastDecisionPath = new ThreadLocal<Decision[]>(() => new Decision[NumberOfOutputs - 1]);
            ParallelOptions = new ParallelOptions();
        }

        /// <summary>
        ///     Gets the number of inner binary classification models used by
        ///     this instance. It should correspond to <c>(c * (c - 1)) / 2</c>
        ///     where <c>c</c> is the number of classes.
        /// </summary>
        public int Count => Indices.Length;

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public override int Decide(TInput input)
        {
            if (method == MulticlassComputeMethod.Voting)
            {
                return DecideByVoting(input);
            }

            if (Track)
            {
                return DecideByElimination(input, lastDecisionPath.Value);
            }

            return DecideByElimination(input);
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="P:Accord.MachineLearning.ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="result">The location where to store the class-labels.</param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public override int[] Decide(TInput[] input, int[] result)
        {
            if (method == MulticlassComputeMethod.Voting)
            {
                for (var i = 0; i < input.Length; i++)
                {
                    result[i] = DecideByVoting(input[i]);
                }
            }
            else
            {
                if (Track)
                {
                    if (options.MaxDegreeOfParallelism == 1)
                    {
                        for (var i = 0; i < input.Length - 1; i++)
                        {
                            result[i] = DecideByElimination(input[i]);
                        }
                    }
                    else
                    {
                        Parallel.For(0, input.Length - 1, options, i => { result[i] = DecideByElimination(input[i]); });
                    }

                    result[result.Length - 1] = DecideByElimination(input[input.Length - 1], lastDecisionPath.Value);
                }
                else
                {
                    Parallel.For(0, input.Length, options, i => { result[i] = DecideByElimination(input[i]); });
                }
            }

            return result;
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public override double[] Scores(TInput input, double[] result)
        {
            if (method == MulticlassComputeMethod.Voting)
            {
                return DistanceByVoting(input, result);
            }

            if (Track)
            {
                return DistanceByElimination(input, result, lastDecisionPath.Value);
            }

            return DistanceByElimination(input, result);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns></returns>
        public override double[] LogLikelihoods(TInput input, double[] result)
        {
            return Scores(input, result);
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public override double LogLikelihood(TInput input, int classIndex)
        {
            return Scores(input)[classIndex];
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int DecideByVoting(TInput input)
        {
            return DistanceByVoting(input, new double[NumberOfOutputs]).ArgMax();
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int DecideByElimination(TInput input)
        {
            var i = NumberOfOutputs - 1;
            var j = 0;

            while (i != j)
            {
                if (Models[i - 1][j].Decide(input))
                {
                    j++; // i won, so we advance j
                }
                else
                {
                    i--; // j won, so we advance i
                }
            }

            return i;
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int DecideByElimination(TInput input, Decision[] path)
        {
            var i = NumberOfOutputs - 1;
            var j = 0;

            var k = 0;

            while (i != j)
            {
                if (Models[i - 1][j].Decide(input))
                {
                    path[k++] = new Decision(j, i, i);
                    j++; // i won, so we advance j
                }
                else
                {
                    path[k++] = new Decision(i, j, j);
                    i--; // j won, so we advance i
                }
            }

            return i;
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private double[] DistanceByElimination(TInput input, double[] result)
        {
            var i = NumberOfOutputs - 1;
            var j = 0;
            var max = double.NegativeInfinity;

            while (i != j)
            {
                var model = Models[i - 1][j] as IBinaryScoreClassifier<TInput>;

                var sum = model.Score(input, out bool decision);

                if (decision)
                {
                    result[j] = -sum;
                    j++;

                    if (sum > max)
                    {
                        max = sum;
                    }
                }
                else
                {
                    result[i] = sum;
                    i--;

                    if (-sum > max)
                    {
                        max = -sum;
                    }
                }
            }

            result[i] = max;
            return result;
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private double[] DistanceByElimination(TInput input, double[] result, Decision[] path)
        {
            var i = NumberOfOutputs - 1;
            var j = 0;
            var k = 0;
            var max = double.NegativeInfinity;

            while (i != j)
            {
                var model = Models[i - 1][j] as IBinaryScoreClassifier<TInput>;

                var sum = model.Score(input, out bool decision);

                if (decision)
                {
                    result[j] = -sum;
                    path[k++] = new Decision(j, i, i);
                    j++;

                    if (sum > max)
                    {
                        max = sum;
                    }
                }
                else
                {
                    result[i] = sum;
                    path[k++] = new Decision(i, j, j);
                    i--;

                    if (-sum > max)
                    {
                        max = -sum;
                    }
                }
            }

            result[i] = max;
            return result;
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private double[] DistanceByVoting(TInput input, double[] result)
        {
            Parallel.For(0, Indices.Length, options, k =>
            {
                var i = Indices[k].Class1;
                var j = Indices[k].Class2;

                if (Models[i - 1][j].Decide(input))
                {
                    InterlockedEx.Increment(ref result[i]);
                }
                else
                {
                    InterlockedEx.Increment(ref result[j]);
                }
            });

            return result;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through all machines
        ///     contained inside this multi-class support vector machine.
        /// </summary>
        public IEnumerator<KeyValuePair<ClassPair, TBinary>> GetEnumerator()
        {
            for (var i = 0; i < Models.Length; i++)
            {
                for (var j = 0; j < Models[i].Length; j++)
                {
                    yield return new KeyValuePair<ClassPair, TBinary>(new ClassPair(i + 1, j), Models[i][j]);
                }
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through all machines
        ///     contained inside this multi-class support vector machine.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    ///     One-Vs-One construction for solving multi-class
    ///     classification using a set of binary classifiers.
    /// </summary>
    /// <typeparam name="TBinary">The type for the binary classifier to be used.</typeparam>
    /// <seealso cref="MulticlassSupportVectorMachine{TKernel}" />
    [Serializable]
    public class OneVsOne<TBinary> : OneVsOne<TBinary, double[]>
        where TBinary : class, IClassifier<double[], bool>, ICloneable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OneVsOne{TBinary}" /> class.
        /// </summary>
        /// <param name="classes">The number of classes in the multi-class classification problem.</param>
        /// <param name="initializer">A function to create the inner binary classifiers.</param>
        public OneVsOne(int classes, Func<TBinary> initializer)
            : base(classes, initializer)
        {
        }
    }
}