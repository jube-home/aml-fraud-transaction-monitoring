// AForge Math Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//
// FFT idea from Exocortex.DSP library
// http://www.exocortex.org/dsp/
//

using System;
using System.Numerics;
using Accord.Math.Transforms;

namespace Accord.Math
{
    /// <summary>
    ///     Original Fourier transform from AForge.NET. If possible,
    ///     please use <see cref="FourierTransform2" /> instead.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The class implements one dimensional and two dimensional Discrete  and Fast Fourier
    ///         Transformation. However, this class works only with square matrices with sizes that
    ///         are power of 2, and implements a different form of the transform that differs from
    ///         the implementations in other packages such as Octave and Matlab. For a more general
    ///         transform that should produce the same results as Octave, see <see cref="FourierTransform2" />.
    ///     </para>
    ///     <para>
    ///         This class may be deprecated (marked as obsolete) in the future.
    ///     </para>
    /// </remarks>
    /// <seealso cref="FourierTransform2" />
    public static class FourierTransform
    {
        /// <summary>
        ///     Fourier transformation direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            ///     Forward direction of Fourier transformation.
            /// </summary>
            Forward = 1,

            /// <summary>
            ///     Backward direction of Fourier transformation.
            /// </summary>
            Backward = -1
        }

        /// <summary>
        ///     One dimensional Discrete Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        public static void DFT(Complex[] data, Direction direction)
        {
            var n = data.Length;
            var dst = new Complex[n];

            // for each destination element
            for (var i = 0; i < dst.Length; i++)
            {
                dst[i] = Complex.Zero;

                var arg = -(int)direction * 2.0 * System.Math.PI * i / n;

                // sum source elements
                for (var j = 0; j < data.Length; j++)
                {
                    var cos = System.Math.Cos(j * arg);
                    var sin = System.Math.Sin(j * arg);

                    var re = data[j].Real * cos - data[j].Imaginary * sin;
                    var im = data[j].Real * sin + data[j].Imaginary * cos;

                    dst[i] += new Complex(re, im);
                }
            }

            // copy elements
            if (direction == Direction.Forward)
            {
                // devide also for forward transform
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] /= n;
                }
            }
            else
            {
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = dst[i];
                }
            }
        }

        /// <summary>
        ///     Two dimensional Discrete Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        public static void DFT2(Complex[,] data, Direction direction)
        {
            var n = data.GetLength(0); // rows
            var m = data.GetLength(1); // columns
            double arg, cos, sin;
            var dst = new Complex[System.Math.Max(n, m)];

            // process rows
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < dst.Length; j++)
                {
                    dst[j] = Complex.Zero;

                    arg = -(int)direction * 2.0 * System.Math.PI * j / m;

                    // sum source elements
                    for (var k = 0; k < m; k++)
                    {
                        cos = System.Math.Cos(k * arg);
                        sin = System.Math.Sin(k * arg);

                        var re = data[i, k].Real * cos - data[i, k].Imaginary * sin;
                        var im = data[i, k].Real * sin + data[i, k].Imaginary * cos;

                        dst[j] += new Complex(re, im);
                    }
                }

                // copy elements
                if (direction == Direction.Forward)
                {
                    // devide also for forward transform
                    for (var j = 0; j < dst.Length; j++)
                    {
                        data[i, j] = dst[j] / m;
                    }
                }
                else
                {
                    for (var j = 0; j < dst.Length; j++)
                    {
                        data[i, j] = dst[j];
                    }
                }
            }

            // process columns
            for (var j = 0; j < m; j++)
            {
                for (var i = 0; i < n; i++)
                {
                    dst[i] = Complex.Zero;

                    arg = -(int)direction * 2.0 * System.Math.PI * i / n;

                    // sum source elements
                    for (var k = 0; k < n; k++)
                    {
                        cos = System.Math.Cos(k * arg);
                        sin = System.Math.Sin(k * arg);

                        var re = data[k, j].Real * cos - data[k, j].Imaginary * sin;
                        var im = data[k, j].Real * sin + data[k, j].Imaginary * cos;

                        dst[i] += new Complex(re, im);
                    }
                }

                // copy elements
                if (direction == Direction.Forward)
                {
                    // devide also for forward transform
                    for (var i = 0; i < dst.Length; i++)
                    {
                        data[i, j] = dst[i] / n;
                    }
                }
                else
                {
                    for (var i = 0; i < dst.Length; i++)
                    {
                        data[i, j] = dst[i];
                    }
                }
            }
        }

        /// <summary>
        ///     One dimensional Fast Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// <remarks>
        ///     <para>
        ///         <note>
        ///             The method accepts <paramref name="data" /> array of 2<sup>n</sup> size
        ///             only, where <b>n</b> may vary in the [1, 14] range.
        ///         </note>
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Incorrect data length.</exception>
        public static void FFT(Complex[] data, Direction direction)
        {
            var n = data.Length;
            var m = Tools.Log2(n);

            // reorder data first
            ReorderData(data);

            // compute FFT
            var tn = 1;

            for (var k = 1; k <= m; k++)
            {
                var rotation = GetComplexRotation(k, direction);

                var tm = tn;
                tn <<= 1;

                for (var i = 0; i < tm; i++)
                {
                    var t = rotation[i];

                    for (var even = i; even < n; even += tn)
                    {
                        var odd = even + tm;
                        var ce = data[even];
                        var co = data[odd];

                        var tr = co.Real * t.Real - co.Imaginary * t.Imaginary;
                        var ti = co.Real * t.Imaginary + co.Imaginary * t.Real;

                        data[even] += new Complex(tr, ti);
                        data[odd] = new Complex(ce.Real - tr, ce.Imaginary - ti);
                    }
                }
            }

            if (direction == Direction.Forward)
            {
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] /= n;
                }
            }
        }

        /// <summary>
        ///     Two dimensional Fast Fourier Transform.
        /// </summary>
        /// <param name="data">Data to transform.</param>
        /// <param name="direction">Transformation direction.</param>
        /// <remarks>
        ///     <para>
        ///         <note>
        ///             The method accepts <paramref name="data" /> array of 2<sup>n</sup> size
        ///             only in each dimension, where <b>n</b> may vary in the [1, 14] range. For example, 16x16 array
        ///             is valid, but 15x15 is not.
        ///         </note>
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Incorrect data length.</exception>
        public static void FFT2(Complex[,] data, Direction direction)
        {
            var k = data.GetLength(0);
            var n = data.GetLength(1);

            // check data size
            if (!Tools.IsPowerOf2(k) || !Tools.IsPowerOf2(n))
            {
                throw new ArgumentException("The matrix rows and columns must be a power of 2.");
            }

            if (k < minLength || k > maxLength || n < minLength || n > maxLength)
            {
                throw new ArgumentException("Incorrect data length.");
            }

            // process rows
            var row = new Complex[n];

            for (var i = 0; i < k; i++)
            {
                // copy row
                for (var j = 0; j < row.Length; j++)
                {
                    row[j] = data[i, j];
                }

                // transform it
                FFT(row, direction);

                // copy back
                for (var j = 0; j < row.Length; j++)
                {
                    data[i, j] = row[j];
                }
            }

            // process columns
            var col = new Complex[k];

            for (var j = 0; j < n; j++)
            {
                // copy column
                for (var i = 0; i < k; i++)
                {
                    col[i] = data[i, j];
                }

                // transform it
                FFT(col, direction);

                // copy back
                for (var i = 0; i < k; i++)
                {
                    data[i, j] = col[i];
                }
            }
        }

        #region Private Region

        private const int minLength = 2;
        private const int maxLength = 16384;
        private const int minBits = 1;
        private const int maxBits = 14;
        private static readonly int[][] reversedBits = new int[maxBits][];
        private static readonly Complex[,][] complexRotation = new Complex[maxBits, 2][];

        // Get array, indicating which data members should be swapped before FFT
        private static int[] GetReversedBits(int numberOfBits)
        {
            if (numberOfBits < minBits || numberOfBits > maxBits)
            {
                throw new ArgumentOutOfRangeException();
            }

            // check if the array is already calculated
            if (reversedBits[numberOfBits - 1] == null)
            {
                var n = Tools.Pow2(numberOfBits);
                var rBits = new int[n];

                // calculate the array
                for (var i = 0; i < n; i++)
                {
                    var oldBits = i;
                    var newBits = 0;

                    for (var j = 0; j < numberOfBits; j++)
                    {
                        newBits = (newBits << 1) | (oldBits & 1);
                        oldBits = oldBits >> 1;
                    }

                    rBits[i] = newBits;
                }

                reversedBits[numberOfBits - 1] = rBits;
            }

            return reversedBits[numberOfBits - 1];
        }

        // Get rotation of complex number
        private static Complex[] GetComplexRotation(int numberOfBits, Direction direction)
        {
            var directionIndex = direction == Direction.Forward ? 0 : 1;

            // check if the array is already calculated
            if (complexRotation[numberOfBits - 1, directionIndex] == null)
            {
                var n = 1 << (numberOfBits - 1);
                var uR = 1.0;
                var uI = 0.0;
                var angle = System.Math.PI / n * (int)direction;
                var wR = System.Math.Cos(angle);
                var wI = System.Math.Sin(angle);
                var rotation = new Complex[n];

                for (var i = 0; i < n; i++)
                {
                    rotation[i] = new Complex(uR, uI);
                    var t = uR * wI + uI * wR;
                    uR = uR * wR - uI * wI;
                    uI = t;
                }

                complexRotation[numberOfBits - 1, directionIndex] = rotation;
            }

            return complexRotation[numberOfBits - 1, directionIndex];
        }

        // Reorder data for FFT using
        private static void ReorderData(Complex[] data)
        {
            var len = data.Length;

            // check data length
            if (len < minLength || len > maxLength || !Tools.IsPowerOf2(len))
            {
                throw new ArgumentException("Incorrect data length.");
            }

            var rBits = GetReversedBits(Tools.Log2(len));

            for (var i = 0; i < len; i++)
            {
                var s = rBits[i];

                if (s > i)
                {
                    var t = data[i];
                    data[i] = data[s];
                    data[s] = t;
                }
            }
        }

        #endregion
    }
}