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

namespace Accord
{
    /// <summary>
    ///     Runtime cast.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <typeparam name="U">The source type.</typeparam>
    internal struct Cast<T, U>
    {
        /// <summary>
        ///     Gets the value being casted.
        /// </summary>
        public T Value { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cast{T,U}" /> struct.
        /// </summary>
        public Cast(U value)
        {
            this.Value = (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        ///     Performs an implicit conversion from <typeparamref name="U" /> to <see cref="Cast{T,U}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T, U>(U value)
        {
            return new Cast<T, U>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="Cast{T,U}" /> to <typeparamref name="T" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator T(Cast<T, U> value)
        {
            return value.Value;
        }
    }

    /// <summary>
    ///     Runtime cast.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    internal struct Cast<T>
    {
        /// <summary>
        ///     Gets the value being casted.
        /// </summary>
        public T Value { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cast{T}" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Cast(object value)
        {
            this.Value = (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Double" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(double value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Single" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(float value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="decimal" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(decimal value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="byte" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(byte value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="sbyte" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(sbyte value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="short" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(short value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="ushort" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(ushort value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="int" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(int value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="uint" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(uint value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="long" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(long value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="ulong" /> to <see cref="Cast{T}" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Cast<T>(ulong value)
        {
            return new Cast<T>(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="Cast{T}" /> to <typeparamref name="T" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator T(Cast<T> value)
        {
            return value.Value;
        }
    }
}