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
    ///     Common interface for score-based multi-label classifiers. A multi-label
    ///     classifier can predict the occurrence of multiple class labels at once
    ///     based on a decision score (a real number) computed for each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int[].</typeparam>
    public interface IMultilabelScoreClassifierBase<in TInput, TClasses>
    {
        /// <summary>
        ///     Predicts a class label vector for each input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        double[][] Scores(TInput[] input, ref TClasses[] decision);

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[][] Scores(TInput[] input, ref TClasses[] decision, double[][] result);
    }

    /// <summary>
    ///     Common interface for score-based multi-label classifiers. A multi-label
    ///     classifier can predict the occurrence of multiple class labels at once
    ///     based on a decision score (a real number) computed for each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int[].</typeparam>
    public interface IMultilabelRefScoreClassifier<TInput, TClasses> :
        IMultilabelScoreClassifierBase<TInput, TClasses>
    {
        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        double[] Scores(TInput input, ref TClasses decision);

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[] Scores(TInput input, ref TClasses decision, double[] result);
    }

    /// <summary>
    ///     Common interface for score-based multi-label classifiers. A multi-label
    ///     classifier can predict the occurrence of multiple class labels at once
    ///     based on a decision score (a real number) computed for each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int[].</typeparam>
    public interface IMultilabelOutScoreClassifier<TInput, TClasses> :
        IMultilabelScoreClassifierBase<TInput, TClasses>
    {
        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        double[] Scores(TInput input, out TClasses decision);

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[] Scores(TInput input, out TClasses decision, double[] result);
    }

    /// <summary>
    ///     Common interface for score-based multi-label classifiers. A multi-label
    ///     classifier can predict the occurrence of multiple class labels at once
    ///     based on a decision score (a real number) computed for each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int[].</typeparam>
    public interface IMultilabelScoreClassifier<TInput, TClasses> :
        IMultilabelOutScoreClassifier<TInput, TClasses>,
        IMultilabelRefScoreClassifier<TInput, TClasses[]>,
        IClassifier<TInput, TClasses>
    {
    }

    /// <summary>
    ///     Common interface for score-based multi-label classifiers. A multi-label
    ///     classifier can predict the occurrence of multiple class labels at once
    ///     based on a decision score (a real number) computed for each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    public interface IMultilabelScoreClassifier<TInput> :
        IMultilabelScoreClassifier<TInput, int>,
        IMultilabelScoreClassifier<TInput, double>,
        IMultilabelRefScoreClassifier<TInput, bool[]>,
        IMultilabelClassifier<TInput>
    {
        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and a given
        ///     <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        double Score(TInput input, int classIndex);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and the
        ///     given <paramref name="classIndex">class indices</paramref>.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="classIndex">
        ///     The class indices associated with each
        ///     input vector whose scores will be computed.
        /// </param>
        double[] Score(TInput[] input, int[] classIndex);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and the
        ///     given <paramref name="classIndex">class indices</paramref>.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="classIndex">
        ///     The class indices associated with each
        ///     input vector whose scores will be computed.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[] Score(TInput[] input, int[] classIndex, double[] result);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and the
        ///     given <paramref name="classIndex">class indices</paramref>.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        double[] Score(TInput[] input, int classIndex);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and the
        ///     given <paramref name="classIndex">class indices</paramref>.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[] Score(TInput[] input, int classIndex, double[] result);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        double[] Scores(TInput input);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[] Scores(TInput input, double[] result);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and each
        ///     possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        double[][] Scores(TInput[] input);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and each
        ///     possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[][] Scores(TInput[] input, double[][] result);

        /// <summary>
        ///     Views this instance as a multi-class score-based classifier.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMulticlassScoreClassifierBase{TInput, TClasses}" />.
        /// </returns>
        IMulticlassScoreClassifier<TInput> ToMulticlass();

        /// <summary>
        ///     Views this instance as a multi-class score-based classifier.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMulticlassScoreClassifierBase{TInput, TClasses}" />.
        /// </returns>
        IMulticlassScoreClassifier<TInput, T> ToMulticlass<T>();
    }
}