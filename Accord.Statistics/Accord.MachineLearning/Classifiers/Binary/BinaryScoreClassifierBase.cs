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
using Accord.Math;
using Accord.Statistics;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Base class for
    ///     <see cref="IBinaryScoreClassifier{TInput}">
    ///         score-based binary classifiers
    ///     </see>
    ///     .
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    [Serializable]
    public abstract class BinaryScoreClassifierBase<TInput> :
        BinaryClassifierBase<TInput>,
        IBinaryScoreClassifier<TInput>
    {
        internal const double SCORE_DECISION_THRESHOLD = 0;
        internal const int CLASS_POSITIVE = 1;
        internal const int CLASS_NEGATIVE = 0;

        // Main, overridable methods

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public abstract double[] Score(TInput[] input, double[] result);

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="P:Accord.MachineLearning.ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="result">The location where to store the class-labels.</param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public override bool[] Decide(TInput[] input, bool[] result)
        {
            Score(input, ref result, new double[input.Length]);
            return result;
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="P:Accord.MachineLearning.ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public override bool Decide(TInput input)
        {
            return Decide(new[] { input }, new bool[1])[0];
        }

        // with class index

        double IMultilabelScoreClassifier<TInput>.Score(TInput input, int classIndex)
        {
            var d = Score(input);
            return classIndex == 0 ? -d : +d;
        }

        double[] IMultilabelScoreClassifier<TInput>.Score(TInput[] input, int[] classIndex)
        {
            return ToMultilabel().Score(input, classIndex, new double[input.Length]);
        }

        double[] IMultilabelScoreClassifier<TInput>.Score(TInput[] input, int classIndex)
        {
            return ToMultilabel().Score(input, classIndex, new double[input.Length]);
        }

        double[] IMultilabelScoreClassifier<TInput>.Score(TInput[] input, int[] classIndex, double[] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Scores(input[i])[classIndex[i]];
            }

            return result;
        }

        double[] IMultilabelScoreClassifier<TInput>.Score(TInput[] input, int classIndex, double[] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Scores(input[i])[classIndex];
            }

            return result;
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public override double[][] Transform(TInput[] input, double[][] result)
        {
            return Scores(input, result);
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public override double[] Transform(TInput[] input, double[] result)
        {
            return Score(input, result);
        }

        int IMulticlassScoreClassifier<TInput>.Decide(TInput input)
        {
            return ((IClassifier<TInput, int>)this).Decide(input);
        }

        int[] IMulticlassScoreClassifier<TInput>.Decide(TInput[] input)
        {
            return ((IClassifier<TInput, int>)this).Decide(input);
        }

        /// <summary>
        ///     Views this instance as a multi-class distance classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of integer labels.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMulticlassScoreClassifier{TInput}" />.
        /// </returns>
        public new IMulticlassScoreClassifier<TInput> ToMulticlass()
        {
            return this;
        }

        /// <summary>
        ///     Views this instance as a multi-class distance classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of integer labels.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMulticlassScoreClassifier{TInput}" />.
        /// </returns>
        public new IMulticlassScoreClassifier<TInput, T> ToMulticlass<T>()
        {
            return (IMulticlassScoreClassifier<TInput, T>)this;
        }

        /// <summary>
        ///     Views this instance as a multi-label distance classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of one-hot vectors.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMultilabelScoreClassifier{TInput}" />.
        /// </returns>
        public new IMultilabelScoreClassifier<TInput> ToMultilabel()
        {
            return this;
        }

        // Transform

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public override double[] Transform(TInput input, double[] result)
        {
            return Scores(input, result);
        }

        #region Score

        // Input

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and its most strongly
        ///     associated class (as predicted by the classifier).
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double Score(TInput input)
        {
            return Score(new[] { input })[0];
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double[] Scores(TInput input)
        {
            return Scores(input, new double[NumberOfClasses]);
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and its most strongly
        ///     associated class (as predicted by the classifier).
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double[] Score(TInput[] input)
        {
            return Score(input, new double[input.Length]);
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double[][] Scores(TInput[] input)
        {
            return Scores(input, create<double>(input));
        }

        // Input, result
        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Scores(TInput input, double[] result)
        {
            return Scores(new[] { input }, new[] { result })[0];
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Scores(TInput[] input, double[][] result)
        {
            var p = new TInput[1];
            var o = new double[1];

            for (var i = 0; i < input.Length; i++)
            {
                p[0] = input[i];
                var d = Score(p, o)[0];
                result[i][CLASS_NEGATIVE] = -d;
                result[i][CLASS_POSITIVE] = +d;
            }

            return result;
        }

        // Input, decision

        /// <summary>
        ///     Predicts a class label for the input vector, returning a
        ///     numerical score measuring the strength of association of the
        ///     input vector to its most strongly related class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <returns></returns>
        public double Score(TInput input, out bool decision)
        {
            var d = new bool[1];
            var s = Score(new[] { input }, ref d)[0];
            decision = d[0];
            return s;
        }

        double IMulticlassOutScoreClassifier<TInput, double>.Score(TInput input, out double decision)
        {
            var d = new bool[1];
            var s = Score(new[] { input }, ref d)[0];
            decision = d[0].ToZeroOne();
            return s;
        }

        double IMulticlassOutScoreClassifier<TInput, int>.Score(TInput input, out int decision)
        {
            var d = new bool[1];
            var s = Score(new[] { input }, ref d)[0];
            decision = d[0].ToZeroOne();
            return s;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        public double[] Scores(TInput input, out bool decision)
        {
            return Scores(input, out decision, new double[NumberOfClasses]);
        }

        double[] IMultilabelOutScoreClassifier<TInput, int>.Scores(TInput input, out int decision)
        {
            return ToMultilabel().Scores(input, out decision, new double[NumberOfClasses]);
        }

        double[] IMultilabelOutScoreClassifier<TInput, double>.Scores(TInput input, out double decision)
        {
            return ToMultilabel().Scores(input, out decision, new double[NumberOfClasses]);
        }

        double[] IMultilabelRefScoreClassifier<TInput, bool[]>.Scores(TInput input, ref bool[] decision)
        {
            return ToMultilabel().Scores(input, ref decision, new double[NumberOfClasses]);
        }

        double[] IMultilabelRefScoreClassifier<TInput, int[]>.Scores(TInput input, ref int[] decision)
        {
            return ToMultilabel().Scores(input, ref decision, new double[NumberOfClasses]);
        }

        double[] IMultilabelRefScoreClassifier<TInput, double[]>.Scores(TInput input, ref double[] decision)
        {
            return ToMultilabel().Scores(input, ref decision, new double[NumberOfClasses]);
        }

        // Input, decision, result

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning a
        ///     numerical score measuring the strength of association of the input vector
        ///     to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Scores(TInput input, out bool decision, double[] result)
        {
            var d = new bool[1];
            var r = new[] { result };
            var s = Scores(new[] { input }, ref d, r)[0];
            decision = d[0];
            return s;
        }

        double[] IMultilabelOutScoreClassifier<TInput, double>.Scores(TInput input, out double decision,
            double[] result)
        {
            var d = new double[1];
            var r = new[] { result };
            var s = Scores(new[] { input }, ref d, r)[0];
            decision = d[0];
            return s;
        }

        double[] IMultilabelOutScoreClassifier<TInput, int>.Scores(TInput input, out int decision, double[] result)
        {
            var d = new int[1];
            var r = new[] { result };
            var s = Scores(new[] { input }, ref d, r)[0];
            decision = d[0];
            return s;
        }

        double[] IMultilabelRefScoreClassifier<TInput, bool[]>.Scores(TInput input, ref bool[] decision,
            double[] result)
        {
            var d = new bool[1];
            var r = new[] { result };
            var s = Scores(new[] { input }, ref d, r)[0];
            Vector.OneHot(d[0], decision);
            return s;
        }

        double[] IMultilabelRefScoreClassifier<TInput, int[]>.Scores(TInput input, ref int[] decision, double[] result)
        {
            var d = new bool[1];
            var r = new[] { result };
            var s = Scores(new[] { input }, ref d, r)[0];
            Vector.OneHot(d[0], decision);
            return s;
        }

        double[] IMultilabelRefScoreClassifier<TInput, double[]>.Scores(TInput input, ref double[] decision,
            double[] result)
        {
            var d = new bool[1];
            var r = new[] { result };
            var s = Scores(new[] { input }, ref d, r)[0];
            Vector.OneHot(d[0], decision);
            return s;
        }

        // Input[], decision[]

        double[] IMulticlassScoreClassifierBase<TInput, int>.Score(TInput[] input, ref int[] decision)
        {
            return ToMulticlass().Score(input, ref decision, new double[input.Length]);
        }

        double[] IMulticlassScoreClassifierBase<TInput, double>.Score(TInput[] input, ref double[] decision)
        {
            return ToMulticlass().Score(input, ref decision, new double[input.Length]);
        }

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
        public double[] Score(TInput[] input, ref bool[] decision)
        {
            return Score(input, ref decision, new double[input.Length]);
        }

        double[][] IMultilabelScoreClassifierBase<TInput, int>.Scores(TInput[] input, ref int[] decision)
        {
            return ToMultilabel().Scores(input, ref decision, create<double>(input));
        }

        double[][] IMultilabelScoreClassifierBase<TInput, double>.Scores(TInput[] input, ref double[] decision)
        {
            return ToMultilabel().Scores(input, ref decision, create<double>(input));
        }

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
        public double[][] Scores(TInput[] input, ref bool[] decision)
        {
            return Scores(input, ref decision, create<double>(input));
        }

        double[][] IMultilabelScoreClassifierBase<TInput, int[]>.Scores(TInput[] input, ref int[][] decision)
        {
            return ToMultilabel().Scores(input, ref decision, create<double>(input));
        }

        double[][] IMultilabelScoreClassifierBase<TInput, bool[]>.Scores(TInput[] input, ref bool[][] decision)
        {
            return ToMultilabel().Scores(input, ref decision, create<double>(input));
        }

        double[][] IMultilabelScoreClassifierBase<TInput, double[]>.Scores(TInput[] input, ref double[][] decision)
        {
            return ToMultilabel().Scores(input, ref decision, create<double>(input));
        }

        // Input[], decision[], result[]        

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
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Score(TInput[] input, ref bool[] decision, double[] result)
        {
            Score(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < input.Length; i++)
            {
                decision[i] = Classes.Decide(result[i] - SCORE_DECISION_THRESHOLD);
            }

            return result;
        }

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
        /// <returns>System.Double[].</returns>
        public double[] Score(TInput[] input, ref int[] decision, double[] result)
        {
            Score(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < input.Length; i++)
            {
                decision[i] = (result[i] - SCORE_DECISION_THRESHOLD).ToZeroOne();
            }

            return result;
        }

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
        /// <returns>System.Double[].</returns>
        public double[] Score(TInput[] input, ref double[] decision, double[] result)
        {
            Score(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < input.Length; i++)
            {
                decision[i] = (result[i] - SCORE_DECISION_THRESHOLD).ToZeroOne();
            }

            return result;
        }

        double[][] IMultilabelScoreClassifierBase<TInput, bool[]>.Scores(TInput[] input, ref bool[][] decision,
            double[][] result)
        {
            ToMultilabel().Scores(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < result.Length; i++)
            {
                Vector.OneHot((result[i][CLASS_POSITIVE] - SCORE_DECISION_THRESHOLD).ToZeroOne(),
                    decision[i]);
            }

            return result;
        }

        double[][] IMultilabelScoreClassifierBase<TInput, int[]>.Scores(TInput[] input, ref int[][] decision,
            double[][] result)
        {
            ToMultilabel().Scores(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < result.Length; i++)
            {
                Vector.OneHot((result[i][CLASS_POSITIVE] - SCORE_DECISION_THRESHOLD).ToZeroOne(),
                    decision[i]);
            }

            return result;
        }

        double[][] IMultilabelScoreClassifierBase<TInput, double[]>.Scores(TInput[] input, ref double[][] decision,
            double[][] result)
        {
            ToMultilabel().Scores(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < result.Length; i++)
            {
                Vector.OneHot<double>((result[i][CLASS_POSITIVE] - SCORE_DECISION_THRESHOLD).ToZeroOne(),
                    decision[i]);
            }

            return result;
        }

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
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Scores(TInput[] input, ref bool[] decision, double[][] result)
        {
            ToMultilabel().Scores(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < result.Length; i++)
            {
                decision[i] = Classes.Decide(result[i][CLASS_POSITIVE] - SCORE_DECISION_THRESHOLD);
            }

            return result;
        }

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
        /// <returns>System.Double[][].</returns>
        public double[][] Scores(TInput[] input, ref int[] decision, double[][] result)
        {
            ToMultilabel().Scores(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < result.Length; i++)
            {
                decision[i] = (result[i][CLASS_POSITIVE] - SCORE_DECISION_THRESHOLD).ToZeroOne();
            }

            return result;
        }

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
        /// <returns>System.Double[][].</returns>
        public double[][] Scores(TInput[] input, ref double[] decision, double[][] result)
        {
            ToMultilabel().Scores(input, result);
            decision = createOrReuse(input, decision);
            for (var i = 0; i < result.Length; i++)
            {
                decision[i] = (result[i][CLASS_POSITIVE] - SCORE_DECISION_THRESHOLD).ToZeroOne();
            }

            return result;
        }

        #endregion
    }
}