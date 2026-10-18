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

using System.Threading.Tasks;

namespace Accord.MachineLearning // TODO: Move those classes to the root Accord namespace
{
    /// <summary>
    ///     Common interface for parallel algorithms.
    /// </summary>
    public interface IParallel : ISupportsCancellation
    {
        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm. It can
        ///     be used to control the maximum number of cores that should be used
        ///     during the algorithm's execution.
        /// </summary>
        /// <remarks>
        ///     The <see cref="IParallel" /> interface is implemented by most machine learning
        ///     algorithms in the framework, and it is most common use is to allow the user to
        ///     tune how many cores should be used by a multi-threaded learning algorithm.
        /// </remarks>
        /// <example>
        ///     <para>
        ///         In the following example, we will be using the <see cref="ParallelOptions" /> property to limit
        ///         the maximum degree of parallelism of a support vector machine learning algorithm to be 1, meaning
        ///         the algorithm will be running in a single thread:
        ///     </para>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
        ///         region="doc_learn_ldcd" />
        /// </example>
        ParallelOptions ParallelOptions { get; set; }
    }
}