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
using Accord.MachineLearning;
using Accord.Math.Optimization;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Fields.Learning
{
    /// <summary>
    ///     Quasi-Newton (L-BFGS) learning algorithm for
    ///     <see cref="HiddenConditionalRandomField{T}">
    ///         Hidden Conditional Hidden Fields
    ///     </see>
    ///     .
    /// </summary>
    /// <typeparam name="T">The type of the observations.</typeparam>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_1" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_2" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_3" />
    ///     <para>
    ///         The next example shows how to use the learning algorithms in a real-world dataset,
    ///         including training and testing in separate sets and evaluating its performance:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\Learning\NormalQuasiNewtonHiddenLearningTest.cs"
    ///         region="doc_learn_pendigits" />
    /// </example>
    /// <seealso cref="HiddenGradientDescentLearning{T}" />
    /// <seealso cref="HiddenResilientGradientLearning{T}" />
    public class HiddenQuasiNewtonLearning<T> :
        BaseHiddenGradientOptimizationLearning<T, BoundedBroydenFletcherGoldfarbShanno>,
        ISupervisedLearning<HiddenConditionalRandomField<T>, T[], int>, IParallel,
        IHiddenConditionalRandomFieldLearning<T>, IConvergenceLearning, IDisposable
    {
        /// <summary>
        ///     Constructs a new L-BFGS learning algorithm.
        /// </summary>
        public HiddenQuasiNewtonLearning()
        {
        }

        /// <summary>
        ///     Constructs a new L-BFGS learning algorithm.
        /// </summary>
        public HiddenQuasiNewtonLearning(HiddenConditionalRandomField<T> model)
        {
            Model = model;
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
        ///     Inheritors of this class should create the optimization algorithm in this
        ///     method, using the current
        ///     <see cref="P:Accord.Statistics.Models.Fields.Learning.BaseHiddenGradientOptimizationLearning`2.MaxIterations" />
        ///     and <see cref="P:Accord.Statistics.Models.Fields.Learning.BaseHiddenGradientOptimizationLearning`2.Tolerance" />
        ///     settings.
        /// </summary>
        /// <returns>BoundedBroydenFletcherGoldfarbShanno.</returns>
        protected override BoundedBroydenFletcherGoldfarbShanno CreateOptimizer()
        {
            var lbfgs = new BoundedBroydenFletcherGoldfarbShanno(Model.Function.Weights.Length)
            {
                FunctionTolerance = Tolerance,
                MaxIterations = MaxIterations
            };

            for (var i = 0; i < lbfgs.UpperBounds.Length; i++)
            {
                lbfgs.UpperBounds[i] = 1e10;
                lbfgs.LowerBounds[i] = -1e100;
            }

            return lbfgs;
        }
    }
}