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
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Common interface for Support Machine Vector learning algorithms.
    /// </summary>
    public interface ILinearSupportVectorMachineLearning :
        ISupervisedLearning<SupportVectorMachine, double[], double>,
        ISupervisedLearning<SupportVectorMachine, double[], int>,
        ISupervisedLearning<SupportVectorMachine, double[], bool>,
        ISupportVectorMachineLearning
    {
    }

    /*public interface ISupportVectorMachineLearning :
        ISupportVectorMachineLearning<double[]>
    {
    }*/

    /*
    /// <summary>
    ///   Common interface for Support Machine Vector learning algorithms.
    /// </summary>
    ///
    public interface ISupportVectorMachineLearning<TInput> :
        ISupervisedBinaryLearning<SupportVectorMachine<IKernel<TInput>, TInput>, TInput>,
        ISupervisedLearning<SupportVectorMachine<IKernel<TInput>, TInput>, TInput, double>
    {

    }*/

    // TODO: Remove those interfaces

    /// <summary>
    ///     Common interface for Support Machine Vector learning algorithms.
    /// </summary>
    public interface ISupportVectorMachineLearning :
        ISupportVectorMachineLearning<double[]>
    {
    }

    /// <summary>
    ///     Common interface for Support Machine Vector learning algorithms.
    /// </summary>
    public interface ISupportVectorMachineLearning<TInput> :
        ISupervisedBinaryLearning<ISupportVectorMachine<TInput>, TInput>,
        ISupervisedLearning<ISupportVectorMachine<TInput>, TInput, double>
    {
        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete]
        double Run();

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete]
        double Run(bool computeError);
    }

    /// <summary>
    ///     Common interface for Support Machine Vector learning algorithms.
    /// </summary>
    public interface ISupportVectorMachineLearning<TKernel, TInput> :
        ISupervisedBinaryLearning<SupportVectorMachine<TKernel, TInput>, TInput>,
        ISupervisedLearning<SupportVectorMachine<TKernel, TInput>, TInput, double>
        where TKernel : IKernel<TInput>
        where TInput : ICloneable
    {
        /// <summary>
        ///     Gets or sets the support vector machine being learned.
        /// </summary>
        SupportVectorMachine<TKernel, TInput> Model { get; set; }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete]
        double Run();

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete]
        double Run(bool computeError);
    }
}