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
using System.Threading;

namespace Accord.Statistics.Filters
{
    /// <summary>
    ///     Column options for filter which have per-column settings.
    /// </summary>
    [Serializable]
    public abstract class ColumnOptionsBase<TFilter>
    {
        [NonSerialized] private CancellationToken token;

        /// <summary>
        ///     Constructs the base class for Column Options.
        /// </summary>
        /// <param name="column">Column's name.</param>
        protected ColumnOptionsBase(string column)
        {
            ColumnName = column;
        }

        /// <summary>
        ///     Gets or sets the filter to which these options belong to.
        /// </summary>
        /// <value>The owner filter.</value>
        public TFilter Owner { get; set; }

        /// <summary>
        ///     Gets or sets the name of the column that the options will apply to.
        /// </summary>
        public string ColumnName { get; internal set; }

        /// <summary>
        ///     Gets or sets a user-determined object associated with this column.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        /// <value>The token.</value>
        public CancellationToken Token
        {
            get => token;
            set => token = value;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ColumnName;
        }
    }
}