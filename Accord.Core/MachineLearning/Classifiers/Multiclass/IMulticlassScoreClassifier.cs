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
    ///     Common interface for score-based multi-class classifiers. A multi-class
    ///     classifier can predict to which class an instance belongs based
    ///     on a decision score (a real number) that measures the association of the
    ///     input with each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int.</typeparam>
    public interface IMulticlassScoreClassifierBase<in TInput, TClasses>
    {
        /// <summary>
        ///     Predicts a class label for each input vector, returning a
        ///     numerical score measuring the strength of association of the
        ///     input vector to the most strongly related class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels predicted for each input
        ///     vector, as predicted by the classifier.
        /// </param>
        double[] Score(TInput[] input, ref TClasses[] decision);

        /// <summary>
        ///     Predicts a class label for each input vector, returning a
        ///     numerical score measuring the strength of association of the
        ///     input vector to the most strongly related class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels predicted for each input
        ///     vector, as predicted by the classifier.
        /// </param>
        /// <param name="result">
        ///     An array where the distances will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[] Score(TInput[] input, ref TClasses[] decision, double[] result);
    }

    /// <summary>
    ///     Common interface for score-based multi-class classifiers. A multi-class
    ///     classifier can predict to which class an instance belongs based
    ///     on a decision score (a real number) that measures the association of the
    ///     input with each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int.</typeparam>
    public interface IMulticlassOutScoreClassifier<TInput, TClasses> :
        IMulticlassScoreClassifierBase<TInput, TClasses>,
        IMultilabelOutScoreClassifier<TInput, TClasses>
    {
        /// <summary>
        ///     Predicts a class label for the input vector, returning a
        ///     numerical score measuring the strength of association of the
        ///     input vector to its most strongly related class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        double Score(TInput input, out TClasses decision);
    }

    /// <summary>
    ///     Common interface for score-based multi-class classifiers. A multi-class
    ///     classifier can predict to which class an instance belongs based
    ///     on a decision score (a real number) that measures the association of the
    ///     input with each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int.</typeparam>
    public interface IMulticlassRefScoreClassifier<TInput, TClasses> :
        //IMulticlassDistanceClassifier<TInput, TClasses>,
        IMultilabelRefScoreClassifier<TInput, TClasses>
    {
        ///// <summary>
        /////   Predicts a class label for the input vector, returning a
        /////   numerical score measuring the strength of association of the
        /////   input vector to its most strongly related class.
        ///// </summary>
        ///// 
        ///// <param name="input">The input vector.</param>
        ///// <param name="decision">The class label predicted by the classifier.</param>
        ///// 
        //double Distance(TInput input, ref TClasses decision);
    }

    /// <summary>
    ///     Common interface for score-based multi-class classifiers. A multi-class
    ///     classifier can predict to which class an instance belongs based
    ///     on a decision score (a real number) that measures the association of the
    ///     input with each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    /// <typeparam name="TClasses">The data type for the class labels. Default is int.</typeparam>
    public interface IMulticlassScoreClassifier<TInput, TClasses>
        : IMulticlassOutScoreClassifier<TInput, TClasses>,
            IMulticlassRefScoreClassifier<TInput, TClasses[]>,
            IClassifier<TInput, TClasses>
    {
    }

    /// <summary>
    ///     Common interface for score-based multi-class classifiers. A multi-class
    ///     classifier can predict to which class an instance belongs based
    ///     on a decision score (a real number) that measures the association of the
    ///     input with each class.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    public interface IMulticlassScoreClassifier<TInput> :
        IMulticlassScoreClassifier<TInput, int>,
        IMulticlassScoreClassifier<TInput, double>,
        IMulticlassRefScoreClassifier<TInput, bool[]>,
        IMultilabelScoreClassifier<TInput>,
        IMulticlassClassifier<TInput>
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
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and its most strongly
        ///     associated class (as predicted by the classifier).
        /// </summary>
        /// <param name="input">The input vector.</param>
        double Score(TInput input);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and their
        ///     respective most strongly associated classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        double[] Score(TInput[] input);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     each of the given <paramref name="input" /> vectors and their
        ///     respective most strongly associated classes.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        double[] Score(TInput[] input, double[] result);

        /// <summary>
        ///     Views this instance as a multi-label distance classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of one-hot vectors.
        /// </summary>
        /// <returns>This instance seen as an <see cref="IMultilabelScoreClassifier{TInput}" />.</returns>
        new IMultilabelScoreClassifier<TInput> ToMultilabel();
    }
}