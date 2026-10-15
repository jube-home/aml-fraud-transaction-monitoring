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
    ///     <see cref="Multivariate.Independent{T}">
    ///         multivariate independent distributions
    ///     </see>
    ///     .
    /// </summary>
    public class IndependentOptions : IFittingOptions
    {
        /// <summary>
        ///     Gets or sets the fitting options for the inner
        ///     independent components in the joint distribution.
        /// </summary>
        public IFittingOptions InnerOption { get; set; }

        /// <summary>
        ///     Gets or sets the fitting options for specific inner
        ///     independent components in the joint distribution.
        /// </summary>
        public IFittingOptions[] InnerOptions { get; set; }

        /// <summary>
        ///     Gets or sets whether the data to be fitted has already been transposed.
        /// </summary>
        public bool Transposed { get; set; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }

    /// <summary>
    ///     Estimation options for
    ///     <see cref="Multivariate.Independent{T}">
    ///         multivariate independent distributions
    ///     </see>
    ///     .
    /// </summary>
    [Serializable]
    public class IndependentOptions<TOptions> : IndependentOptions
        where TOptions : class, IFittingOptions, new()
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="IndependentOptions{TOptions}" /> class.
        /// </summary>
        public IndependentOptions()
        {
            InnerOption = new TOptions();
        }

        /// <summary>
        ///     Gets or sets the fitting options for the inner
        ///     independent components in the joint distribution.
        /// </summary>
        public new TOptions InnerOption
        {
            get => base.InnerOption as TOptions;
            set => base.InnerOption = value;
        }

        /// <summary>
        ///     Gets or sets the fitting options for specific inner
        ///     independent components in the joint distribution.
        /// </summary>
        public new TOptions[] InnerOptions
        {
            get => base.InnerOptions as TOptions[];
            set => base.InnerOptions = value;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}