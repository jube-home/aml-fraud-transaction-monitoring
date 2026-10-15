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
using System.Reflection;
using Accord.Math.Distances;

namespace Accord.Math
{
    /// <summary>
    ///     Static class Distance. Defines a set of extension methods defining distance measures.
    /// </summary>
    public static partial class Distance
    {
        /// <summary>
        ///     Checks whether a function is a real metric distance, i.e. respects
        ///     the triangle inequality. Please note that a function can still pass
        ///     this test and not respect the triangle inequality.
        /// </summary>
        public static bool IsMetric(Func<double[], double[], double> value)
        {
            // Direct test
            var z = value(new[] { 1.0 }, new[] { 1.0 });
            if (z > 2 || z < 0)
            {
                return false;
            }


            var a = new double[1];
            var b = new double[1];

            for (var i = -10; i < 10; i++)
            {
                a[0] = i;

                for (var j = -10; j < +10; j++)
                {
                    b[0] = j;
                    var c = value(a, b);

                    if (c > System.Math.Abs(i) + System.Math.Abs(j))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Checks whether a function is a real metric distance, i.e. respects
        ///     the triangle inequality. Please note that a function can still pass
        ///     this test and not respect the triangle inequality.
        /// </summary>
        public static bool IsMetric(Func<int[], int[], double> value)
        {
            // Direct test
            var z = value(new[] { 1 }, new[] { 1 });
            if (z > 2 || z < 0)
            {
                return false;
            }

            var size = 3;
            var zero = new int[size];

            foreach (var a in Combinatorics.Sequences(3, size, true))
            {
                foreach (var b in Combinatorics.Sequences(3, size, true))
                {
                    var dza = value(zero, a);
                    var dzb = value(zero, b);
                    var dab = value(a, b);

                    if (dab > dza + dzb)
                    {
                        return false;
                    }

                    var daz = value(a, zero);
                    var dbz = value(b, zero);
                    var dba = value(b, a);

                    if (daz != dza || dbz != dzb || dab != dba)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Checks whether a function is a real metric distance, i.e. respects
        ///     the triangle inequality. Please note that a function can still pass
        ///     this test and not respect the triangle inequality.
        /// </summary>
        public static bool IsMetric<T>(IDistance<T> value)
        {
            return value is IMetric<T>;
        }

        /// <summary>
        ///     Gets the a <see cref="IDistance{T}" /> object implementing a
        ///     particular method of the <see cref="Distance" /> static class.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is intended to be used in scenarios where you have been using any
        ///         of the static methods in the <see cref="Distance" /> class, but now you would like
        ///         to obtain a reference to an object that implements the same distance you have been
        ///         using before, but in a object-oriented, polymorphic manner. Please see the example
        ///         below for more details.
        ///     </para>
        ///     <para>
        ///         Note: This method relies on reflection and might not work
        ///         on all scenarios, environments, and/or platforms.
        ///     </para>
        /// </remarks>
        /// <typeparam name="T">The type of the elements being compared in the distance function.</typeparam>
        /// <param name="func">The method of <see cref="Distance" />.</param>
        /// <returns>
        ///     An object of the class that implements the given distance.
        /// </returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\DistanceTest.cs" region="doc_getdistance" />
        /// </example>
        public static IDistance<T> GetDistance<T>(Func<T, T, double> func)
        {
#if NETSTANDARD1_4
            var methods = typeof(Distance).GetTypeInfo().DeclaredMethods.Where(m=>m.IsPublic && m.IsStatic);
#else
            var methods = typeof(Distance).GetMethods(BindingFlags.Public | BindingFlags.Static);
#endif
            foreach (var method in methods)
            {
#if NETSTANDARD1_4
                var methodInfo = func.GetMethodInfo();
#else
                var methodInfo = func.Method;
#endif
                if (methodInfo == method)
                {
                    var t = Type.GetType("Accord.Math.Distances." + method.Name);

                    if (t == null)
                    {
                        // TODO: Remove the following special case, as it is needed only
                        // for preserving compatibility for a few next releases more.
                        if (methodInfo.Name == "BitwiseHamming")
                        {
                            return new Hamming() as IDistance<T>;
                        }
                    }

                    return (IDistance<T>)Activator.CreateInstance(t, new object[] { });
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets the Bitwise Hamming distance between two points.
        ///     Please use the <see cref="Hamming(byte[], byte[])">Distance.Hamming</see>
        ///     method or the <see cref="Distances.Hamming" /> class instead.
        /// </summary>
        [Obsolete("Please use Distance.Hamming instead.")]
        public static double BitwiseHamming(byte[] x, byte[] y)
        {
            return Hamming(x, y);
        }

        /// <summary>
        ///     Gets the Levenshtein distance between two points.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The Levenshtein distance between x and y.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static double Levenshtein<T>(T[] x, T[] y)
        {
            return new Levenshtein<T>().Distance(x, y);
        }
    }
}