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
    ///     Common interface for multi-class models. Classification models
    ///     learn how to produce a class-label <c>y</c> from an input vector <c>x</c>.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int.</typeparam>
    public interface IMulticlassClassifier<in TInput, TClasses> :
        IClassifier<TInput, TClasses>
    {
    }

    /// <summary>
    ///     Common interface for multi-class models. Classification models
    ///     learn how to produce a class-label <c>y</c> from an input vector <c>x</c>.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    public interface IMulticlassClassifier<in TInput> :
        IMultilabelClassifier<TInput>,
        IMulticlassClassifier<TInput, int>,
        IMulticlassClassifier<TInput, double>
    {
        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        new int Decide(TInput input);

        /// <summary>
        ///     Computes class-label decisions for each vector in the given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vectors that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     The class-labels that best describe each <paramref name="input" />
        ///     vectors according to this classifier.
        /// </returns>
        new int[] Decide(TInput[] input);

        /// <summary>
        ///     Views this instance as a multi-label classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of one-hot vectors.
        /// </summary>
        /// <returns>This instance seen as an <see cref="IMultilabelLikelihoodClassifier{TInput}" />.</returns>
        IMultilabelClassifier<TInput> ToMultilabel();
    }

    /// <summary>
    ///     Common interface for multi-class models. Classification models
    ///     learn how to produce a class-label <c>y</c> from an input vector <c>x</c>.
    /// </summary>
    public interface IMulticlassClassifier :
        IMulticlassClassifier<double[]>,
        IMulticlassClassifier<int[]>,
        IMulticlassClassifier<float[]>
    {
    }
}