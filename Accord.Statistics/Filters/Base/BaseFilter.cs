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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading;

namespace Accord.Statistics.Filters
{
    /// <summary>
    ///     Base abstract class for the Data Table preprocessing filters.
    /// </summary>
    /// <typeparam name="TOptions">The column options type.</typeparam>
    /// <typeparam name="TFilter">The filter type to whom these options should belong to.</typeparam>
    [Serializable]
    public abstract class BaseFilter<TOptions, TFilter> : IFilter, IEnumerable<TOptions>
        where TOptions : ColumnOptionsBase<TFilter>, new()
        where TFilter : BaseFilter<TOptions, TFilter>
    {
        [NonSerialized] private CancellationToken token;

        /// <summary>
        ///     Creates a new DataTable Filter Base.
        /// </summary>
        protected BaseFilter()
        {
            Columns = new ColumnOptionCollection<TOptions, TFilter>();
            Columns.AddingNew += Columns_AddingNew;
            Active = true;
        }

        /// <summary>
        ///     Gets or sets whether this filter is active. An inactive
        ///     filter will repass the input table as output unchanged.
        /// </summary>
        [Description("Gets or sets whether this filter is active.")]
        public bool Active { get; set; }

        /// <summary>
        ///     Gets the collection of filter options.
        /// </summary>
        [Description("Gets or sets processing options for the columns in the source DataTables")]
        public ColumnOptionCollection<TOptions, TFilter> Columns { get; private set; }

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
        ///     Gets options associated with a given variable (data column).
        /// </summary>
        /// <param name="columnName">The name of the variable.</param>
        public TOptions this[string columnName]
        {
            get
            {
                if (!Columns.Contains(columnName))
                {
                    Columns.Add(new TOptions
                    {
                        ColumnName = columnName,
                        Owner = (TFilter)this
                    });
                }

                return Columns[columnName];
            }
        }

        /// <summary>
        ///     Gets options associated with a given variable (data column).
        /// </summary>
        /// <param name="index">The column's index for the variable.</param>
        public TOptions this[int index]
        {
            get
            {
                for (var i = Columns.Count; i <= index; i++)
                {
                    Columns.Add(new TOptions
                    {
                        ColumnName = i.ToString(),
                        Owner = (TFilter)this
                    });
                }

                return Columns[index];
            }
        }

        /// <summary>
        ///     Gets the number of inputs accepted by the model.
        /// </summary>
        /// <value>The number of inputs.</value>
        public int NumberOfInputs
        {
            get => Columns.Count;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<TOptions> GetEnumerator()
        {
            return Columns.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Columns.GetEnumerator();
        }

        /// <summary>
        ///     Add a new column options definition to the collection.
        /// </summary>
        public void Add(TOptions options)
        {
            Columns.Add(options);
        }

        private void Columns_AddingNew(object sender, TOptions e)
        {
            OnAddingOptions(e);
        }

        /// <summary>
        ///     Called when a new column options definition is being added.
        ///     Can be used to validate or modify these options beforehand.
        /// </summary>
        /// <param name="options">The column options being added.</param>
        protected virtual void OnAddingOptions(TOptions options)
        {
        }

#if !NETSTANDARD1_4
        /// <summary>
        ///     Applies the Filter to a <see cref="DataTable" />.
        /// </summary>
        /// <param name="data">The source <see cref="DataTable" />.</param>
        /// <param name="columnNames">The name of the columns that should be processed.</param>
        /// <returns>The processed <see cref="DataTable" />.</returns>
        public DataTable Apply(DataTable data, params string[] columnNames)
        {
            // Initial argument checking
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (Active)
            {
                return ProcessFilter(data.DefaultView.ToTable(false, columnNames));
            }

            return data;
        }

        /// <summary>
        ///     Applies the Filter to a <see cref="DataTable" />.
        /// </summary>
        /// <param name="data">The source <see cref="DataTable" />.</param>
        /// <returns>The processed <see cref="DataTable" />.</returns>
        public DataTable Apply(DataTable data)
        {
            // Initial argument checking
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (Active)
            {
                return ProcessFilter(data);
            }

            return data;
        }

        /// <summary>
        ///     Processes the current filter.
        /// </summary>
        protected abstract DataTable ProcessFilter(DataTable data);
#endif
    }
}