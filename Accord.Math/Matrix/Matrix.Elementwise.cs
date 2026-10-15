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

namespace Accord.Math
{
    public static partial class Matrix
    {
        /// <summary>
        ///     Elementwise absolute value.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static int[] Abs(int[] value)
        {
            return value.Abs();
        }

        /// <summary>
        ///     Elementwise absolute value.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] Abs(double[] value)
        {
            return value.Abs();
        }

        /// <summary>
        ///     Elementwise absolute value.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] Sign(double[] value)
        {
            return value.Sign();
        }

        /// <summary>
        ///     Elementwise absolute value.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] Abs(double[,] value)
        {
            return value.Abs();
        }

        /// <summary>
        ///     Elementwise absolute value.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static int[,] Abs(int[,] value)
        {
            return value.Abs();
        }

        /// <summary>
        ///     Elementwise Square root.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] Sqrt(double[] value)
        {
            return value.Sqrt();
        }

        /// <summary>
        ///     Elementwise Square root.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] Sqrt(double[,] value)
        {
            return value.Sqrt();
        }

        /// <summary>
        ///     Elementwise Log operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] Log(double[,] value)
        {
            return value.Log();
        }

        /// <summary>
        ///     Elementwise Exp operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] Exp(double[,] value)
        {
            return value.Exp();
        }

        /// <summary>
        ///     Elementwise Exp operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] Exp(double[] value)
        {
            return value.Exp();
        }

        /// <summary>
        ///     Elementwise Log operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] Log(double[] value)
        {
            return value.Log();
        }

        /// <summary>
        ///     Elementwise Log operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] Log(int[] value)
        {
            return value.Log();
        }

        /// <summary>
        ///     Elementwise power operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] ElementwisePower(double[,] x, double y)
        {
            return x.Pow(y);
        }

        /// <summary>
        ///     Elementwise power operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] ElementwisePower(double[] x, double y)
        {
            return x.Pow(y);
        }

        /// <summary>
        ///     Elementwise divide operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] ElementwiseDivide(double[] a, double[] b)
        {
            return a.Divide(b);
        }

        /// <summary>
        ///     Elementwise divide operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] ElementwiseDivide(this double[,] a, double[,] b)
        {
            return a.Divide(b);
        }

        /// <summary>
        ///     Elementwise divide operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static float[,] ElementwiseDivide(float[,] a, float[,] b)
        {
            return a.Divide(b);
        }

        /// <summary>
        ///     Elementwise division.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] ElementwiseDivide(double[,] a, double[] b, int dimension = 0, bool inPlace = false)
        {
            if (inPlace)
            {
                return a.Divide(b, dimension, a);
            }

            return a.Divide(b, dimension);
        }

        /// <summary>
        ///     Elementwise division.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] ElementwiseDivide(int[,] a, int[] b, int dimension)
        {
            return a.Divide(b, dimension);
        }

        /// <summary>
        ///     Elementwise multiply operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] ElementwiseMultiply(double[] a, double[] b)
        {
            return a.Multiply(b);
        }

        /// <summary>
        ///     Elementwise multiply operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[] ElementwiseMultiply(double[] a, int[] b)
        {
            return a.Multiply(b);
        }

        /// <summary>
        ///     Elementwise multiply operation.
        /// </summary>
        public static double[,] ElementwiseMultiply(double[,] a, double[,] b)
        {
            return a.Multiply(b);
        }

        /// <summary>
        ///     Elementwise multiply operation.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static int[] ElementwiseMultiply(int[] a, int[] b)
        {
            return a.Multiply(b);
        }

        /// <summary>
        ///     Elementwise multiplication.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static int[,] ElementwiseMultiply(int[,] a, int[,] b)
        {
            return a.Multiply(b);
        }

        /// <summary>
        ///     Elementwise multiplication.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] ElementwiseMultiply(double[,] a, double[] b, int dimension)
        {
            return a.Multiply(b, dimension);
        }

        /// <summary>
        ///     Elementwise multiplication.
        /// </summary>
        [Obsolete("Please use the functions in the Elementwise class instead.")]
        public static double[,] ElementwiseMultiply(double[,] a, double[] b, double[,] r, int dimension)
        {
            return a.Multiply(b, dimension, r);
        }
    }
}