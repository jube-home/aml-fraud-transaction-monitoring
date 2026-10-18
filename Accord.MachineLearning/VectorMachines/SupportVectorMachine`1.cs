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
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     Sparse Kernel Support Vector Machine (kSVM)
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The original optimal hyperplane algorithm (SVM) proposed by Vladimir Vapnik in 1963 was a
    ///         linear classifier. However, in 1992, Bernhard Boser, Isabelle Guyon and Vapnik suggested
    ///         a way to create non-linear classifiers by applying the kernel trick (originally proposed
    ///         by Aizerman et al.) to maximum-margin hyperplanes. The resulting algorithm is formally
    ///         similar, except that every dot product is replaced by a non-linear kernel function.
    ///     </para>
    ///     <para>
    ///         This allows the algorithm to fit the maximum-margin hyperplane in a transformed feature space.
    ///         The transformation may be non-linear and the transformed space high dimensional; thus though
    ///         the classifier is a hyperplane in the high-dimensional feature space, it may be non-linear in
    ///         the original input space.
    ///     </para>
    ///     <para>
    ///         The machines are also able to learn sequence classification problems in which the input vectors
    ///         can have arbitrary length. For an example on how to do that, please see the documentation page
    ///         for the DynamicTimeWarping kernel.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Support_vector_machine">
    ///                         http://en.wikipedia.org/wiki/Support_vector_machine
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.kernel-machines.org/">
    ///                         http://www.kernel-machines.org/
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The first example shows how to learn an SVM using a
    ///         standard kernel that operates on vectors of doubles.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_normal" />
    ///     <para>
    ///         The second example shows how to learn an SVM using a
    ///         Sparse kernel that operates on sparse vectors.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_sparse" />
    /// </example>
    /// <seealso cref="Statistics.Kernels" />
    /// <seealso cref="MulticlassSupportVectorMachine{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorMachine{TKernel}" />
    /// <seealso cref="Learning.SequentialMinimalOptimization{TKernel}" />
    [Serializable]
    public class SupportVectorMachine<TKernel> : SupportVectorMachine<TKernel, double[]>
        where TKernel : IKernel<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SupportVectorMachine{TKernel}" /> class.
        /// </summary>
        /// <param name="inputs">The number of inputs for this machine.</param>
        /// <param name="kernel">The kernel function to be used.</param>
        public SupportVectorMachine(int inputs, TKernel kernel)
            : base(inputs, kernel)
        {
        }

        /// <summary>
        ///     Converts a <see cref="Linear" />-kernel machine into an array of
        ///     linear coefficients. The first position in the array is the
        ///     <see cref="SupportVectorMachine{TKernel, TInput}.Threshold" /> value. If this
        ///     machine is not linear, an exception will be thrown.
        /// </summary>
        /// <returns>
        ///     An array of linear coefficients representing this machine.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the <see cref="SupportVectorMachine{TKernel, TInput}.Kernel">kernel function</see> is not
        ///     <see cref="Linear" />.
        /// </exception>
        public override double[] ToWeights()
        {
            if (!(Kernel is Linear))
            {
                throw new InvalidOperationException();
            }

            var w = new double[NumberOfInputs + 1];

            for (var i = 0; i < SupportVectors.Length; i++)
            {
                for (var j = 0; j < SupportVectors[i].Length; j++)
                {
                    w[j + 1] += SupportVectors[i][j];
                }
            }

            w[0] = Threshold;

            return w;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            var clone = new SupportVectorMachine<TKernel>(NumberOfInputs, Kernel);
            clone.SupportVectors = Matrix.MemberwiseClone(SupportVectors);
            clone.Weights = (double[])Weights.Clone();
            clone.IsProbabilistic = IsProbabilistic;
            clone.Threshold = Threshold;
            return clone;
        }
    }
}