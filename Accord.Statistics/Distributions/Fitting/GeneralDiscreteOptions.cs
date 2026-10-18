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

namespace Accord.Statistics.Distributions.Fitting
{
    /// <summary>
    ///     Estimation options for
    ///     <see cref="Univariate.GeneralDiscreteDistribution">general discrete (categorical) distributions</see>.
    /// </summary>
    [Serializable]
    public class GeneralDiscreteOptions : IFittingOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GeneralDiscreteOptions" /> class.
        /// </summary>
        public GeneralDiscreteOptions()
        {
            Minimum = 1e-10;
        }

        /// <summary>
        ///     Gets or sets the minimum allowed probability
        ///     in the frequency tables specifying the discrete
        ///     distribution. Default is 1e-10.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        ///     Gets ors sets whether to use Laplace's rule of succession
        ///     to avoid zero probabilities. Default is false.
        /// </summary>
        public bool UseLaplaceRule { get; set; }

        /// <summary>
        ///     Gets or sets how much percent of the previous value for
        ///     the distribution should be kept in its updated value. Default is 0.
        /// </summary>
        public double Regularization { get; set; }

        /// <summary>
        ///     Gets or sets whether current frequency values in the distribution
        ///     should be considered as priors during the next time the distribution
        ///     is estimated. Default is false.
        /// </summary>
        public bool UsePreviousValuesAsPriors { get; set; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}