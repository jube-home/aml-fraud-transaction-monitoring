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
    ///     Base class for multi-class and multi-label classifiers.
    /// </summary>
    /// <typeparam name="TInput">The data type for the input data. Default is double[].</typeparam>
    [Serializable]
    public abstract class TaggerBase<TInput> :
        TransformBase<TInput[], int[]>,
        ITagger<TInput>
    {
        /// <summary>
        ///     Gets the number of classes expected and recognized by the classifier.
        /// </summary>
        /// <value>The number of classes.</value>
        public virtual int NumberOfClasses
        {
            get => NumberOfOutputs;
            set => NumberOfOutputs = value;
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
        public virtual int[] Decide(TInput[] input, int[] result)
        {
            return Decide(new[] { input }, new[] { result })[0];
        }

        /// <summary>
        ///     Computes class-label decisions for the given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vectors that should be classified as
        ///     any of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A set of class-labels that best describe the <paramref name="input" />
        ///     vectors according to this classifier.
        /// </returns>
        public int[] Decide(TInput[] input)
        {
            return Decide(input, new int[input.Length]);
        }

        /// <summary>
        ///     Computes class-label decisions for the given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vectors that should be classified as
        ///     any of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A set of class-labels that best describe the <paramref name="input" />
        ///     vectors according to this classifier.
        /// </returns>
        public int[][] Decide(TInput[][] input)
        {
            return Decide(input, Jagged.CreateAs<TInput, int>(input));
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
        public abstract int[][] Decide(TInput[][] input, int[][] result);

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override int[] Transform(TInput[] input)
        {
            return Decide(input);
        }

        internal double[][] create(TInput[] input)
        {
            var result = new double[input.Length][];
            for (var j = 0; j < input.Length; j++)
            {
                result[j] = new double[NumberOfOutputs];
            }

            return result;
        }

        internal double[][][] create(TInput[][] input)
        {
            var result = new double[input.Length][][];
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = new double[input[i].Length][];
                for (var j = 0; j < input[i].Length; j++)
                {
                    result[i][j] = new double[NumberOfOutputs];
                }
            }

            return result;
        }

        internal T[][] create<T>(TInput[][] input, T[][] decision)
        {
            if (decision == null)
            {
                decision = Jagged.CreateAs<TInput, T>(input);
            }

            return decision;
        }
    }
}