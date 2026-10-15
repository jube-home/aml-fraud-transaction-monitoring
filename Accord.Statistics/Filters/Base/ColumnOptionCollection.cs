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
using System.Collections.ObjectModel;

namespace Accord.Statistics.Filters
{
    /// <summary>
    ///     Column option collection.
    /// </summary>
    /// <typeparam name="TFilter">The type of the filter that this collection belongs to.</typeparam>
    /// <typeparam name="TOptions">
    ///     The type of the column options that will be used by the
    ///     <typeparamref name="TFilter" /> to determine how to process a particular column.
    /// </typeparam>
    [Serializable]
    public class ColumnOptionCollection<TOptions, TFilter> : KeyedCollection<string, TOptions>
        where TOptions : ColumnOptionsBase<TFilter>
    {
        /// <summary>
        ///     Occurs when a new <typeparamref name="TOptions" /> is being
        ///     added to the collection. Handlers of this event can prevent a column
        ///     options from being added by throwing an exception.
        /// </summary>
#if !NET35 && !NET40
        public event EventHandler<TOptions> AddingNew;
#else
        public event ColumnOptionsEventHandler AddingNew;

        /// <summary>
        ///   Compatibility event args for the <see cref="AddingNew"/> event. This
        ///   is only required and used for the .NET 3.5 version of the framework.
        /// </summary>
        /// 
        public delegate void ColumnOptionsEventHandler(object sender, TOptions options);
#endif

        /// <summary>
        ///     Extracts the key from the specified column options.
        /// </summary>
        protected override string GetKeyForItem(TOptions item)
        {
            return item.ColumnName;
        }

        /// <summary>
        ///     Adds a new column options definition to the collection.
        /// </summary>
        /// <param name="options">The column options to be added.</param>
        /// <returns>The added column options.</returns>
        public new TOptions Add(TOptions options)
        {
            if (AddingNew != null)
            {
                AddingNew(this, options);
            }

            base.Add(options);
            return options;
        }

        /// <summary>
        ///     Gets the associated options for the given column name.
        /// </summary>
        /// <param name="columnName">The name of the column whose options should be retrieved.</param>
        /// <param name="options">The retrieved options.</param>
        /// <returns>True if the options was contained in the collection; false otherwise.</returns>
        public new bool TryGetValue(string columnName, out TOptions options)
        {
            var columnOptionsBases = Dictionary;
            return columnOptionsBases.TryGetValue(columnName, out options);
        }
    }
}