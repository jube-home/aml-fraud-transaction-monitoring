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
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Base learning algorithm for <see cref="OneVsRest{TBinary, TModel}" /> multi-class classifiers.
    /// </summary>
    /// <typeparam name="TBinary">The type for the inner binary classifiers used in the one-vs-rest approach.</typeparam>
    /// <typeparam name="TModel">The type of the model being learned.</typeparam>
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    public abstract class OneVsRestLearning<TBinary, TModel> :
        OneVsRestLearning<double[], TBinary, TModel>,
        ISupervisedLearning<TModel, double[], int>
        where TModel : OneVsRest<TBinary>
        where TBinary : class, IBinaryScoreClassifier<double[]>
    {
    }

    /// <summary>
    ///     Base learning algorithm for <see cref="OneVsRest{TBinary, TModel}" /> multi-class classifiers.
    /// </summary>
    /// <typeparam name="TInput">The type for the samples handled by the classifier. Default is double[].</typeparam>
    /// <typeparam name="TBinary">The type for the inner binary classifiers used in the one-vs-rest approach.</typeparam>
    /// <typeparam name="TModel">The type of the model being learned.</typeparam>
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    [Serializable]
    public abstract class OneVsRestLearning<TInput, TBinary, TModel> :
        ParallelLearningBase,
        ISupervisedLearning<TModel, TInput, int>,
        ISupervisedLearning<TModel, TInput, int[]>,
        ISupervisedLearning<TModel, TInput, bool[]>
        where TBinary : IBinaryScoreClassifier<TInput>
        where TModel : OneVsRest<TBinary, TInput>
    {
        private bool aggregateExceptions = true;
        private bool? isMultilabel = false;
        private Func<InnerParameters<TBinary, TInput>, ISupervisedLearning<TBinary, TInput, bool>> learner;
        private ConcurrentDictionary<ClassPair, ISupervisedLearning<TBinary, TInput, bool>> teachers;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OneVsRestLearning{TInput, TBinary, TModel}" /> class.
        /// </summary>
        protected OneVsRestLearning()
        {
        }

        /// <summary>
        ///     Gets or sets the model being learned.
        /// </summary>
        public TModel Model { get; set; }

        /// <summary>
        ///     Gets or sets a function that takes a set of parameters and creates
        ///     a learning algorithm for learning each of the binary inner classifiers
        ///     needed by the one-vs-rest classification strategy.
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
        ///     Gets or sets a value indicating whether the learning algorithm should generate multi-label
        ///     (as opposed to multi-class) models. If left unspecified, the type of the model will be determined
        ///     automatically depending on which overload of the <see cref="Learn(TInput[], bool[][], double[])" />
        ///     method will be called first by the executing code.
        /// </summary>
        public bool? IsMultilabel
        {
            get => isMultilabel;
            set => isMultilabel = value;
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
        public virtual TModel Learn(TInput[] x, bool[][] y, double[] weights = null)
        {
            Tools.CheckArgs(x, y, weights, () =>
            {
                if (Model == null)
                {
                    teachers = null;
                    if (isMultilabel == null)
                    {
                        isMultilabel = y.IsMultilabel();
                    }

                    var numberOfInputs = Tools.GetNumberOfInputs(x);
                    var numberOfClasses = y.Columns();
                    Model = Create(numberOfInputs, numberOfClasses, isMultilabel.Value);
                }

                return Model;
            });

            if (teachers == null)
            {
                teachers = new ConcurrentDictionary<ClassPair, ISupervisedLearning<TBinary, TInput, bool>>();
            }

            var total = Model.NumberOfClasses;
            var progress = 0;

            // Save exceptions but process all machines
            var exceptions = new ConcurrentBag<Exception>();

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                // For each class k
                for (var k = 0; k < total; k++)
                {
                    TrainBinaryMachine(x, y, weights, k, total, ref progress, exceptions);
                }
            }
            else
            {
                // For each class k
                Parallel.For(0, total, ParallelOptions, k =>
                    TrainBinaryMachine(x, y, weights, k, total, ref progress, exceptions));
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
            if (isMultilabel == null)
            {
                isMultilabel = false;
            }

            return Learn(x, Jagged.OneHot<bool>(y), weights);
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
        public TModel Learn(TInput[] x, int[][] y, double[] weights = null)
        {
            return Learn(x, Classes.Decide(y), weights);
        }

        /// <summary>
        ///     Occurs when the learning of a subproblem has started.
        /// </summary>
        public event EventHandler<SubproblemEventArgs> SubproblemStarted;

        /// <summary>
        ///     Occurs when the learning of a subproblem has finished.
        /// </summary>
        public event EventHandler<SubproblemEventArgs> SubproblemFinished;

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

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected abstract TModel Create(int inputs, int classes, bool multilabel);

        private void TrainBinaryMachine(TInput[] x, bool[][] y, double[] weights, int k, int total, ref int progress,
            ConcurrentBag<Exception> exceptions)
        {
            if (ParallelOptions.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            // We will start the binary sub-problem
            var pair = new ClassPair(k, -k);
            var args = new SubproblemEventArgs(k, -k)
            {
                Maximum = total
            };
            OnSubproblemStarted(args);


            // Retrieve the associated machine
            var model = Model.Models[k];

            // Extract outputs for the given label
            var suby = y.GetColumn(k);

            if (aggregateExceptions)
            {
                try
                {
                    // Train the machine on the two-class problem.
                    TrainBinaryMachine(pair, k, model, x, suby, weights);
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
                TrainBinaryMachine(pair, k, model, x, suby, weights);
            }

            // Update and report progress
            args.Progress = Interlocked.Increment(ref progress);
            args.Maximum = total;

            OnSubproblemFinished(args);
        }

        private void TrainBinaryMachine(ClassPair pair, int k, TBinary model, TInput[] x, bool[] suby, double[] weights)
        {
            // Configure the machine on the two-class problem. Check if the learner
            // for this machine has already been created before, and re-use it if it
            // was the case. This is necessary to support mini-batch/online learning.

            ISupervisedLearning<TBinary, TInput, bool> subproblemTeacher;
            if (!teachers.TryGetValue(pair, out subproblemTeacher))
            {
                var p = new InnerParameters<TBinary, TInput>(inputs: x, outputs: suby, pair: pair, model: model);
                subproblemTeacher = Learner(p);
                teachers[pair] = subproblemTeacher;
            }

            if (subproblemTeacher != null)
            {
                // TODO: This check only exists to provide support to previous way of 
                // using the library and should be removed after a few releases. In the
                // current way (without using any Obsolete methods), subproblem should never be null.
                subproblemTeacher.Token = ParallelOptions.CancellationToken;
                Model[k] = subproblemTeacher.Learn(x, suby, weights);
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