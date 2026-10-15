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
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Exact support vector reduction through linear dependency elimination.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The following example shows how to reduce the number of support vectors in
    ///         a SVM by removing vectors which are linearly dependent between themselves.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SupportVectorReductionTest.cs"
    ///         region="doc_learn" />
    /// </example>
    //[Obsolete("Please use SupportVectorReduction<TKernel> instead.")]
    public class SupportVectorReduction
        : SupportVectorReductionBase<SupportVectorMachine<IKernel<double[]>, double[]>,
            IKernel<double[]>, double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SupportVectorReduction" /> class.
        /// </summary>
        /// <param name="machine">The machine to be reduced.</param>
        public SupportVectorReduction(ISupportVectorMachine<double[]> machine)
            : base(machine)
        {
        }
    }

    /// <summary>
    ///     Exact support vector reduction through linear dependency elimination.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The following example shows how to reduce the number of support vectors in
    ///         a SVM by removing vectors which are linearly dependent between themselves.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SupportVectorReductionTest.cs"
    ///         region="doc_learn" />
    /// </example>
    public class SupportVectorReduction<TKernel>
        : SupportVectorReductionBase<SupportVectorMachine<TKernel, double[]>, TKernel, double[]>
        where TKernel : IKernel<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SupportVectorReduction" /> class.
        /// </summary>
        /// <param name="machine">The machine to be reduced.</param>
        public SupportVectorReduction(ISupportVectorMachine<double[]> machine)
            : base(machine)
        {
        }
    }

    /// <summary>
    ///     Exact support vector reduction through linear dependency elimination.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The following example shows how to reduce the number of support vectors in
    ///         a SVM by removing vectors which are linearly dependent between themselves.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SupportVectorReductionTest.cs"
    ///         region="doc_learn" />
    /// </example>
    public class SupportVectorReduction<TKernel, TInput>
        : SupportVectorReductionBase<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
        where TKernel : IKernel<TInput>
        where TInput : ICloneable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SupportVectorReduction" /> class.
        /// </summary>
        /// <param name="machine">The machine to be reduced.</param>
        public SupportVectorReduction(ISupportVectorMachine<TInput> machine)
            : base(machine)
        {
        }
    }

    /// <summary>
    ///     Exact support vector reduction through linear dependency elimination.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The following example shows how to reduce the number of support vectors in
    ///         a SVM by removing vectors which are linearly dependent between themselves.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SupportVectorReductionTest.cs"
    ///         region="doc_learn" />
    /// </example>
    public class SupportVectorReductionBase<TModel, TKernel, TInput>
        : BaseSupportVectorCalibration<TModel, TKernel, TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>
        where TKernel : IKernel<TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        /// <summary>
        ///     Creates a new <see cref="SupportVectorReduction" /> algorithm.
        /// </summary>
        /// <param name="machine">The machine to be reduced.</param>
        public SupportVectorReductionBase(TModel machine)
            : base(machine)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="SupportVectorReduction" /> algorithm.
        /// </summary>
        /// <param name="machine">The machine to be reduced.</param>
        public SupportVectorReductionBase(ISupportVectorMachine<TInput> machine)
            : base(machine)
        {
        }

        /// <summary>
        ///     Gets or sets the minimum threshold that is used to determine
        ///     whether a weight will be kept in the machine or not. Default
        ///     is 1e-12.
        /// </summary>
        public double Threshold { get; set; } = 1e-12;

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        public TModel Learn()
        {
            return Learn(Model.SupportVectors, Classes.Decide(Model.Weights));
        }

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected override void InnerRun()
        {
            var supportVectors = Model.SupportVectors;
            var alpha = (double[])Model.Weights.Clone();
            var kernel = Model.Kernel;

            Input = supportVectors;
            Output = Classes.Decide(alpha);

            // Create Gram matrix
            var K = kernel.ToJagged(supportVectors);

            // Reduce to Echelon form to detect linear dependence
            var ech = new JaggedReducedRowEchelonForm(K);

            var rref = ech.Result;


            // For each support vector
            for (var i = 0; i < supportVectors.Length; i++)
            {
                // Get its corresponding row
                var row = ech.Pivot[i];

                // Check if it can be expressed as a
                // linear combination of other vectors

                if (row > supportVectors.Length - ech.FreeVariables - 1)
                {
                    var c = alpha[row];
                    for (var j = 0; j < rref.Length; j++)
                    {
                        alpha[j] = alpha[j] + c * rref[j][row];
                    }

                    alpha[row] = 0;
                }
            }

            // Retain only multipliers which are not zero
            var idx = alpha.Find(a => System.Math.Abs(a) > Threshold);
            Model.Weights = alpha.Get(idx);
            Model.SupportVectors = supportVectors.Get(idx);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Learn() instead.")]
        public override double Run()
        {
            var classifier = Learn();
            return new ZeroOneLoss(Output).Loss(classifier.Decide(Input));
        }
    }
}