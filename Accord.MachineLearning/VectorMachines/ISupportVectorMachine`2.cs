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

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     Common interface for binary support vector machines.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data handled by the machine.</typeparam>
    public interface ISupportVectorMachine<TInput> : IBinaryLikelihoodClassifier<TInput>
    {
        /// <summary>
        ///     Gets or sets the collection of weights used by this machine.
        /// </summary>
        double[] Weights { get; set; }

        /// <summary>
        ///     Gets or sets the collection of support vectors used by this machine.
        /// </summary>
        TInput[] SupportVectors { get; set; }

        /// <summary>
        ///     Gets or sets the threshold (bias) term for this machine.
        /// </summary>
        double Threshold { get; set; }

        /// <summary>
        ///     Gets or sets the kernel used by this machine.
        /// </summary>
        IKernel<TInput> Kernel { get; set; }

        /// <summary>
        ///     Gets whether this machine has been calibrated to
        ///     produce probabilistic outputs (through the Probability
        ///     method).
        /// </summary>
        bool IsProbabilistic { get; set; }

        /// <summary>
        ///     If this machine has a linear kernel, compresses all
        ///     support vectors into a single parameter vector.
        /// </summary>
        void Compress();
    }
}