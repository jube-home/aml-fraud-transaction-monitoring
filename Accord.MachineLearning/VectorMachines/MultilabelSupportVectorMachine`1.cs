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
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     One-against-all Multi-label Kernel Support Vector Machine Classifier.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Support Vector Machine is by nature a binary classifier. Multiple label
    ///         problems are problems in which an input sample is allowed to belong to one
    ///         or more classes. A way to implement multi-label classes in support vector
    ///         machines is to build a one-against-all decision scheme where multiple SVMs
    ///         are trained to detect each of the available classes.
    ///     </para>
    ///     <para>
    ///         Currently this class supports only Kernel machines as the underlying classifiers.
    ///         If a Linear Support Vector Machine is needed, specify a Linear kernel in the
    ///         constructor at the moment of creation.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://courses.media.mit.edu/2006fall/mas622j/Projects/aisen-project/index.html">
    ///                         http://courses.media.mit.edu/2006fall/mas622j/Projects/aisen-project/index.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://nlp.stanford.edu/IR-book/html/htmledition/multiclass-svms-1.html">
    ///                         http://nlp.stanford.edu/IR-book/html/htmledition/multiclass-svms-1.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to learn a linear, multi-label (one-vs-rest) support
    ///         vector machine using the <see cref="LinearDualCoordinateDescent" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_ldcd" />
    ///     <para>
    ///         The following example shows how to learn a non-linear, multi-label (one-vs-rest)
    ///         support vector machine using the <see cref="Gaussian" /> kernel and the
    ///         <see cref="SequentialMinimalOptimization{TKernel}" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_gaussian" />
    ///     <para>
    ///         Support vector machines can have their weights calibrated in order to produce probability
    ///         estimates (instead of simple class separation distances). The following example shows how
    ///         to use <see cref="ProbabilisticOutputCalibration" /> within
    ///         <see cref="MulticlassSupportVectorLearning{TKernel}" />
    ///         to generate a probabilistic SVM:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_calibration" />
    /// </example>
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    /// <seealso cref="MulticlassSupportVectorMachine{TKernel}" />
    [Serializable]
    public class MultilabelSupportVectorMachine<TKernel> :
        MultilabelSupportVectorMachine<
            SupportVectorMachine<TKernel>,
            TKernel,
            double[]>
        where TKernel : IKernel<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorMachine{TKernel}" /> class.
        /// </summary>
        /// <param name="classes">The number of classes in the multi-label classification problem.</param>
        /// <param name="initializer">A function to create the inner binary classifiers.</param>
        public MultilabelSupportVectorMachine(int classes, Func<SupportVectorMachine<TKernel>> initializer)
            : base(classes, initializer)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorMachine{TKernel}" /> class.
        /// </summary>
        /// <param name="inputs">The number of inputs by the machine.</param>
        /// <param name="classes">The number of classes to be handled by the machine.</param>
        /// <param name="kernel">The kernel function to be used in the machine.</param>
        public MultilabelSupportVectorMachine(int inputs, TKernel kernel, int classes)
            : base(classes, () => new SupportVectorMachine<TKernel>(inputs, kernel))
        {
        }
    }
}