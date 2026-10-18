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

namespace Accord.Statistics.Filters
{
#if !NETSTANDARD1_4
    using System.Data;
#endif

    /// <summary>
    ///     Sample processing filter interface.
    /// </summary>
    /// <remarks>
    ///     The interface defines the set of methods which should be
    ///     provided by all table processing filters. Methods of this interface should
    ///     keep the source table unchanged and return the result of data processing
    ///     filter as new data table.
    /// </remarks>
    public interface IFilter
    {
#if !NETSTANDARD1_4
        /// <summary>
        ///     Applies the filter to a <see cref="DataTable" />.
        /// </summary>
        /// <param name="data">Source table to apply filter to.</param>
        /// <returns>
        ///     Returns filter's result obtained by applying the filter to
        ///     the source table.
        /// </returns>
        /// <remarks>
        ///     The method keeps the source table unchanged and returns the
        ///     the result of the table processing filter as new data table.
        /// </remarks>
        DataTable Apply(DataTable data);
#endif
    }
}