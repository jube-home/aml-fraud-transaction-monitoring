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

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Common interface for multi-label classifiers. A multi-label classifier can
    ///     predict the occurrence of multiple class labels at once.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int[].</typeparam>
    public interface IMultilabelClassifier<in TInput, TClasses> :
        IClassifier<TInput, TClasses>
    {
        /// <summary>
        ///     Computes class-label decisions for the given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vectors that should be classified as
        ///     any of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="result">The location where to store the class-labels.</param>
        /// <returns>
        ///     A set of class-labels that best describe the <paramref name="input" />
        ///     vectors according to this classifier.
        /// </returns>
        TClasses Decide(TInput input, TClasses result);
    }

    /// <summary>
    ///     Common interface for multi-label classifiers. A multi-label classifier can
    ///     predict the occurrence of multiple class labels at once.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    public interface IMultilabelClassifier<in TInput> :
        IMultilabelClassifier<TInput, int[]>,
        IMultilabelClassifier<TInput, bool[]>,
        IMultilabelClassifier<TInput, double[]>
    {
        ///// <summary>
        /////   Computes whether a class label applies to an <paramref name="input"/> vector.
        ///// </summary>
        ///// 
        ///// <param name="input">The input vectors that should be classified as
        /////   any of the <see cref="ITransform.NumberOfOutputs"/> possible classes.</param>
        ///// <param name="classIndex">The class label index to be tested.</param>
        ///// 
        ///// <returns>A boolean value indicating whether the given <paramref name="classIndex">
        ///// class label</paramref> applies to the <paramref name="input"/> vector.</returns>
        ///// 
        //bool Decide(TInput input, int classIndex);
    }

    /// <summary>
    ///     Common interface for multi-label classifiers. A multi-label classifier can
    ///     predict the occurrence of multiple class labels at once.
    /// </summary>
    public interface IMultilabelClassifier :
        IMultilabelClassifier<int[]>,
        IMultilabelClassifier<float[]>,
        IMultilabelClassifier<double[]>
    {
    }
}