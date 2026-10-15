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

using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     One-against-one Multi-class Support Vector Machine Learning Algorithm
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class can be used to train Kernel Support Vector Machines with
    ///         any algorithm using a <c>one-against-one</c> strategy. The underlying
    ///         training algorithm can be configured by defining the
    ///         <see cref="OneVsOneLearning{TInput, TBinary, TInput}.Learner" />
    ///         property.
    ///     </para>
    ///     <para>
    ///         One example of learning algorithm that can be used with this class is the
    ///         <see cref="SequentialMinimalOptimization{TKernel}">
    ///             Sequential Minimal Optimization
    ///         </see>
    ///         (SMO) algorithm.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to learn a linear, multi-class support vector
    ///         machine using the <see cref="LinearDualCoordinateDescent" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_ldcd" />
    ///     <para>
    ///         The following example shows how to learn a non-linear, multi-class support
    ///         vector machine using the <see cref="Gaussian" /> kernel and the
    ///         <see cref="SequentialMinimalOptimization{TKernel}" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_gaussian" />
    ///     <para>
    ///         Support vector machines can have their weights calibrated in order to produce
    ///         probability estimates (instead of simple class separation distances). The
    ///         following example shows how to use <see cref="ProbabilisticOutputCalibration" />
    ///         within <see cref="MulticlassSupportVectorLearning{TKernel}" /> to generate a probabilistic
    ///         SVM:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_calibration" />
    /// </example>
    /// <seealso cref="SequentialMinimalOptimization{TKernel}" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    /// <seealso cref="SupportVectorMachine" />
    public class MulticlassSupportVectorLearning<TKernel> :
        BaseMulticlassSupportVectorLearning<double[],
            SupportVectorMachine<TKernel>, TKernel,
            MulticlassSupportVectorMachine<TKernel>>
        where TKernel : IKernel<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MulticlassSupportVectorLearning{TKernel}" /> class.
        /// </summary>
        public MulticlassSupportVectorLearning()
        {
            Learner = p => new SequentialMinimalOptimization<TKernel>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MulticlassSupportVectorLearning{TKernel}" /> class.
        /// </summary>
        /// <param name="machine">The existing machine to be learned.</param>
        public MulticlassSupportVectorLearning(MulticlassSupportVectorMachine<TKernel> machine)
        {
            Model = machine;
            Learner = p => new SequentialMinimalOptimization<TKernel>
            {
                Model = Model[p.Pair.Class1, p.Pair.Class2]
            };
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override MulticlassSupportVectorMachine<TKernel> Create(int inputs, int classes)
        {
            return new MulticlassSupportVectorMachine<TKernel>(inputs, Kernel, classes);
        }
    }
}