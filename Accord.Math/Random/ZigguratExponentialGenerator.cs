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
    ///     Exponential random number generator using the Ziggurat method.
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
    public class ZigguratExponentialGenerator : IRandomNumberGenerator<double>
    {
        private readonly double[] fe;
        private readonly uint[] ke;
        private readonly ZigguratUniformOneGenerator u;
        private readonly double[] we;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZigguratExponentialGenerator" /> class.
        /// </summary>
        /// <param name="seed">
        ///     The random seed to use. Default is to use the next value from
        ///     the <see cref="Generator">the framework-wide random generator</see>.
        /// </param>
        public ZigguratExponentialGenerator(int seed)
        {
            u = new ZigguratUniformOneGenerator(seed);

            ke = new uint[256];
            fe = new double[256];
            we = new double[256];
            setup();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZigguratExponentialGenerator" /> class.
        /// </summary>
        public ZigguratExponentialGenerator()
            : this(Generator.Random.Next())
        {
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
        public double Generate()
        {
            var jz = u.Next();
            var iz = jz & 255;

            if (jz < ke[iz])
            {
                return jz * we[iz];
            }

            for (;;)
            {
                if (iz == 0)
                {
                    return 7.69711 - System.Math.Log(u.Generate());
                }

                var x = jz * we[iz];

                if (fe[iz] + u.Generate() * (fe[iz - 1] - fe[iz]) < System.Math.Exp(-x))
                {
                    return x;
                }

                jz = u.Next();
                iz = jz & 255;

                if (jz < ke[iz])
                {
                    return jz * we[iz];
                }
            }

            //throw new InvalidOperationException("Execution should not reach here.");
        }

        private void setup()
        {
            var de = 7.697117470131487;
            const double m2 = 4294967296.0;
            var te = 7.697117470131487;
            const double ve = 3.949659822581572E-03;

            var q = ve / System.Math.Exp(-de);

            ke[0] = (uint)(de / q * m2);
            ke[1] = 0;

            we[0] = q / m2;
            we[255] = de / m2;

            fe[0] = 1.0f;
            fe[255] = System.Math.Exp(-de);

            for (var i = 254; 1 <= i; i--)
            {
                de = -System.Math.Log(ve / de + System.Math.Exp(-de));
                ke[i + 1] = (uint)(de / te * m2);
                te = de;
                fe[i] = System.Math.Exp(-de);
                we[i] = de / m2;
            }
        }
    }
}