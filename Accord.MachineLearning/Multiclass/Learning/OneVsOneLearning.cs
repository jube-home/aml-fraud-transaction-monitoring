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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Base learning algorithm for <see cref="OneVsOne{TBinary, TBinary}" /> multi-class classifiers.
    /// </summary>
    /// <typeparam name="TBinary">The type for the inner binary classifiers used in the one-vs-one approach.</typeparam>
    /// <typeparam name="TModel">The type of the model being learned.</typeparam>
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    public abstract class OneVsOneLearning<TBinary, TModel> :
        OneVsOneLearning<double[], TBinary, TModel>
        where TModel : OneVsOne<TBinary>
        where TBinary : class, IBinaryClassifier<double[]>, ICloneable
    {
    }

    /// <summary>
    ///     Base learning algorithm for <see cref="OneVsOne{TBinary, TBinary}" /> multi-class classifiers.
    /// </summary>
    /// <typeparam name="TInput">The type for the samples handled by the classifier. Default is double[].</typeparam>
    /// <typeparam name="TBinary">The type for the inner binary classifiers used in the one-vs-one approach.</typeparam>
    /// <typeparam name="TModel">The type of the model being learned.</typeparam>
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    [Serializable]
    public abstract class OneVsOneLearning<TInput, TBinary, TModel> : ParallelLearningBase,
        ISupervisedLearning<TModel, TInput, int>
        where TModel : OneVsOne<TBinary, TInput>
        where TBinary : class, IBinaryClassifier<TInput>, ICloneable
    {
        private bool aggregateExceptions = true;
        private Func<InnerParameters<TBinary, TInput>, ISupervisedLearning<TBinary, TInput, bool>> learner;
        private ClassPair[] pairs;
        private ConcurrentDictionary<ClassPair, ISupervisedLearning<TBinary, TInput, bool>> teachers;

        /// <summary>
        ///     Gets or sets the model being learned.
        /// </summary>
        public TModel Model { get; set; }

        /// <summary>
        ///     Gets or sets a function that takes a set of parameters and creates
        ///     a learning algorithm for learning each of the binary inner classifiers
        ///     needed by the one-vs-one classification strategy.
        /// </summary>
        public Func<InnerParameters<TBinary, TInput>, ISupervisedLearning<TBinary, TInput, bool>> Learner
        {
            get => learner;
            set
            {
                learner = value;
                teachers = null; // reset the teaching algorithm cache
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the entire training algorithm should stop
        ///     in case an exception has been detected at just one of the inner binary learning
        ///     problems. Default is true (execution will not be stopped).
        /// </summary>
        public bool AggregateExceptions
        {
            get => aggregateExceptions;
            set => aggregateExceptions = value;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public TModel Learn(TInput[] x, int[] y, double[] weights = null)
        {
            Tools.CheckArgs(x, y, weights, () =>
            {
                if (Model == null)
                {
                    teachers = null;
                    var numberOfInputs = Tools.GetNumberOfInputs(x);
                    var numberOfClasses = y.DistinctCount();
                    Model = Create(numberOfInputs, numberOfClasses);
                }

                return Model;
            });

            if (pairs == null)
            {
                teachers = null;
                var classes = Model.NumberOfClasses;
                var total = classes * (classes - 1) / 2;
                pairs = new ClassPair[total];
                for (int i = 0, k = 0; i < classes; i++)
                {
                    for (var j = 0; j < i; j++, k++)
                    {
                        pairs[k] = new ClassPair(i, j);
                    }
                }
            }

            if (teachers == null)
            {
                teachers = new ConcurrentDictionary<ClassPair, ISupervisedLearning<TBinary, TInput, bool>>();
            }

            var progress = 0;

            // Save exceptions but process all machines
            var exceptions = new ConcurrentBag<Exception>();

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                // For each class k
                for (var k = 0; k < pairs.Length; k++)
                {
                    TrainBinaryMachine(x, y, weights, ref progress, pairs[k], exceptions);
                }
            }
            else
            {
                // For each class k
                Parallel.For(0, pairs.Length, ParallelOptions, k =>
                    TrainBinaryMachine(x, y, weights, ref progress, pairs[k], exceptions));
            }


            if (exceptions.Count > 0)
            {
                throw new AggregateException("One or more exceptions were thrown when teaching "
                                             + "the machines. Please check the InnerException property of this AggregateException "
                                             + "to discover what exactly caused this error.", exceptions);
            }

            return Model;
        }

        /// <summary>
        ///     Occurs when the learning of a subproblem has started.
        /// </summary>
        public event EventHandler<SubproblemEventArgs> SubproblemStarted;

        /// <summary>
        ///     Occurs when the learning of a subproblem has finished.
        /// </summary>
        public event EventHandler<SubproblemEventArgs> SubproblemFinished;

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected abstract TModel Create(int inputs, int classes);

        // Overloads to simplify call from F#
        /// <summary>
        ///     Sets a callback function that takes a set of parameters and creates
        ///     a learning algorithm for learning each of the binary inner classifiers
        ///     needed by the one-vs-rest classification strategy. Calling this method
        ///     sets the <see cref="Learner" /> property.
        /// </summary>
        public void Configure<T, TResult>(Func<T, TResult> learner)
            where T : InnerParameters<TBinary, TInput>
            where TResult : ISupervisedLearning<TBinary, TInput, bool>
        {
            Learner = p => learner((T)p);
        }

        /// <summary>
        ///     Sets a callback function that takes a set of parameters and creates
        ///     a learning algorithm for learning each of the binary inner classifiers
        ///     needed by the one-vs-rest classification strategy. Calling this method
        ///     sets the <see cref="Learner" /> property.
        /// </summary>
        public void Configure<TResult>(Func<TResult> learner)
            where TResult : ISupervisedLearning<TBinary, TInput, bool>
        {
            Learner = p => learner();
        }

        private void TrainBinaryMachine(TInput[] x, int[] y, double[] weights, ref int progress, ClassPair pair,
            ConcurrentBag<Exception> exceptions)
        {
            if (ParallelOptions.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var i = pair.Class1;
            var j = pair.Class2;

            // We will start the binary sub-problem
            var args = new SubproblemEventArgs(i, j);
            OnSubproblemStarted(args);

            // Retrieve the associated machine
            var model = Model.GetClassifierForClassPair(i, j);

            // Retrieve the associated classes
            var idx = y.Find(y_i => y_i == i || y_i == j);

            if (idx.Length == 0)
            {
                Trace.TraceWarning("Class pair ({0}, {1}) does not have any examples.", i, j);
            }

            var subx = x.Get(idx);
            var suby = y.Get(idx).Apply(y_i => y_i == i);

            double[] subw = null;
            if (weights != null)
            {
                subw = weights.Get(idx);
            }

            if (aggregateExceptions)
            {
                try
                {
                    // Train the machine on the two-class problem.
                    TrainBinaryMachine(pair, i, j, model, subx, suby, subw);
                }
                catch (Exception ex)
                {
                    ex.Data["pair"] = pair;
                    exceptions.Add(ex);
                }
            }
            else
            {
                // Train the machine on the two-class problem.
                TrainBinaryMachine(pair, i, j, model, subx, suby, subw);
            }

            // Update and report progress
            args.Progress = Interlocked.Increment(ref progress);
            args.Maximum = pairs.Length;

            OnSubproblemFinished(args);
        }

        private void TrainBinaryMachine(ClassPair pair, int i, int j, TBinary model, TInput[] subx, bool[] suby,
            double[] subw)
        {
            // Configure the machine on the two-class problem. Check if the learner
            // for this machine has already been created before, and re-use it if it
            // was the case. This is necessary to support mini-batch/online learning.

            ISupervisedLearning<TBinary, TInput, bool> subproblemTeacher;
            if (!teachers.TryGetValue(pair, out subproblemTeacher))
            {
                var p = new InnerParameters<TBinary, TInput>(inputs: subx, outputs: suby, pair: pair, model: model);
                subproblemTeacher = Learner(p);
                teachers[pair] = subproblemTeacher;
            }

            if (subproblemTeacher != null)
            {
                // TODO: This check only exists to provide support to previous way of 
                // using the library and should be removed after a few releases. In the
                // current way (without using any Obsolete methods), subproblem should never be null.
                subproblemTeacher.Token = ParallelOptions.CancellationToken;
                Model[i, j] = subproblemTeacher.Learn(subx, suby, subw);
            }
        }

        /// <summary>
        ///     Raises the <see cref="E:SubproblemFinished" /> event.
        /// </summary>
        /// <param name="args">The <see cref="SubproblemEventArgs" /> instance containing the event data.</param>
        protected void OnSubproblemFinished(SubproblemEventArgs args)
        {
            if (SubproblemFinished != null)
            {
                SubproblemFinished(this, args);
            }
        }

        /// <summary>
        ///     Raises the <see cref="E:SubproblemStarted" /> event.
        /// </summary>
        /// <param name="args">The <see cref="SubproblemEventArgs" /> instance containing the event data.</param>
        protected void OnSubproblemStarted(SubproblemEventArgs args)
        {
            if (SubproblemStarted != null)
            {
                SubproblemStarted(this, args);
            }
        }
    }
}