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
using System.IO;
using System.Runtime.Serialization;

namespace Accord.IO
{
    /// <summary>
    ///     Model serializer. Can be used to serialize and deserialize (i.e. save and
    ///     load) models from the framework to and from the disk and other streams.
    /// </summary>
    /// <remarks>
    ///     This class uses a binding mechanism to automatically convert files saved using
    ///     older versions of the framework to the new format. If a deserialization doesn't
    ///     work, please fill in a bug report at https://github.com/accord-net/framework/issues
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The first example shows the simplest way to use the serializer to persist objects:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Core\SerializerTest.cs" region="doc_simple" />
    ///     <para>
    ///         The second example shows the same, but using compression:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Core\SerializerTest.cs" region="doc_compression" />
    ///     <para>
    ///         The third and last example shows a complete example on how to create, save and re-load
    ///         a classifier from disk using serialization:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\KNearestNeighbors\KNearestNeighborsTest.cs"
    ///         region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\KNearestNeighbors\KNearestNeighborsTest.cs"
    ///         region="doc_serialization" />
    /// </example>
    public static class Serializer
    {
        private const SerializerCompression DEFAULT_COMPRESSION = SerializerCompression.None;

        /// <summary>
        ///     Saves an object to a stream.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="stream">The stream to which the object is to be serialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        public static void Save<T>(this T obj, Stream stream, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Saves an object to a stream.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="formatter">The binary formatter.</param>
        /// <param name="stream">The stream to which the object is to be serialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
#if NETSTANDARD1_4
    internal
#else
        public
#endif
            static void Save<T>(this T obj, object formatter, Stream stream,
                SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Saves an object to a stream.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="path">The path to the file to which the object is to be serialized.</param>
        public static void Save<T>(this T obj, string path)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Saves an object to a stream.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="path">The path to the file to which the object is to be serialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        public static void Save<T>(this T obj, string path, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Saves an object to a stream, represented as an array of bytes.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        public static byte[] Save<T>(this T obj, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Saves an object to a stream.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="bytes">The sequence of bytes to which the object has been serialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        public static void Save<T>(this T obj, out byte[] bytes,
            SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads an object from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the object is to be deserialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        /// <returns>The deserialized machine.</returns>
        public static T Load<T>(Stream stream, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Loads an object from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the object is to be deserialized.</param>
        /// <returns>The deserialized object.</returns>
        public static T Load<T>(string path)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads an object from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the object is to be deserialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        /// <returns>The deserialized object.</returns>
        public static T Load<T>(string path, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads an object from a stream, represented as an array of bytes.
        /// </summary>
        /// <param name="bytes">The byte stream containing the object to be deserialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        /// <returns>The deserialized object.</returns>
        public static T Load<T>(byte[] bytes, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads an object from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the object is to be deserialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        /// <param name="value">
        ///     The object to be read. This parameter can be used to avoid the
        ///     need of specifying a generic argument to this function.
        /// </param>
        /// <returns>The deserialized machine.</returns>
        public static T Load<T>(Stream stream, out T value, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads an object from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the object is to be deserialized.</param>
        /// <param name="value">
        ///     The object to be read. This parameter can be used to avoid the
        ///     need of specifying a generic argument to this function.
        /// </param>
        /// <returns>The deserialized object.</returns>
        public static T Load<T>(string path, out T value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads an object from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the object is to be deserialized.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        /// <param name="value">
        ///     The object to be read. This parameter can be used to avoid the
        ///     need of specifying a generic argument to this function.
        /// </param>
        /// <returns>The deserialized object.</returns>
        public static T Load<T>(string path, out T value, SerializerCompression compression)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads an object from a stream, represented as an array of bytes.
        /// </summary>
        /// <param name="bytes">The byte stream containing the object to be deserialized.</param>
        /// <param name="value">
        ///     The object to be read. This parameter can be used to avoid the
        ///     need of specifying a generic argument to this function.
        /// </param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        /// <returns>The deserialized object.</returns>
        public static T Load<T>(byte[] bytes, out T value, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Loads a model from a stream.
        /// </summary>
        /// <typeparam name="T">The type of the model to be loaded.</typeparam>
        /// <param name="formatter">The binary formatter.</param>
        /// <param name="stream">The stream from which to deserialize the object graph.</param>
        /// <param name="compression">The type of compression to use. Default is None.</param>
        /// <returns>The deserialized object.</returns>
#if NETSTANDARD1_4
    internal
#else
        public
#endif
            static T Load<T>(Stream stream, object formatter, SerializerCompression compression = DEFAULT_COMPRESSION)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Performs a deep copy of an object by serializing and deserializing it.
        /// </summary>
        /// <typeparam name="T">The type of the model to be copied.</typeparam>
        /// <param name="obj">The object.</param>
        /// <returns>A deep copy of the given object.</returns>
        public static T DeepClone<T>(this T obj)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Retrieves a value from the SerializationInfo store.
        /// </summary>
        /// <typeparam name="T">The type of the value to be retrieved.</typeparam>
        /// <param name="info">The serialization info store containing the value.</param>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value retrieved from the info store.</param>
        /// <returns>The value retrieved from the info store.</returns>
        public static T GetValue<T>(this SerializationInfo info, string name, out T value)
        {
            return value = (T)info.GetValue(name, typeof(T));
        }
    }
}