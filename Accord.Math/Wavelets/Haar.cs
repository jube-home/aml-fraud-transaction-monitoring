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

namespace Accord.Math.Wavelets
{
    /// <summary>
    ///     Haar Wavelet Transform.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Musawir Ali, An Introduction to Wavelets and the Haar Transform.
    ///                     Available on: http://www.cs.ucf.edu/~mali/haar/
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public class Haar : IWavelet
    {
        private const double w0 = 0.5;
        private const double w1 = -0.5;
        private const double s0 = 0.5;
        private const double s1 = 0.5;

        private readonly int levels;

        /// <summary>
        ///     Constructs a new Haar Wavelet Transform.
        /// </summary>
        /// <param name="levels">The number of iterations for the 2D transform.</param>
        public Haar(int levels)
        {
            this.levels = levels;
        }

        /// <summary>
        ///     1-D Forward Discrete Wavelet Transform.
        /// </summary>
        public void Forward(double[] data)
        {
            FWT(data);
        }

        /// <summary>
        ///     1-D Backward (Inverse) Discrete Wavelet Transform.
        /// </summary>
        public void Backward(double[] data)
        {
            IWT(data);
        }

        /// <summary>
        ///     2-D Forward Discrete Wavelet Transform.
        /// </summary>
        public void Forward(double[,] data)
        {
            FWT(data, levels);
        }

        /// <summary>
        ///     2-D Backward (Inverse) Discrete Wavelet Transform.
        /// </summary>
        public void Backward(double[,] data)
        {
            IWT(data, levels);
        }

        /// <summary>
        ///     Discrete Haar Wavelet Transform
        /// </summary>
        public static void FWT(double[] data)
        {
            var temp = new double[data.Length];

            var h = data.Length >> 1;
            for (var i = 0; i < h; i++)
            {
                var k = i << 1;
                temp[i] = data[k] * s0 + data[k + 1] * s1;
                temp[i + h] = data[k] * w0 + data[k + 1] * w1;
            }

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = temp[i];
            }
        }

        /// <summary>
        ///     Inverse Haar Wavelet Transform
        /// </summary>
        public static void IWT(double[] data)
        {
            var temp = new double[data.Length];

            var h = data.Length >> 1;
            for (var i = 0; i < h; i++)
            {
                var k = i << 1;
                temp[k] = (data[i] * s0 + data[i + h] * w0) / w0;
                temp[k + 1] = (data[i] * s1 + data[i + h] * w1) / s0;
            }

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = temp[i];
            }
        }

        /// <summary>
        ///     Discrete Haar Wavelet 2D Transform
        /// </summary>
        public static void FWT(double[,] data, int iterations)
        {
            var rows = data.GetLength(0);
            var cols = data.GetLength(1);

            for (var k = 0; k < iterations; k++)
            {
                var lev = 1 << k;

                var levCols = cols / lev;
                var levRows = rows / lev;

                var row = new double[levCols];
                for (var i = 0; i < levRows; i++)
                {
                    for (var j = 0; j < row.Length; j++)
                    {
                        row[j] = data[i, j];
                    }

                    FWT(row);

                    for (var j = 0; j < row.Length; j++)
                    {
                        data[i, j] = row[j];
                    }
                }


                var col = new double[levRows];
                for (var j = 0; j < levCols; j++)
                {
                    for (var i = 0; i < col.Length; i++)
                    {
                        col[i] = data[i, j];
                    }

                    FWT(col);

                    for (var i = 0; i < col.Length; i++)
                    {
                        data[i, j] = col[i];
                    }
                }
            }
        }

        /// <summary>
        ///     Inverse Haar Wavelet 2D Transform
        /// </summary>
        public static void IWT(double[,] data, int iterations)
        {
            var rows = data.GetLength(0);
            var cols = data.GetLength(1);

            for (var k = iterations - 1; k >= 0; k--)
            {
                var lev = 1 << k;

                var levCols = cols / lev;
                var levRows = rows / lev;

                var col = new double[levRows];
                for (var j = 0; j < levCols; j++)
                {
                    for (var i = 0; i < col.Length; i++)
                    {
                        col[i] = data[i, j];
                    }

                    IWT(col);

                    for (var i = 0; i < col.Length; i++)
                    {
                        data[i, j] = col[i];
                    }
                }

                var row = new double[levCols];
                for (var i = 0; i < levRows; i++)
                {
                    for (var j = 0; j < row.Length; j++)
                    {
                        row[j] = data[i, j];
                    }

                    IWT(row);

                    for (var j = 0; j < row.Length; j++)
                    {
                        data[i, j] = row[j];
                    }
                }
            }
        }
    }
}