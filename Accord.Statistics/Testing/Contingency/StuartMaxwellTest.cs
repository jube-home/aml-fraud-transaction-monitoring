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
using Accord.Statistics.Analysis;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Stuart-Maxwell test of homogeneity for <c>K x K</c> contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Stuart-Maxwell test is a generalization of
    ///         <see cref="McNemarTest">
    ///             McNemar's test
    ///         </see>
    ///         for multiple categories.
    ///     </para>
    ///     <para>
    ///         This is a <see cref="ChiSquareTest">Chi-square kind of test</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Uebersax, John (2006). "McNemar Tests of Marginal Homogeneity".
    ///                     Available on: http://www.john-uebersax.com/stat/mcnemar.htm
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Sun, Xuezheng; Yang, Zhao (2008). "Generalized McNemar's Test for Homogeneity of the Marginal
    ///                     Distributions". Available on: http://www2.sas.com/proceedings/forum2008/382-2008.pdf
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="ChiSquareTest" />
    [Serializable]
    public class StuartMaxwellTest : ChiSquareTest
    {
        /// <summary>
        ///     Creates a new Stuart-Maxwell test.
        /// </summary>
        /// <param name="matrix">The contingency table to test.</param>
        public StuartMaxwellTest(GeneralConfusionMatrix matrix)
        {
            var classes = matrix.NumberOfClasses;

            var df = classes - 1;

            var rowMarginals = matrix.RowTotals;
            var colMarginals = matrix.ColumnTotals;

            Delta = new double[df];
            for (var i = 0; i < Delta.Length; i++)
            {
                Delta[i] = rowMarginals[i] - colMarginals[i];
            }

            Covariance = new double[df, df];

            for (var i = 0; i < df; i++)
            {
                for (var j = 0; j < df; j++)
                {
                    if (i == j)
                    {
                        // double u = (rowMarginals[i] - colMarginals[i]);
                        double pii = matrix.Matrix[i, i];

                        Covariance[i, i] = rowMarginals[i] + colMarginals[i] - 2.0 * pii;
                    }
                    else
                    {
                        double pij = matrix.Matrix[i, j];
                        double pji = matrix.Matrix[j, i];

                        Covariance[i, j] = -(pij + pji);
                    }
                }
            }

            Precision = Covariance.PseudoInverse();

            var chiSquare = Delta.DotAndDot(Precision, Delta);

            Compute(chiSquare, df);
        }

        /// <summary>
        ///     Gets the delta vector <c>d</c> used
        ///     in the test calculations.
        /// </summary>
        public double[] Delta { get; }

        /// <summary>
        ///     Gets the covariance matrix <c>S</c>
        ///     used in the test calculations.
        /// </summary>
        public double[,] Covariance { get; }

        /// <summary>
        ///     Gets the inverse covariance matrix
        ///     <c>S^-1</c> used in the calculations.
        /// </summary>
        public double[,] Precision { get; }
    }
}