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
using System.ComponentModel;
using Accord.MachineLearning;
using Accord.Math.Optimization;

namespace Accord.Statistics.Models.Fields.Learning
{
#pragma warning disable 612, 618
    /// <summary>
    ///     Conjugate Gradient learning algorithm for
    ///     <see cref="HiddenConditionalRandomField{T}">
    ///         Hidden Conditional Hidden Fields
    ///     </see>
    ///     .
    /// </summary>
    /// <example>
    ///     <para>
    ///         For an example on how to learn Hidden Conditional Random Fields, please see the
    ///         <see cref="HiddenResilientGradientLearning{T}">Hidden Resilient Gradient Learning</see>
    ///         page. All learning algorithms can be utilized in a similar manner.
    ///     </para>
    /// </example>
    public class HiddenConjugateGradientLearning<T> : BaseHiddenGradientOptimizationLearning<T, ConjugateGradient>,
        ISupervisedLearning<HiddenConditionalRandomField<T>, T[], int>, IParallel,
        IHiddenConditionalRandomFieldLearning<T>, IConvergenceLearning, IDisposable
    {
        /// <summary>
        ///     Constructs a new Conjugate Gradient learning algorithm.
        /// </summary>
        public HiddenConjugateGradientLearning(HiddenConditionalRandomField<T> model)
        {
            Model = model;
        }

        /// <summary>
        ///     Please use HasConverged instead.
        /// </summary>
        [Obsolete("Please use HasConverged instead.")]
        public bool Converged => HasConverged;

        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => MaxIterations;
            set => MaxIterations = value;
        }

        int IConvergenceLearning.Iterations
        {
            get
            {
                if (Optimizer == null)
                {
                    return 0;
                }

                return Optimizer.Iterations;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the current iteration number.
        /// </summary>
        /// <value>The current iteration.</value>
        public int CurrentIteration
        {
            get
            {
                if (Optimizer != null)
                {
                    return Optimizer.Iterations;
                }

                return 0;
            }
        }

        /// <summary>
        ///     Occurs when the current learning progress has changed.
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        ///     Inheritors of this class should create the optimization algorithm in this
        ///     method, using the current
        ///     <see cref="P:Accord.Statistics.Models.Fields.Learning.BaseHiddenGradientOptimizationLearning`2.MaxIterations" />
        ///     and <see cref="P:Accord.Statistics.Models.Fields.Learning.BaseHiddenGradientOptimizationLearning`2.Tolerance" />
        ///     settings.
        /// </summary>
        /// <returns>ConjugateGradient.</returns>
        protected override ConjugateGradient CreateOptimizer()
        {
            var cg = new ConjugateGradient(Model.Function.Weights.Length)
            {
                Tolerance = Tolerance,
                MaxIterations = MaxIterations
            };

            cg.Progress += progressChanged;
            return cg;
        }

        private void progressChanged(object sender, OptimizationProgressEventArgs e)
        {
            var percentage = 100;
            var ratio = e.GradientNorm / e.SolutionNorm;
            if (!double.IsNaN(ratio))
            {
                percentage = (int)System.Math.Max(0, System.Math.Min(100, (1.0 - ratio) * 100));
            }

            if (ProgressChanged != null)
            {
                ProgressChanged(this, new ProgressChangedEventArgs(percentage, e));
            }
        }
    }
}