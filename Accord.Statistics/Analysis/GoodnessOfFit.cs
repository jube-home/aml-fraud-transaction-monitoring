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
using Accord.Statistics.Distributions;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Goodness-of-fit result for a given distribution.
    /// </summary>
    [Serializable]
    public class GoodnessOfFit : IComparable<GoodnessOfFit>, IComparable,
        IFormattable
    {
        internal GoodnessOfFit(DistributionAnalysis analysis, int index)
        {
            this.Analysis = analysis;
            this.Index = index;
        }

        /// <summary>
        ///     Gets the analysis that has produced this measure.
        /// </summary>
        [Browsable(false)]
        public DistributionAnalysis Analysis { get; }

        /// <summary>
        ///     Gets the variable's index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the distribution name
        /// </summary>
        public string Name => Analysis.DistributionNames[Index];

        /// <summary>
        ///     Gets (a clone of) the measured distribution.
        /// </summary>
        /// <value>
        ///     The distribution associated with this good-of-fit measure.
        /// </value>
        public IFittableDistribution<double> Distribution =>
            (IFittableDistribution<double>)Analysis.Distributions[Index].Clone();

        /// <summary>
        ///     Gets the value of the Kolmogorov-Smirnov statistic.
        /// </summary>
        /// <value>
        ///     The Kolmogorov-Smirnov for the <see cref="Distribution" />.
        /// </value>
        public double KolmogorovSmirnov
        {
            get
            {
                if (Analysis.KolmogorovSmirnov[Index] == null)
                {
                    return double.NaN;
                }

                return Analysis.KolmogorovSmirnov[Index].Statistic;
            }
        }

        /// <summary>
        ///     Gets the rank of this distribution according to the
        ///     <see cref="KolmogorovSmirnovTest">Kolmogorov-Smirnov test</see>.
        /// </summary>
        /// <value>
        ///     An integer value where 0 indicates most probable.
        /// </value>
        public int KolmogorovSmirnovRank => Analysis.KolmogorovSmirnovRank[Index];

        /// <summary>
        ///     Gets the value of the Chi-Square statistic.
        /// </summary>
        /// <value>
        ///     The Chi-Square for the <see cref="Distribution" />.
        /// </value>
        public double ChiSquare
        {
            get
            {
                if (Analysis.ChiSquare[Index] == null)
                {
                    return double.NaN;
                }

                return Analysis.ChiSquare[Index].Statistic;
            }
        }

        /// <summary>
        ///     Gets the rank of this distribution according to the
        ///     <see cref="ChiSquareTest">Chi-Square test</see>.
        /// </summary>
        /// <value>
        ///     An integer value where 0 indicates most probable.
        /// </value>
        public int ChiSquareRank => Analysis.ChiSquareRank[Index];

        /// <summary>
        ///     Gets the value of the Anderson-Darling statistic.
        /// </summary>
        /// <value>
        ///     The Anderson-Darling for the <see cref="Distribution" />.
        /// </value>
        public double AndersonDarling
        {
            get
            {
                if (Analysis.AndersonDarling[Index] == null)
                {
                    return double.NaN;
                }

                return Analysis.AndersonDarling[Index].Statistic;
            }
        }

        /// <summary>
        ///     Gets the rank of this distribution according to the
        ///     <see cref="AndersonDarlingTest">Anderson-Darling test</see>.
        /// </summary>
        /// <value>
        ///     An integer value where 0 indicates most probable.
        /// </value>
        public int AndersonDarlingRank => Analysis.AndersonDarlingRank[Index];

        /// <summary>
        ///     Compares the current instance with another object of the same type and returns an
        ///     integer that indicates whether the current instance precedes, follows, or occurs in
        ///     the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return
        ///     value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" />
        ///     in the sort order. Zero This instance occurs in the same position in the sort order as
        ///     <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" />
        ///     in the sort order.
        /// </returns>
        public int CompareTo(object obj)
        {
            return CompareTo(obj as GoodnessOfFit);
        }

        /// <summary>
        ///     Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value
        ///     has the following meanings: Value Meaning Less than zero This object is less than the
        ///     <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />.
        ///     Greater than zero This object is greater than <paramref name="other" />.
        /// </returns>
        public int CompareTo(GoodnessOfFit other)
        {
            return ChiSquareRank.CompareTo(other.ChiSquareRank);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">
        ///     The format to use.-or- A null reference (Nothing in Visual Basic) to use the default format
        ///     defined for the type of the <see cref="T:System.IFormattable" /> implementation.
        /// </param>
        /// <param name="formatProvider">
        ///     The provider to use to format the value.-or- A null reference (Nothing in Visual Basic) to
        ///     obtain the numeric format information from the current locale setting of the operating system.
        /// </param>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            var dist = Analysis.Distributions[Index];
            var fmt = dist as IFormattable;
            if (fmt != null)
            {
                return fmt.ToString(format, formatProvider);
            }

            return dist.ToString();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Analysis.Distributions[Index].ToString();
        }
    }
}