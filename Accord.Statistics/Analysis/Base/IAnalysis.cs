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

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Determines the method to be used in a statistical analysis.
    /// </summary>
    public enum AnalysisMethod
    {
        /// <summary>
        ///     By choosing Center, the method will be run on the mean-centered data.
        /// </summary>
        /// <remarks>
        ///     In Principal Component Analysis this means the method will operate
        ///     on the Covariance matrix of the given data.
        /// </remarks>
        Center = 0,

        /// <summary>
        ///     By choosing Standardize, the method will be run on the mean-centered and
        ///     standardized data.
        /// </summary>
        /// <remarks>
        ///     In Principal Component Analysis this means the method
        ///     will operate on the Correlation matrix of the given data. One should always
        ///     choose to standardize when dealing with different units of variables.
        /// </remarks>
        Standardize = 1
    }

    /// <summary>
    ///     Determines the method to be used in a statistical analysis.
    /// </summary>
    public enum PrincipalComponentMethod
    {
        /// <summary>
        ///     By choosing Center, the method will be run on the mean-centered data.
        /// </summary>
        /// <remarks>
        ///     In Principal Component Analysis this means the method will operate
        ///     on the Covariance matrix of the given data.
        /// </remarks>
        Center = 0,

        /// <summary>
        ///     By choosing Standardize, the method will be run on the mean-centered and
        ///     standardized data.
        /// </summary>
        /// <remarks>
        ///     In Principal Component Analysis this means the method
        ///     will operate on the Correlation matrix of the given data. One should always
        ///     choose to standardize when dealing with different units of variables.
        /// </remarks>
        Standardize = 1,

        /// <summary>
        ///     By choosing CorrelationMatrix, the method will interpret the given data
        ///     as a correlation matrix.
        /// </summary>
        CorrelationMatrix = 2,

        /// <summary>
        ///     By choosing CovarianceMatrix, the method will interpret the given data
        ///     as a correlation matrix.
        /// </summary>
        CovarianceMatrix = 3,

        /// <summary>
        ///     By choosing KernelMatrix, the method will interpret the given data
        ///     as a Kernel (Gram) matrix.
        /// </summary>
        KernelMatrix = 4
    }

    /// <summary>
    ///     Common interface for statistical analysis.
    /// </summary>
    [Obsolete]
    public interface IAnalysis
    {
        /// <summary>
        ///     Computes the analysis using given source data and parameters.
        /// </summary>
        void Compute();
    }
}