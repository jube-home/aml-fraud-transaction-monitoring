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
using System.ComponentModel;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Source of variation in an <see cref="IAnova">ANOVA experiment</see>.
    /// </summary>
    [Serializable]
    public class AnovaVariationSource
    {
        /// <summary>
        ///     Creates a new object representation of a variation source in an ANOVA experiment.
        /// </summary>
        /// <param name="anova">The associated ANOVA analysis.</param>
        /// <param name="source">The name of the variation source.</param>
        public AnovaVariationSource(IAnova anova, string source)
        {
            this.Anova = anova;
            Source = source;
        }

        /// <summary>
        ///     Creates a new object representation of a variation source in an ANOVA experiment.
        /// </summary>
        /// <param name="anova">The associated ANOVA analysis.</param>
        /// <param name="source">The name of the variation source.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom for the source.</param>
        /// <param name="sumOfSquares">The sum of squares of the source.</param>
        public AnovaVariationSource(IAnova anova, string source, double sumOfSquares,
            int degreesOfFreedom)
            : this(anova, source, sumOfSquares, degreesOfFreedom, null)
        {
        }

        /// <summary>
        ///     Creates a new object representation of a variation source in an ANOVA experiment.
        /// </summary>
        /// <param name="anova">The associated ANOVA analysis.</param>
        /// <param name="source">The name of the variation source.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom for the source.</param>
        /// <param name="meanSquares">The mean sum of squares of the source.</param>
        /// <param name="sumOfSquares">The sum of squares of the source.</param>
        public AnovaVariationSource(IAnova anova, string source, double sumOfSquares,
            int degreesOfFreedom, double meanSquares)
            : this(anova, source, sumOfSquares, degreesOfFreedom, meanSquares, null)
        {
        }

        /// <summary>
        ///     Creates a new object representation of a variation source in an ANOVA experiment.
        /// </summary>
        /// <param name="anova">The associated ANOVA analysis.</param>
        /// <param name="source">The name of the variation source.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom for the source.</param>
        /// <param name="sumOfSquares">The sum of squares of the source.</param>
        /// <param name="test">The F-Test containing the F-Statistic for the source.</param>
        public AnovaVariationSource(IAnova anova, string source, double sumOfSquares,
            int degreesOfFreedom, FTest test)
            : this(anova, source, sumOfSquares, degreesOfFreedom, sumOfSquares / degreesOfFreedom, test)
        {
        }

        /// <summary>
        ///     Creates a new object representation of a variation source in an ANOVA experiment.
        /// </summary>
        /// <param name="anova">The associated ANOVA analysis.</param>
        /// <param name="source">The name of the variation source.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom for the source.</param>
        /// <param name="sumOfSquares">The sum of squares of the source.</param>
        /// <param name="meanSquares">The mean sum of squares of the source.</param>
        /// <param name="test">The F-Test containing the F-Statistic for the source.</param>
        public AnovaVariationSource(IAnova anova, string source, double sumOfSquares,
            int degreesOfFreedom, double meanSquares, FTest test)
        {
            this.Anova = anova;
            Source = source;
            SumOfSquares = sumOfSquares;
            DegreesOfFreedom = degreesOfFreedom;
            Significance = test;
            MeanSquares = meanSquares;
        }

        /// <summary>
        ///     Gets the ANOVA associated with this source.
        /// </summary>
        [Browsable(false)]
        public IAnova Anova { get; }

        /// <summary>
        ///     Gets the name of the variation source.
        /// </summary>
        [DisplayName("Source")]
        public string Source { get; private set; }

        /// <summary>
        ///     Gets the sum of squares associated with the variation source.
        /// </summary>
        [DisplayName("Sum of Squares")]
        public double SumOfSquares { get; private set; }

        /// <summary>
        ///     Gets the degrees of freedom associated with the variation source.
        /// </summary>
        [DisplayName("Degrees of Freedom")]
        public int DegreesOfFreedom { get; private set; }

        /// <summary>
        ///     Get the mean squares, or the variance, associated with the source.
        /// </summary>
        [DisplayName("Mean Squares")]
        public double MeanSquares { get; private set; }

        /// <summary>
        ///     Gets the significance of the source.
        /// </summary>
        [DisplayName("P-Value")]
        public FTest Significance { get; private set; }

        /// <summary>
        ///     Gets the F-Statistic associated with the source's significance.
        /// </summary>
        [DisplayName("F-Statistic")]
        public double? Statistic
        {
            get
            {
                if (Significance != null)
                {
                    return Significance.Statistic;
                }

                return null;
            }
        }
    }
}