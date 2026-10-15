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
    ///     Base class for multi-class classifiers.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    [Serializable]
    public abstract class MulticlassClassifierBase<TInput> :
        ClassifierBase<TInput, int>,
        IMulticlassClassifier<TInput>
    {
        // Input

        double IClassifier<TInput, double>.Decide(TInput input)
        {
            return Decide(input);
        }

        bool[] IClassifier<TInput, bool[]>.Decide(TInput input)
        {
            return Decide(input, new bool[NumberOfOutputs]);
        }

        int[] IClassifier<TInput, int[]>.Decide(TInput input)
        {
            return Decide(input, new int[NumberOfOutputs]);
        }

        double[] IClassifier<TInput, double[]>.Decide(TInput input)
        {
            return Decide(input, new double[NumberOfOutputs]);
        }

        // Input[]

        double[] IClassifier<TInput, double>.Decide(TInput[] input)
        {
            return Decide(input, new double[input.Length]);
        }

        bool[][] IClassifier<TInput, bool[]>.Decide(TInput[] input)
        {
            return ToMultilabel().Decide(input, create<bool>(input));
        }

        double[][] IClassifier<TInput, double[]>.Decide(TInput[] input)
        {
            return ToMultilabel().Decide(input, create<double>(input));
        }

        int[][] IClassifier<TInput, int[]>.Decide(TInput[] input)
        {
            return ToMultilabel().Decide(input, create<int>(input));
        }

        // Input, result

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
        public bool[] Decide(TInput input, bool[] result)
        {
            return Vector.OneHot(Decide(input), result);
        }

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
        public int[] Decide(TInput input, int[] result)
        {
            return Vector.OneHot(Decide(input), result);
        }

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
        public double[] Decide(TInput input, double[] result)
        {
            return Vector.OneHot<double>(Decide(input), result);
        }

        // Input[], result[]

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="result">The location where to store the class-labels.</param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public double[] Decide(TInput[] input, double[] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Decide(input[i]);
            }

            return result;
        }

        bool[][] IClassifier<TInput, bool[]>.Decide(TInput[] input, bool[][] result)
        {
            return Jagged.OneHot(Decide(input), result);
        }

        int[][] IClassifier<TInput, int[]>.Decide(TInput[] input, int[][] result)
        {
            return Jagged.OneHot(Decide(input), result);
        }

        double[][] IClassifier<TInput, double[]>.Decide(TInput[] input, double[][] result)
        {
            return Jagged.OneHot<double>(Decide(input), result);
        }

        // Transform

        double ICovariantTransform<TInput, double>.Transform(TInput input)
        {
            return ((IClassifier<TInput, double>)this).Decide(input);
        }

        double[] ICovariantTransform<TInput, double>.Transform(TInput[] input)
        {
            return Transform(input, new double[input.Length]);
        }

        double[] ICovariantTransform<TInput, double[]>.Transform(TInput input)
        {
            return Transform(input, create<double>(input));
        }

        double[][] ICovariantTransform<TInput, double[]>.Transform(TInput[] input)
        {
            return Transform(input, create<double>(input));
        }

        bool[] ICovariantTransform<TInput, bool[]>.Transform(TInput input)
        {
            return Transform(input, create<bool>(input));
        }

        bool[][] ICovariantTransform<TInput, bool[]>.Transform(TInput[] input)
        {
            return Transform(input, create<bool>(input));
        }

        int[] ICovariantTransform<TInput, int[]>.Transform(TInput input)
        {
            return Transform(input, create<int>(input));
        }

        int[][] ICovariantTransform<TInput, int[]>.Transform(TInput[] input)
        {
            return Transform(input, create<int>(input));
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public virtual double[] Transform(TInput[] input, double[] result)
        {
            return Decide(input, result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public virtual bool[][] Transform(TInput[] input, bool[][] result)
        {
            return ToMultilabel().Decide(input, result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        /// this transformation to the given input.
        public virtual int[][] Transform(TInput[] input, int[][] result)
        {
            return ToMultilabel().Decide(input, result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public virtual double[][] Transform(TInput[] input, double[][] result)
        {
            return ToMultilabel().Decide(input, result);
        }

        /// <summary>
        ///     Views this instance as a multi-label classifier,
        ///     giving access to more advanced methods, such as the prediction
        ///     of one-hot vectors.
        /// </summary>
        /// <returns>
        ///     This instance seen as an <see cref="IMultilabelLikelihoodClassifier{TInput}" />.
        /// </returns>
        public IMultilabelClassifier<TInput> ToMultilabel()
        {
            return this;
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public virtual bool[] Transform(TInput input, bool[] result)
        {
            return Decide(input, result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public virtual int[] Transform(TInput input, int[] result)
        {
            return Decide(input, result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public virtual double[] Transform(TInput input, double[] result)
        {
            return Decide(input, result);
        }
    }
}