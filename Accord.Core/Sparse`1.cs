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
using System.Globalization;
using System.Linq;
using System.Text;

namespace Accord.Math
{
    /// <summary>
    ///     Sparse vector representation (in LibSVM format).
    /// </summary>
    /// <typeparam name="T">The type for the non-zero elements in this vector.</typeparam>
    [Serializable]
    public sealed class Sparse<T> : IEnumerable<T>, ICloneable, IList<T>, IList, IFormattable
        where T : IEquatable<T>
    {
        private int[] indices;
        private T[] values;

        /// <summary>
        ///     Creates a sparse vector with zero elements.
        /// </summary>
        public Sparse()
            : this(0)
        {
        }

        /// <summary>
        ///     Creates a sparse vector with the maximum number of elements.
        /// </summary>
        /// <param name="length">
        ///     The maximum number of non-zero
        ///     elements that this vector can accomodate.
        /// </param>
        public Sparse(int length)
        {
            indices = new int[length];
            values = new T[length];
        }

        /// <summary>
        ///     Creates a sparse vector from a vector of indices
        ///     and a vector of values occuring at those indices.
        /// </summary>
        /// <param name="indices">The indices for non-zero entries.</param>
        /// <param name="values">The non-zero values happening at each index.</param>
        public Sparse(int[] indices, T[] values)
        {
            this.indices = indices;
            this.values = values;
        }

        /// <summary>
        ///     Gets or sets the vector of indices indicating the location
        ///     of the non-zero elements contained in this sparse vector.
        /// </summary>
        public int[] Indices
        {
            get => indices;
            set => indices = value;
        }

        /// <summary>
        ///     Gets or sets the vector of values indicating which non-zero
        ///     value happens at each position indicated in <see cref="Indices" />.
        /// </summary>
        public T[] Values
        {
            get => values;
            set => values = value;
        }

        /// <summary>
        ///     Gets the the value stored at position <paramref name="i" />.
        /// </summary>
        public T this[int i]
        {
            get
            {
                var j = Array.IndexOf(Indices, i);
                if (j >= 0)
                {
                    return Values[j];
                }

                return default;
            }
            set
            {
                var j = Array.IndexOf(Indices, i);
                if (j >= 0)
                {
                    Values[j] = value;
                    return;
                }

                var newValues = new T[Values.Length + 1];
                var newIndices = new int[indices.Length + 1];

                int k;
                for (k = 0; k < indices.Length && indices[k] < i; k++)
                {
                    newIndices[k] = indices[k];
                    newValues[k] = values[k];
                }

                newIndices[k] = i;
                newValues[k] = value;
                k++;
                for (; k < newIndices.Length; k++)
                {
                    newIndices[k] = indices[k - 1];
                    newValues[k] = values[k - 1];
                }

                indices = newIndices;
                values = newValues;
            }
        }

        /// <summary>
        ///     Gets the maximum non-zero element index in the sparse vector.
        /// </summary>
        public int Length
        {
            get
            {
                var zero = default(T);

                for (var i = Indices.Length - 1; i >= 0; i--)
                {
                    if (!Values[i].Equals(zero))
                    {
                        return Indices[i] + 1;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new Sparse<T>((int[])indices.Clone(), (T[])values.Clone());
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach (var t in ToDense())
            {
                yield return t;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var t in ToDense())
            {
                yield return t;
            }
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Indices.Length; i++)
            {
                sb.Append(Indices[i] + 1); // Note: LibSVM array format is one-based
                sb.Append(":");
                sb.AppendFormat(formatProvider, "{0:" + format + "}", Values[i]);
                if (i < Indices.Length - 1)
                {
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }

        int IList.Add(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Clear()
        {
            Array.Clear(values, 0, values.Length);
        }

        bool IList.Contains(object value)
        {
            return ((IList)Values).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        bool IList.IsFixedSize => true;

        bool IList.IsReadOnly => false;

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        object IList.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            for (var i = 0; i < indices.Length; i++)
            {
                array.SetValue(values[i], index + indices[i]);
            }
        }

        int ICollection.Count => 0;

        bool ICollection.IsSynchronized => throw new NotImplementedException();

        object ICollection.SyncRoot => throw new NotImplementedException();

        int IList<T>.IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        T IList<T>.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Clear()
        {
            Array.Clear(values, 0, values.Length);
        }

        bool ICollection<T>.Contains(T item)
        {
            return values.Contains(item);
        }

        /// <summary>
        ///     Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an
        ///     <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied
        ///     from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have
        ///     zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < indices.Length; i++)
            {
                array[arrayIndex + indices[i]] = values[i];
            }
        }

        int ICollection<T>.Count => 0;

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Converts this sparse vector to a dense vector of the given length.
        /// </summary>
        public T[] ToDense()
        {
            return ToDense(Indices.Max() + 1);
        }

        /// <summary>
        ///     Converts this sparse vector to a dense vector of the given length.
        /// </summary>
        public T[] ToDense(int length)
        {
            var result = new T[length];
            for (var i = 0; i < Indices.Length; i++)
            {
                result[Indices[i]] = Values[i];
            }

            return result;
        }

        /// <summary>
        ///     Converts this sparse vector to a sparse representation where
        ///     the indices are intertwined with their corresponding values.
        /// </summary>
        public T[] ToSparse()
        {
            var result = new T[Indices.Length * 2];
            for (var i = 0; i < Indices.Length; i++)
            {
                result[2 * i + 0] = (T)Convert.ChangeType(Indices[i], typeof(T));
                result[2 * i + 1] = Values[i];
            }

            return result;
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="Sparse{T}" /> to <see cref="Array" />.
        /// </summary>
        public static implicit operator Array(Sparse<T> obj)
        {
            return obj.Values;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ToString("g", CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Determines whether this Sparse vector has elements on all indices.
        /// </summary>
        /// <returns><c>true</c> if this instance is full; otherwise, <c>false</c>.</returns>
        public bool IsFull()
        {
            for (var i = 0; i < Indices.Length; i++)
            {
                if (Indices[i] != i)
                {
                    return false;
                }
            }

            return true;
        }
    }
}