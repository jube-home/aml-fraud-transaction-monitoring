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

using System.Runtime.CompilerServices;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     Contains classes related to <see cref="SupportVectorMachine">Support Vector Machines</see> (SVMs).
    ///     Contains <see cref="SupportVectorMachine">linear machines</see>,
    ///     <see cref="SupportVectorMachine{TKernel}">
    ///         kernel machines
    ///     </see>
    ///     , <see cref="MulticlassSupportVectorMachine{TKernel}">multi-class machines</see>, SVM-DAGs
    ///     (Directed Acyclic Graphs), <see cref="MultilabelSupportVectorMachine{TKernel}">multi-label classification</see>
    ///     and also offers support for the
    ///     <see cref="ProbabilisticOutputCalibration">
    ///         probabilistic output calibration
    ///     </see>
    ///     of SVM outputs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This namespace contains both standard <see cref="SupportVectorMachine" />s and the
    ///         kernel extension given by <see cref="SupportVectorMachine{TKernel}" />s. For multiple
    ///         classes or categories, the framework offers <see cref="MulticlassSupportVectorMachine{TKernel}" />s
    ///         and <see cref="MultilabelSupportVectorMachine{TKernel}" />s. Multi-class machines can be used for
    ///         cases where a single class should be picked up from a list of several class labels, and
    ///         the multi-label machine for cases where multiple class labels might be detected for a
    ///         single input vector. The multi-class machines also support two types of classification:
    ///         the faster decision based on Decision Directed Acyclic Graphs, and the more traditional
    ///         based on a Voting scheme.
    ///     </para>
    ///     <para>
    ///         Learning can be achieved using the standard <see cref="SequentialMinimalOptimization{TKernel}" />
    ///         (SMO) algorithm. However, the framework can also learn Least Squares SVMs (LS-SVMs) using
    ///         <see
    ///             cref="LeastSquaresLearning" />
    ///         , and even calibrate SVMs to produce probabilistic outputs
    ///         using <see cref="ProbabilisticOutputCalibration" />. A
    ///         <see cref="Statistics.Kernels">
    ///             huge variety of kernels functions is available in the statistics namespace
    ///         </see>
    ///         , and
    ///         new kernels can be created easily using the <see cref="IKernel" /> interface.
    ///     </para>
    ///     <para>
    ///         The namespace class diagram is shown below.
    ///     </para>
    ///     <img src="..\diagrams\classes\Accord.MachineLearning.VectorMachines.png" />
    ///     <para>
    ///         Please note that class diagrams for each of the inner namespaces are
    ///         also available within their own documentation pages.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Learning" />
    /// <seealso cref="Statistics.Kernels" />
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}