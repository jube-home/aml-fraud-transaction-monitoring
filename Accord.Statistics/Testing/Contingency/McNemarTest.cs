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
    ///     McNemar test of homogeneity for <c>2 x 2</c> contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         McNemar's test is a non-parametric method used on nominal data. It is applied to
    ///         2 × 2 contingency tables with a dichotomous trait, with matched pairs of subjects,
    ///         to determine whether the row and column marginal frequencies are equal, i.e. if
    ///         the contingency table presents marginal homogeneity.
    ///     </para>
    ///     <para>
    ///         This is a <see cref="ChiSquareTest">Chi-square kind of test</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors, "McNemar's test," Wikipedia, The Free Encyclopedia,
    ///                     Available on: http://http://en.wikipedia.org/wiki/McNemar's_test.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="ChiSquareTest" />
    [Serializable]
    public class McNemarTest : ChiSquareTest
    {
        /// <summary>
        ///     Creates a new McNemar test.
        /// </summary>
        /// <param name="matrix">The contingency table to test.</param>
        /// <param name="yatesCorrection">
        ///     True to use Yate's correction of
        ///     continuity, falser otherwise. Default is false.
        /// </param>
        public McNemarTest(ConfusionMatrix matrix, bool yatesCorrection = false)
        {
            //int a = matrix.TruePositives;
            var b = matrix.FalseNegatives;
            var c = matrix.FalsePositives;
            //int d = matrix.TrueNegatives;

            double u = b - c;

            if (yatesCorrection)
            {
                u = System.Math.Abs(u) - 0.5;
            }

            var chiSquare = u * u / (b + c);

            var df = 1;

            Compute(chiSquare, df);
        }
    }
}