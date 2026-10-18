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
    ///     Common interface for generative binary classifiers. A binary classifier can
    ///     predict whether or not an instance belongs to a class, while at the same time
    ///     being able to provide the probability of this sample belonging to the positive
    ///     class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    public interface IBinaryLikelihoodClassifier<TInput> :
        IBinaryScoreClassifier<TInput>,
        IMulticlassOutLikelihoodClassifier<TInput, bool>,
        IMulticlassLikelihoodClassifier<TInput>
    {
        /// <summary>
        ///     Views this instance as a multi-class generative classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of integer labels.
        /// </summary>
        /// <returns>This instance seen as an <see cref="IMulticlassLikelihoodClassifier{TInput}" />.</returns>
        new IMulticlassLikelihoodClassifier<TInput> ToMulticlass();

        /// <summary>
        ///     Views this instance as a multi-class generative classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of integer labels.
        /// </summary>
        /// <returns>This instance seen as an <see cref="IMulticlassLikelihoodClassifier{TInput}" />.</returns>
        new IMulticlassLikelihoodClassifier<TInput, T> ToMulticlass<T>();

        //new IMultilabelGenerativeClassifier<TInput> ToMultilabel();
    }
}