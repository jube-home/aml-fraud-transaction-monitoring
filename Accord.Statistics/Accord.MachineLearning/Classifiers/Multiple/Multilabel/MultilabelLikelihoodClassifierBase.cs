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

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Base class for
    ///     <see cref="IMultilabelLikelihoodClassifier{TInput}">
    ///         generative multi-label classifiers
    ///     </see>
    ///     .
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    [Serializable]
    public abstract class MultilabelLikelihoodClassifierBase<TInput> :
        MultilabelScoreClassifierBase<TInput>,
        IMultilabelLikelihoodClassifier<TInput>,
        IMulticlassLikelihoodClassifier<TInput>
    {
        int IMulticlassLikelihoodClassifier<TInput>.Decide(TInput input)
        {
            return ((IClassifier<TInput, int>)this).Decide(input);
        }

        int[] IMulticlassLikelihoodClassifier<TInput>.Decide(TInput[] input)
        {
            return ((IClassifier<TInput, int>)this).Decide(input);
        }

        double IMulticlassLikelihoodClassifier<TInput>.LogLikelihood(TInput input)
        {
            int decision;
            return LogLikelihoods(input, out decision)[decision];
        }

        double[] IMulticlassLikelihoodClassifier<TInput>.LogLikelihood(TInput[] input)
        {
            return ((IMulticlassLikelihoodClassifier<TInput>)this).LogLikelihood(input, new double[input.Length]);
        }

        double[] IMulticlassLikelihoodClassifier<TInput>.LogLikelihood(TInput[] input, double[] result)
        {
            int d;
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = LogLikelihoods(input[i], out d)[d];
            }

            return result;
        }

        IMultilabelLikelihoodClassifier<TInput> IMulticlassLikelihoodClassifier<TInput>.ToMultilabel()
        {
            return this;
        }

        double IMulticlassOutLikelihoodClassifier<TInput, int>.LogLikelihood(TInput input, out int decision)
        {
            return LogLikelihoods(input, out decision)[decision];
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, int>.LogLikelihood(TInput[] input, ref int[] decision)
        {
            return ((IMulticlassLikelihoodClassifier<TInput>)this).LogLikelihood(input, ref decision,
                new double[input.Length]);
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, int>.LogLikelihood(TInput[] input, ref int[] decision,
            double[] result)
        {
            decision = createOrReuse(input, decision);

            int d;
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = LogLikelihoods(input[i], out d)[d];
                decision[i] = d;
            }

            return result;
        }

        double IMulticlassOutLikelihoodClassifier<TInput, double>.LogLikelihood(TInput input, out double decision)
        {
            int d;
            var result = LogLikelihoods(input, out d)[d];
            decision = d;
            return result;
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, double>.LogLikelihood(TInput[] input,
            ref double[] decision)
        {
            return ((IMulticlassLikelihoodClassifier<TInput>)this).LogLikelihood(input, ref decision);
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, double>.LogLikelihood(TInput[] input,
            ref double[] decision, double[] result)
        {
            decision = createOrReuse(input, decision);

            int d;
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = LogLikelihoods(input[i], out d)[d];
                decision[i] = d;
            }

            return result;
        }

        double IMulticlassLikelihoodClassifier<TInput>.Probability(TInput input)
        {
            int decision;
            return Probabilities(input, out decision)[decision];
        }

        double[] IMulticlassLikelihoodClassifier<TInput>.Probability(TInput[] input)
        {
            return ((IMulticlassLikelihoodClassifier<TInput>)this).Probability(input, new double[input.Length]);
        }

        double[] IMulticlassLikelihoodClassifier<TInput>.Probability(TInput[] input, double[] result)
        {
            int d;
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Probabilities(input[i], out d)[d];
            }

            return result;
        }

        double IMulticlassOutLikelihoodClassifier<TInput, int>.Probability(TInput input, out int decision)
        {
            return Probabilities(input, out decision)[decision];
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, int>.Probability(TInput[] input, ref int[] decision)
        {
            return ((IMulticlassLikelihoodClassifier<TInput>)this).Probability(input, ref decision,
                new double[input.Length]);
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, int>.Probability(TInput[] input, ref int[] decision,
            double[] result)
        {
            decision = createOrReuse(input, decision);

            int d;
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Probabilities(input[i], out d)[d];
                decision[i] = d;
            }

            return result;
        }

        double IMulticlassOutLikelihoodClassifier<TInput, double>.Probability(TInput input, out double decision)
        {
            int d;
            var result = Probabilities(input, out d)[d];
            decision = d;
            return result;
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, double>.Probability(TInput[] input, ref double[] decision)
        {
            return ((IMulticlassLikelihoodClassifier<TInput>)this).Probability(input, ref decision);
        }

        double[] IMulticlassLikelihoodClassifierBase<TInput, double>.Probability(TInput[] input, ref double[] decision,
            double[] result)
        {
            decision = createOrReuse(input, decision);

            int d;
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Probabilities(input[i], out d)[d];
                decision[i] = d;
            }

            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the log-likelihoods will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public virtual double[] Probabilities(TInput input, ref bool[] decision, double[] result)
        {
            LogLikelihoods(input, ref decision, result);
            return result.Exp(result);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the log-likelihoods will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public virtual double[] LogLikelihoods(TInput input, ref bool[] decision, double[] result)
        {
            return Scores(input, ref decision, result);
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public virtual double LogLikelihood(TInput input, int classIndex)
        {
            var decision = new bool[NumberOfOutputs];
            var result = new double[NumberOfOutputs];
            LogLikelihoods(input, ref decision, result);
            return result[classIndex];
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double[] LogLikelihoods(TInput input)
        {
            return LogLikelihoods(input, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the log-likelihoods will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] LogLikelihoods(TInput input, double[] result)
        {
            var decision = new bool[NumberOfOutputs];
            return LogLikelihoods(input, ref decision, result);
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double[][] LogLikelihoods(TInput[] input)
        {
            return LogLikelihoods(input, create<double>(input));
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the log-likelihoods will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] LogLikelihoods(TInput[] input, double[][] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = LogLikelihoods(input[i], result[i]);
            }

            return result;
        }

        // Input, classIndex
        /// <summary>
        ///     Computes the probability that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public double Probability(TInput input, int classIndex)
        {
            return Probabilities(input)[classIndex];
        }

        /// <summary>
        ///     Computes the probability that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public double[] Probability(TInput[] input, int classIndex)
        {
            return Probability(input, classIndex, new double[input.Length]);
        }

        /// <summary>
        ///     Computes the probability that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Probability(TInput[] input, int classIndex, double[] result)
        {
            var temp = new double[NumberOfOutputs];
            for (var i = 0; i < input.Length; i++)
            {
                Probabilities(input[i], temp);
                result[i] = temp[classIndex];
            }

            return result;
        }

        // Input, classIndex[]
        /// <summary>
        ///     Computes the probability that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public double[] Probability(TInput[] input, int[] classIndex)
        {
            return Probability(input, classIndex, new double[input.Length]);
        }

        /// <summary>
        ///     Computes the probability that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Probability(TInput[] input, int[] classIndex, double[] result)
        {
            var temp = new double[NumberOfOutputs];
            for (var i = 0; i < input.Length; i++)
            {
                Probabilities(input[i], temp);
                result[i] = temp[classIndex[i]];
            }

            return result;
        }

        // Probabilities        

        /// <summary>
        ///     Computes the probabilities that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double[] Probabilities(TInput input)
        {
            return Probabilities(input, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Computes the probabilities that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Probabilities(TInput input, double[] result)
        {
            return Probabilities(input, out int _, result);
        }

        /// <summary>
        ///     Computes the probabilities that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        public double[][] Probabilities(TInput[] input)
        {
            return Probabilities(input, create<double>(input));
        }

        /// <summary>
        ///     Computes the probabilities that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Probabilities(TInput[] input, double[][] result)
        {
            var decision = new bool[NumberOfOutputs];
            for (var i = 0; i < input.Length; i++)
            {
                Probabilities(input[i], ref decision, result[i]);
            }

            return result;
        }

        // LogLikelihood, input, classIndex

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public double[] LogLikelihood(TInput[] input, int[] classIndex)
        {
            return LogLikelihood(input, classIndex, new double[input.Length]);
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="result">
        ///     An array where the log-likelihoods will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] LogLikelihood(TInput[] input, int[] classIndex, double[] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = LogLikelihood(input[i], classIndex[i]);
            }

            return result;
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public double[] LogLikelihood(TInput[] input, int classIndex)
        {
            return LogLikelihood(input, classIndex, new double[input.Length]);
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="result">
        ///     An array where the log-likelihoods will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] LogLikelihood(TInput[] input, int classIndex, double[] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = LogLikelihood(input[i], classIndex);
            }

            return result;
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double[][] Transform(TInput[] input, double[][] result)
        {
            return Probabilities(input, result);
        }

        /// <summary>
        ///     Views this instance as a multi-class generative classifier.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMulticlassLikelihoodClassifier{TInput}" />.
        /// </returns>
        public new IMulticlassLikelihoodClassifier<TInput> ToMulticlass()
        {
            return this;
        }

        /// <summary>
        ///     Views this instance as a multi-class generative classifier.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMulticlassLikelihoodClassifier{TInput}" />.
        /// </returns>
        public new IMulticlassLikelihoodClassifier<TInput, T> ToMulticlass<T>()
        {
            return (IMulticlassLikelihoodClassifier<TInput, T>)this;
        }
        // Main overridable methods

        /// <summary>
        ///     Computes a log-likelihood measuring the association between
        ///     the given <paramref name="input" /> vector and a given
        ///     <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="decision">
        ///     The class label associated with the input
        ///     vector, as predicted by the classifier.
        /// </param>
        public abstract double LogLikelihood(TInput input, int classIndex, out bool decision);

        // Transform

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double[] Transform(TInput input, double[] result)
        {
            return Probabilities(input, result);
        }

        // Same as Distance methods in IMultilabelDistanceClassifier, 
        // but replaced with LogLikelihood

        #region LogLikelihoods

        // Input, decision

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[] LogLikelihoods(TInput input, out double decision)
        {
            return LogLikelihoods(input, out decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[] LogLikelihoods(TInput input, out int decision)
        {
            return LogLikelihoods(input, out decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[] LogLikelihoods(TInput input, ref bool[] decision)
        {
            return LogLikelihoods(input, ref decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        public double[] LogLikelihoods(TInput input, ref int[] decision)
        {
            return LogLikelihoods(input, ref decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        public double[] LogLikelihoods(TInput input, ref double[] decision)
        {
            return LogLikelihoods(input, ref decision, new double[NumberOfOutputs]);
        }

        // Input, decision, result

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] LogLikelihoods(TInput input, out double decision, double[] result)
        {
            LogLikelihoods(input, result);
            decision = result.ArgMax();
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] LogLikelihoods(TInput input, out int decision, double[] result)
        {
            LogLikelihoods(input, result);
            decision = result.ArgMax();
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] LogLikelihoods(TInput input, ref int[] decision, double[] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            LogLikelihoods(input, ref mask, result);
            Vector.KHot(mask, decision);
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] LogLikelihoods(TInput input, ref double[] decision, double[] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            LogLikelihoods(input, ref mask, result);
            Vector.KHot(mask, decision);
            return result;
        }

        // Input[], decision[]

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] LogLikelihoods(TInput[] input, ref double[] decision)
        {
            return LogLikelihoods(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] LogLikelihoods(TInput[] input, ref int[] decision)
        {
            return LogLikelihoods(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] LogLikelihoods(TInput[] input, ref int[][] decision)
        {
            return LogLikelihoods(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] LogLikelihoods(TInput[] input, ref bool[][] decision)
        {
            return LogLikelihoods(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] LogLikelihoods(TInput[] input, ref double[][] decision)
        {
            return LogLikelihoods(input, ref decision, create<double>(input));
        }

        // Input[], decision[], result[]        

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] LogLikelihoods(TInput[] input, ref double[] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            for (var i = 0; i < input.Length; i++)
            {
                LogLikelihoods(input[i], out decision[i], result[i]);
            }

            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] LogLikelihoods(TInput[] input, ref int[] decision, double[][] result)
        {
            LogLikelihoods(input, result);
            result.ArgMax(1, decision);
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] LogLikelihoods(TInput[] input, ref bool[][] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            for (var i = 0; i < input.Length; i++)
            {
                LogLikelihoods(input[i], ref decision[i], result[i]);
            }

            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] LogLikelihoods(TInput[] input, ref int[][] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            for (var i = 0; i < input.Length; i++)
            {
                LogLikelihoods(input[i], ref mask, result[i]);
                decision[i] = Vector.KHot(mask, decision[i]);
            }

            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] LogLikelihoods(TInput[] input, ref double[][] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            for (var i = 0; i < input.Length; i++)
            {
                LogLikelihoods(input[i], ref mask, result[i]);
                decision[i] = Vector.KHot(mask, decision[i]);
            }

            return result;
        }

        #endregion

        // Same as Distance methods in IMultilabelDistanceClassifier, 
        // but replaced with Probabilities

        #region Probabilities

        // Input, decision

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[] Probabilities(TInput input, out double decision)
        {
            return Probabilities(input, out decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        public double[] Probabilities(TInput input, out int decision)
        {
            return Probabilities(input, out decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        public double[] Probabilities(TInput input, ref bool[] decision)
        {
            return Probabilities(input, ref decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        public double[] Probabilities(TInput input, ref int[] decision)
        {
            return Probabilities(input, ref decision, new double[NumberOfOutputs]);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        public double[] Probabilities(TInput input, ref double[] decision)
        {
            return Probabilities(input, ref decision, new double[NumberOfOutputs]);
        }

        // Input, decision, result

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Probabilities(TInput input, out double decision, double[] result)
        {
            var mask = new bool[NumberOfOutputs];
            Probabilities(input, ref mask, result);
            decision = mask.ArgMax();
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">
        ///     The class labels associated with each input
        ///     vector, as predicted by the classifier. If passed as null, the classifier
        ///     will create a new array.
        /// </param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Probabilities(TInput input, out int decision, double[] result)
        {
            var mask = new bool[NumberOfOutputs];
            Probabilities(input, ref mask, result);
            decision = mask.ArgMax();
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Probabilities(TInput input, ref int[] decision, double[] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            Probabilities(input, ref mask, result);
            Vector.KHot(mask, decision);
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[] Probabilities(TInput input, ref double[] decision, double[] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            Probabilities(input, ref mask, result);
            Vector.KHot(mask, decision);
            return result;
        }

        // Input[], decision[]

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] Probabilities(TInput[] input, ref double[] decision)
        {
            return Probabilities(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] Probabilities(TInput[] input, ref int[] decision)
        {
            return Probabilities(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] Probabilities(TInput[] input, ref int[][] decision)
        {
            return Probabilities(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] Probabilities(TInput[] input, ref bool[][] decision)
        {
            return Probabilities(input, ref decision, create<double>(input));
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        public double[][] Probabilities(TInput[] input, ref double[][] decision)
        {
            return Probabilities(input, ref decision, create<double>(input));
        }

        // Input, decision, result        

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Probabilities(TInput[] input, ref double[] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            for (var i = 0; i < input.Length; i++)
            {
                Probabilities(input[i], out decision[i], result[i]);
            }

            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Probabilities(TInput[] input, ref int[] decision, double[][] result)
        {
            Probabilities(input, result);
            result.ArgMax(1, decision);
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Probabilities(TInput[] input, ref bool[][] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            for (var i = 0; i < input.Length; i++)
            {
                Probabilities(input[i], ref decision[i], result[i]);
            }

            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Probabilities(TInput[] input, ref int[][] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            for (var i = 0; i < input.Length; i++)
            {
                Probabilities(input[i], ref mask, result[i]);
                decision[i] = Vector.KHot(mask, decision[i]);
            }

            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for each input vector, returning the
        ///     probabilities of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The labels predicted by the classifier.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        public double[][] Probabilities(TInput[] input, ref double[][] decision, double[][] result)
        {
            decision = createOrReuse(input, decision);
            var mask = new bool[NumberOfOutputs];
            for (var i = 0; i < input.Length; i++)
            {
                Probabilities(input[i], ref mask, result[i]);
                decision[i] = Vector.KHot(mask, decision[i]);
            }

            return result;
        }

        #endregion
    }
}