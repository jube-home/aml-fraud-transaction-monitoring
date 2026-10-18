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

#if NET35 || NET40
namespace Accord.Compat
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   Read-only dictionary wrapper.
    /// </summary>
    /// 
    /// <remarks>
    ///   This collection implements a read-only dictionary. Read-only collections
    ///   can not be changed once they are created and are useful for presenting
    ///   information to the user without allowing alteration.
    /// </remarks>
    /// 
    /// <typeparam name="TKey">The types of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// 
    [Serializable]
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {

        private IDictionary<TKey, TValue> dictionary;

        /// <summary>
        ///   Constructs a new read-only wrapper around a <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// 
        /// <param name="dictionary">The dictionary to wrap.</param>
        /// 
        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        /// <summary>
        ///   Does nothing, as this collection is read-only.
        /// </summary>
        /// 
        public void Add(TKey key, TValue value)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        /// <summary>
        ///   Determines whether the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        ///   contains an element with the specified key.
        /// </summary>
        /// 
        /// <param name="key">The key to locate in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.</param>
        /// 
        /// <returns>
        ///    <c>true</c> if the <see cref="IDictionary{TKey,TValue}"/> contains
        ///     an element with the key; otherwise, false.
        /// </returns>
        /// 
        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        /// <summary>
        ///   Gets an <see cref="ICollection{T}"/> containing the keys of
        ///   the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// 
        /// <value>The keys.</value>
        /// 
        public ICollection<TKey> Keys
        {
            get { return dictionary.Keys; }
        }

        /// <summary>
        ///   Does nothing, as this collection is read-only.
        /// </summary>
        /// 
        public bool Remove(TKey key)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        /// <summary>
        ///    Gets the value associated with the specified key.
        /// </summary>
        /// 
        /// <param name="key">The key whose value to get.</param>
        /// 
        /// <param name="value">
        ///    When this method returns, the value associated with the specified key, if
        ///    the key is found; otherwise, the default value for the type of the value
        ///    parameter. This parameter is passed uninitialized.</param>
        ///    
        /// <returns>
        ///     true if the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        ///     contains an element with the specified key; otherwise, false.</returns>
        ///     
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        ///  Gets an <see cref="ICollection{T}"/> containing the values in
        ///  the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// 
        /// <value>
        ///   An <see cref="ICollection{T}"/> containing the
        ///   values in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        ///  </value>
        ///  
        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }


        /// <summary>
        ///   Gets the element with the specified key. Set is not supported.
        /// </summary>
        /// 
        /// <value>The element with the specified key.</value>
        /// 
        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set { throw new NotSupportedException("This dictionary is read-only"); }
        }

        /// <summary>
        ///   Does nothing, as this collection is read-only.
        /// </summary>
        /// 
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        /// <summary>
        ///   Does nothing, as this collection is read-only.
        /// </summary>
        /// 
        public void Clear()
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        /// <summary>
        ///   Determines whether the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        ///   contains an element with the specified key.
        /// </summary>
        /// 
        /// <param name="item">The key to locate in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.</param>
        /// 
        /// <returns>
        ///   <c>true</c> if the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        ///   contains an element with the key; otherwise, false.
        /// </returns>
        /// 
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        /// <summary>
        ///   Copies the entire <see cref="ReadOnlyDictionary{TKey, TValue}"/> to a 
        ///   compatible one-dimensional Array, starting at the specified index of 
        ///   the target array.
        /// </summary>
        /// 
        /// <param name="array">
        ///   The one-dimensional Array that is the destination
        ///   of the elements copied from <see cref="ReadOnlyDictionary{TKey, TValue}"/>. The 
        ///   Array must have zero-based indexing.</param>
        /// 
        /// <param name="arrayIndex">
        ///   The zero-based index in array at which copying begins. </param>
        /// 
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///   Gets the number of elements contained in this
        ///   <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// 
        public int Count
        {
            get { return dictionary.Count; }
        }

        /// <summary>
        ///   Always returns true.
        /// </summary>
        /// 
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        ///   Does nothing, as this collection is read-only.
        /// </summary>
        /// 
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException("This dictionary is read-only");
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// 
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// 
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (dictionary as System.Collections.IEnumerable).GetEnumerator();
        }

    }
}
#endif