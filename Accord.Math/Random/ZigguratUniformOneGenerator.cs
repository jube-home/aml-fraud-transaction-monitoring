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

namespace Accord.Math.Random
{
    /// <summary>
    ///     Uniform random number generator using the Ziggurat method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://people.sc.fsu.edu/~jburkardt/c_src/ziggurat/ziggurat.html">
    ///                         John Burkard, Ziggurat Random Number Generator (RNG). Available on:
    ///                         http://people.sc.fsu.edu/~jburkardt/c_src/ziggurat/ziggurat.c (LGPL)
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Philip Leong, Guanglie Zhang, Dong-U Lee, Wayne Luk, John Villasenor,
    ///                     A comment on the implementation of the ziggurat method,
    ///                     Journal of Statistical Software, Volume 12, Number 7, February 2005.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     George Marsaglia, Wai Wan Tsang, The Ziggurat Method for Generating Random Variables,
    ///                     Journal of Statistical Software, Volume 5, Number 8, October 2000, seven pages.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public sealed class ZigguratUniformOneGenerator :
        IRandomNumberGenerator<double>,
        IRandomNumberGenerator<int>,
        IRandomNumberGenerator<uint>
    {
        private uint jsr;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZigguratExponentialGenerator" /> class.
        /// </summary>
        public ZigguratUniformOneGenerator()
            : this(Generator.Random.Next())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZigguratUniformOneGenerator" /> class.
        /// </summary>
        /// <param name="seed">
        ///     The random seed to use. Default is to use the next value from
        ///     the <see cref="Generator">the framework-wide random generator</see>.
        /// </param>
        public ZigguratUniformOneGenerator(int seed)
        {
            jsr = (uint)seed;
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public double[] Generate(int samples)
        {
            return Generate(samples, new double[samples]);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public double[] Generate(int samples, double[] result)
        {
            for (var i = 0; i < samples; i++)
            {
                result[i] = Generate();
            }

            return result;
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Generate()
        {
            unchecked
            {
                var value = jsr;
                jsr = jsr ^ (jsr << 13);
                jsr = jsr ^ (jsr >> 17);
                jsr = jsr ^ (jsr << 5);
                // https://core.ac.uk/download/files/153/6287927.pdf
                // return 0.5 + (value + jsr) * 0.2328306e-9;
                return (0.5 + (value + jsr) / 65536.0 / 65536.0) % 1.0;
            }
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public int[] Generate(int samples, int[] result)
        {
            for (var i = 0; i < samples; i++)
            {
                result[i] = (int)Next();
            }

            return result;
        }

        int[] IRandomNumberGenerator<int>.Generate(int samples)
        {
            return Generate(samples, new int[samples]);
        }

        int IRandomNumberGenerator<int>.Generate()
        {
            return (int)Next();
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public uint[] Generate(int samples, uint[] result)
        {
            for (var i = 0; i < samples; i++)
            {
                result[i] = Next();
            }

            return result;
        }

        uint[] IRandomNumberGenerator<uint>.Generate(int samples)
        {
            return Generate(samples, new uint[samples]);
        }

        uint IRandomNumberGenerator<uint>.Generate()
        {
            return Next();
        }

        /// <summary>
        ///     Generates a new non-negative integer random number.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public uint Next()
        {
            unchecked
            {
                var value = jsr;
                jsr = jsr ^ (jsr << 13);
                jsr = jsr ^ (jsr >> 17);
                jsr = jsr ^ (jsr << 5);
                return value + jsr;
            }
        }
    }
}