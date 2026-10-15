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
    [Serializable]
    public abstract class MulticlassClassifierBase :
        MulticlassClassifierBase<double[]>,
        IMulticlassClassifier<int[]>,
        IMulticlassClassifier<float[]>
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
        public virtual int Decide(float[] input)
        {
            return Decide(input.ToDouble());
        }

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
        public int[] Decide(float[][] input)
        {
            return Decide(input, new int[input.Length]);
        }

        double IClassifier<float[], double>.Decide(float[] input)
        {
            return Decide(input);
        }

        double[] IClassifier<float[], double>.Decide(float[][] input)
        {
            return Decide(input, new double[input.Length]);
        }

        double[][] IClassifier<float[], double[]>.Decide(float[][] input)
        {
            return Decide(input, Jagged.Create<double>(input.Length, NumberOfOutputs));
        }

        bool[][] IClassifier<float[], bool[]>.Decide(float[][] input)
        {
            return Decide(input, Jagged.Create<bool>(input.Length, NumberOfOutputs));
        }

        double[] IClassifier<float[], double[]>.Decide(float[] input)
        {
            return Decide(input, new double[NumberOfOutputs]);
        }

        bool[] IClassifier<float[], bool[]>.Decide(float[] input)
        {
            return Decide(input, new bool[NumberOfOutputs]);
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
        public double[] Decide(float[] input, double[] result)
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
        public bool[] Decide(float[] input, bool[] result)
        {
            return Vector.OneHot(Decide(input), result);
        }

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
        public int[] Decide(float[][] input, int[] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                result[i] = Decide(cast);
            }

            return result;
        }

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
        public int[][] Decide(float[][] input, int[][] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                Vector.OneHot(Decide(cast), result[i]);
            }

            return result;
        }

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
        public double[] Decide(float[][] input, double[] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                result[i] = Decide(cast);
            }

            return result;
        }

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
        public double[][] Decide(float[][] input, double[][] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                Vector.OneHot(Decide(cast), result[i]);
            }

            return result;
        }

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
        public bool[][] Decide(float[][] input, bool[][] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                Vector.OneHot(Decide(cast), result[i]);
            }

            return result;
        }

        int[] IClassifier<float[], int[]>.Decide(float[] input)
        {
            var t = (IMultilabelClassifier<float[]>)this;
            return t.Decide(input, new int[NumberOfOutputs]);
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
        public int[] Decide(float[] input, int[] result)
        {
            return Decide(input.ToDouble(), result);
        }

        int[][] IClassifier<float[], int[]>.Decide(float[][] input)
        {
            return Decide(input, Jagged.Create<int>(input.Length, NumberOfOutputs));
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public int Transform(float[] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public int[] Transform(float[][] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public int[] Transform(float[][] input, int[] result)
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
        public double[][] Transform(float[][] input, double[][] result)
        {
            return Jagged.OneHot(Decide(input), result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public bool[][] Transform(float[][] input, bool[][] result)
        {
            return Jagged.OneHot(Decide(input), result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public int[][] Transform(float[][] input, int[][] result)
        {
            return Jagged.OneHot(Decide(input), result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public double[] Transform(float[][] input, double[] result)
        {
            return Decide(input, result);
        }

        double[] ICovariantTransform<float[], double[]>.Transform(float[] input)
        {
            return Vector.OneHot(Decide(input), NumberOfOutputs);
        }

        double[][] ICovariantTransform<float[], double[]>.Transform(float[][] input)
        {
            return Jagged.OneHot(Decide(input), NumberOfOutputs);
        }

        bool[] ICovariantTransform<float[], bool[]>.Transform(float[] input)
        {
            return Vector.OneHot<bool>(Decide(input), NumberOfOutputs);
        }

        bool[][] ICovariantTransform<float[], bool[]>.Transform(float[][] input)
        {
            return Jagged.OneHot<bool>(Decide(input), NumberOfOutputs);
        }

        int[] ICovariantTransform<float[], int[]>.Transform(float[] input)
        {
            return Vector.OneHot<int>(Decide(input), NumberOfOutputs);
        }

        int[][] ICovariantTransform<float[], int[]>.Transform(float[][] input)
        {
            return Jagged.OneHot<int>(Decide(input), NumberOfOutputs);
        }

        double ICovariantTransform<float[], double>.Transform(float[] input)
        {
            return Decide(input);
        }

        double[] ICovariantTransform<float[], double>.Transform(float[][] input)
        {
            var result = new double[input.Length];
            return Decide(input, result);
        }

        IMultilabelClassifier<float[]> IMulticlassClassifier<float[]>.ToMultilabel()
        {
            return this;
        }

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
        public virtual int Decide(int[] input)
        {
            return Decide(input.ToDouble());
        }

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
        public int[] Decide(int[][] input)
        {
            return Decide(input, new int[input.Length]);
        }

        double IClassifier<int[], double>.Decide(int[] input)
        {
            return Decide(input);
        }

        double[] IClassifier<int[], double>.Decide(int[][] input)
        {
            return Decide(input, new double[input.Length]);
        }

        double[][] IClassifier<int[], double[]>.Decide(int[][] input)
        {
            return Decide(input, Jagged.Create<double>(input.Length, NumberOfOutputs));
        }

        bool[][] IClassifier<int[], bool[]>.Decide(int[][] input)
        {
            return Decide(input, Jagged.Create<bool>(input.Length, NumberOfOutputs));
        }

        double[] IClassifier<int[], double[]>.Decide(int[] input)
        {
            return Decide(input, new double[NumberOfOutputs]);
        }

        bool[] IClassifier<int[], bool[]>.Decide(int[] input)
        {
            return Decide(input, new bool[NumberOfOutputs]);
        }

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
        public int[] Decide(int[][] input, int[] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                result[i] = Decide(cast);
            }

            return result;
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
        public double[] Decide(int[] input, double[] result)
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
        public bool[] Decide(int[] input, bool[] result)
        {
            return Vector.OneHot(Decide(input), result);
        }

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
        public double[] Decide(int[][] input, double[] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                result[i] = Decide(cast);
            }

            return result;
        }

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
        public int[][] Decide(int[][] input, int[][] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                Vector.OneHot(Decide(cast), result[i]);
            }

            return result;
        }

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
        public double[][] Decide(int[][] input, double[][] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                Vector.OneHot(Decide(cast), result[i]);
            }

            return result;
        }

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
        public bool[][] Decide(int[][] input, bool[][] result)
        {
            var cast = new double[NumberOfInputs];
            for (var i = 0; i < input.Length; i++)
            {
                input[i].ToDouble(cast);
                Vector.OneHot(Decide(cast), result[i]);
            }

            return result;
        }

        int[] IClassifier<int[], int[]>.Decide(int[] input)
        {
            var t = (IMultilabelClassifier<double[]>)this;
            return t.Decide(input.ToDouble(), new int[NumberOfOutputs]);
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
        public int[] Decide(int[] input, int[] result)
        {
            return Decide(input.ToDouble(), result);
        }

        int[][] IClassifier<int[], int[]>.Decide(int[][] input)
        {
            return Decide(input, Jagged.Create<int>(input.Length, NumberOfOutputs));
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public double[][] Transform(int[][] input, double[][] result)
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
        public int[][] Transform(int[][] input, int[][] result)
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
        public int[] Transform(int[][] input, int[] result)
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
        public bool[][] Transform(int[][] input, bool[][] result)
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
        public double[] Transform(int[][] input, double[] result)
        {
            return Decide(input, result);
        }

        double[] ICovariantTransform<int[], double[]>.Transform(int[] input)
        {
            return Vector.OneHot(Decide(input), NumberOfOutputs);
        }

        double[][] ICovariantTransform<int[], double[]>.Transform(int[][] input)
        {
            return Jagged.OneHot(Decide(input), NumberOfOutputs);
        }

        int[] ICovariantTransform<int[], int[]>.Transform(int[] input)
        {
            return Vector.OneHot<int>(Decide(input), NumberOfOutputs);
        }

        int[][] ICovariantTransform<int[], int[]>.Transform(int[][] input)
        {
            return Jagged.OneHot<int>(Decide(input), NumberOfOutputs);
        }

        bool[] ICovariantTransform<int[], bool[]>.Transform(int[] input)
        {
            return Vector.OneHot<bool>(Decide(input), NumberOfOutputs);
        }

        bool[][] ICovariantTransform<int[], bool[]>.Transform(int[][] input)
        {
            return Jagged.OneHot<bool>(Decide(input), NumberOfOutputs);
        }

        int ICovariantTransform<int[], int>.Transform(int[] input)
        {
            return Decide(input);
        }

        int[] ICovariantTransform<int[], int>.Transform(int[][] input)
        {
            return Decide(input);
        }

        double ICovariantTransform<int[], double>.Transform(int[] input)
        {
            return Decide(input);
        }

        double[] ICovariantTransform<int[], double>.Transform(int[][] input)
        {
            var result = new double[input.Length];
            return Decide(input, result);
        }

        IMultilabelClassifier<int[]> IMulticlassClassifier<int[]>.ToMultilabel()
        {
            return this;
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public int Transform(int[] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public double[] Transform(int[] input, double[] result)
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
        public int Transform(float[] input, int result)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public double[] Transform(float[] input, double[] result)
        {
            return Vector.OneHot(Decide(input), result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public bool[] Transform(float[] input, bool[] result)
        {
            return Vector.OneHot(Decide(input), result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public int[] Transform(float[] input, int[] result)
        {
            return Vector.OneHot(Decide(input), result);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public int[] Transform(int[] input, int[] result)
        {
            return Decide(input, result);
        }
    }
}