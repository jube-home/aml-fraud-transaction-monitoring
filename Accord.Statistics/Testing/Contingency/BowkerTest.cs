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
using Accord.Statistics.Analysis;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Bowker test of symmetry for contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a <see cref="ChiSquareTest">Chi-square kind of test</see>.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ChiSquareTest" />
    [Serializable]
    public class BowkerTest : ChiSquareTest
    {
        /// <summary>
        ///     Creates a new Bowker test.
        /// </summary>
        /// <param name="matrix">The contingency table to test.</param>
        public BowkerTest(GeneralConfusionMatrix matrix)
        {
            var classes = matrix.NumberOfClasses;
            var n = matrix.Matrix;

            double Qb = 0;

            for (var j = 0; j < classes; j++)
            {
                for (var i = 0; i < j; i++)
                {
                    double q = n[i, j] - n[j, i];
                    Qb += q * q / (n[i, j] + n[j, i]);
                }
            }

            var df = classes * (classes - 1) / 2;

            Compute(Qb, df);
        }
    }
}