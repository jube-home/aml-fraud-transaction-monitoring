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

using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Fields.Functions;
using Accord.Statistics.Models.Markov;

namespace Accord.Statistics.Models.Fields
{
    /// <summary>
    ///     Utility methods to assist in the creating of <see cref="HiddenConditionalRandomField" />s.
    /// </summary>
    public static class HiddenConditionalRandomField
    {
        /// <summary>
        ///     Creates a new <see cref="HiddenConditionalRandomField{T}" /> from the given <paramref name="classifier" />.
        /// </summary>
        /// <param name="classifier">The classifier.</param>
        /// <returns>
        ///     A <see cref="HiddenConditionalRandomField{T}" /> that implements
        ///     exactly the same model as the given <paramref name="classifier" />.
        /// </returns>
        public static HiddenConditionalRandomField<int> FromHiddenMarkov(HiddenMarkovClassifier classifier)
        {
            return new HiddenConditionalRandomField<int>(new MarkovDiscreteFunction(classifier));
        }

        /// <summary>
        ///     Creates a new <see cref="HiddenConditionalRandomField{T}" /> from the given <paramref name="classifier" />.
        /// </summary>
        /// <param name="classifier">The classifier.</param>
        /// <returns>
        ///     A <see cref="HiddenConditionalRandomField{T}" /> that implements
        ///     exactly the same model as the given <paramref name="classifier" />.
        /// </returns>
        public static HiddenConditionalRandomField<int> FromHiddenMarkov(
            HiddenMarkovClassifier<GeneralDiscreteDistribution, int> classifier)
        {
            return new HiddenConditionalRandomField<int>(new MarkovDiscreteFunction(classifier));
        }

        /// <summary>
        ///     Creates a new <see cref="HiddenConditionalRandomField{T}" /> from the given <paramref name="classifier" />.
        /// </summary>
        /// <param name="classifier">The classifier.</param>
        /// <returns>
        ///     A <see cref="HiddenConditionalRandomField{T}" /> that implements
        ///     exactly the same model as the given <paramref name="classifier" />.
        /// </returns>
        public static HiddenConditionalRandomField<double> FromHiddenMarkov(
            HiddenMarkovClassifier<NormalDistribution, double> classifier)
        {
            return new HiddenConditionalRandomField<double>(new MarkovContinuousFunction(classifier));
        }

        /// <summary>
        ///     Creates a new <see cref="HiddenConditionalRandomField{T}" /> from the given <paramref name="classifier" />.
        /// </summary>
        /// <param name="classifier">The classifier.</param>
        /// <returns>
        ///     A <see cref="HiddenConditionalRandomField{T}" /> that implements
        ///     exactly the same model as the given <paramref name="classifier" />.
        /// </returns>
        public static HiddenConditionalRandomField<double[]> FromHiddenMarkov(
            HiddenMarkovClassifier<Independent<NormalDistribution>, double[]> classifier)
        {
            return new HiddenConditionalRandomField<double[]>(new MarkovMultivariateFunction(classifier));
        }

        /// <summary>
        ///     Creates a new <see cref="HiddenConditionalRandomField{T}" /> from the given <paramref name="classifier" />.
        /// </summary>
        /// <param name="classifier">The classifier.</param>
        /// <returns>
        ///     A <see cref="HiddenConditionalRandomField{T}" /> that implements
        ///     exactly the same model as the given <paramref name="classifier" />.
        /// </returns>
        public static HiddenConditionalRandomField<double[]> FromHiddenMarkov(
            HiddenMarkovClassifier<Independent<NormalDistribution, double>, double[]> classifier)
        {
            return new HiddenConditionalRandomField<double[]>(new MarkovMultivariateFunction(classifier));
        }

        /// <summary>
        ///     Creates a new <see cref="HiddenConditionalRandomField{T}" /> from the given <paramref name="classifier" />.
        /// </summary>
        /// <param name="classifier">The classifier.</param>
        /// <returns>
        ///     A <see cref="HiddenConditionalRandomField{T}" /> that implements
        ///     exactly the same model as the given <paramref name="classifier" />.
        /// </returns>
        public static HiddenConditionalRandomField<double[]> FromHiddenMarkov(
            HiddenMarkovClassifier<MultivariateNormalDistribution, double[]> classifier)
        {
            return new HiddenConditionalRandomField<double[]>(new MarkovMultivariateFunction(classifier));
        }
    }
}