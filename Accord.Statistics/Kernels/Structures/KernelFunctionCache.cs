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
using System.Collections.Generic;
using System.Diagnostics;
using Accord.Math;
using Debug = Accord.Diagnostics.Debug;

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Value cache for kernel function evaluations. The total memory size occupied by the
    ///     cache can fluctuate between <see cref="KernelFunctionCache{TKernel, TInput}.MinimumBytes" />
    ///     and <see cref="KernelFunctionCache{TKernel, TInput}.MaximumBytes" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class works as a least-recently-used cache for elements
    ///         computed from a the kernel (Gram) matrix. Elements which have
    ///         not been needed for some time are discarded from the cache;
    ///         while elements which are constantly requested remains cached.
    ///     </para>
    ///     <para>
    ///         The use of cache may speedup learning by a large factor; however
    ///         the actual speedup may vary according to the choice of cache size.
    ///     </para>
    ///     <para>
    ///         The total memory size occupied by the cache can fluctuate between
    ///         <see cref="KernelFunctionCache{TKernel, TInput}.MinimumBytes" />
    ///         and <see cref="KernelFunctionCache{TKernel, TInput}.MaximumBytes" />.
    ///     </para>
    /// </remarks>
    public class KernelFunctionCache : KernelFunctionCache<IKernel, double[]>
    {
        /// <summary>
        ///     Constructs a new <see cref="KernelFunctionCache" />.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="inputs">The inputs values.</param>
        public KernelFunctionCache(IKernel kernel, double[][] inputs)
            : base(kernel, inputs)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="KernelFunctionCache" />.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="inputs">The inputs values.</param>
        /// <param name="cacheSize">
        ///     The size for the cache, measured in number of
        ///     rows from the <paramref name="inputs" /> set. Default is to use all
        ///     rows. In order to know how many rows can fit under a amount of memory,
        ///     use <see cref="GetNumberOfRowsForMaximumSizeInBytes(int)" />.
        /// </param>
        public KernelFunctionCache(IKernel kernel, double[][] inputs, int cacheSize)
            : base(kernel, inputs, cacheSize)
        {
        }

        /// <summary>
        ///     Gets the maximum number of rows that a cache can keep inside the given amount of bytes.
        ///     This value can be used to initialize SequentialMinimalOptimization's CacheSize property,
        ///     or be passed to <see cref="KernelFunctionCache" /> constructor.
        /// </summary>
        public static int GetNumberOfRowsForMaximumSizeInBytes(int bytes)
        {
            return (int)System.Math.Floor(System.Math.Sqrt(bytes / sizeof(double)));
        }

        /// <summary>
        ///     Gets the maximum number of rows that a cache can keep inside the given amount of bytes.
        ///     This value can be used to initialize SequentialMinimalOptimization's CacheSize property,
        ///     or be passed to <see cref="KernelFunctionCache" /> constructor.
        /// </summary>
        public static int GetNumberOfRowsForMaximumSizeInMegaBytes(double megaBytes)
        {
            return GetNumberOfRowsForMaximumSizeInBytes((int)System.Math.Ceiling(megaBytes * 1024 * 1024));
        }

        /// <summary>
        ///     Gets the maximum number of rows that a cache can keep inside the given amount of bytes.
        ///     This value can be used to initialize SequentialMinimalOptimization's CacheSize property,
        ///     or be passed to <see cref="KernelFunctionCache" /> constructor.
        /// </summary>
        public static int GetNumberOfRowsForMaximumSizeInGigaBytes(double gigaBytes)
        {
            return GetNumberOfRowsForMaximumSizeInMegaBytes((int)System.Math.Ceiling(gigaBytes * 1024));
        }
    }

    /// <summary>
    ///     Value cache for kernel function evaluations. The total memory size occupied by the
    ///     cache can fluctuate between <see cref="MinimumBytes" /> and <see cref="MaximumBytes" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class works as a least-recently-used cache for elements
    ///         computed from a the kernel (Gram) matrix. Elements which have
    ///         not been needed for some time are discarded from the cache;
    ///         while elements which are constantly requested remains cached.
    ///     </para>
    ///     <para>
    ///         The use of cache may speedup learning by a large factor; however
    ///         the actual speedup may vary according to the choice of cache size.
    ///     </para>
    ///     <para>
    ///         The total memory size occupied by the cache can fluctuate between
    ///         <see cref="KernelFunctionCache{TKernel, TInput}.MinimumBytes" />
    ///         and <see cref="KernelFunctionCache{TKernel, TInput}.MaximumBytes" />.
    ///     </para>
    /// </remarks>
    public class KernelFunctionCache<TKernel, TInput> : IDisposable
        where TKernel : IKernel<TInput>
    {
        private readonly Dictionary<int, double[]> rows;

        private double[] diagonal;

        private TInput[] inputs;
        private TKernel kernel;
        private LinkedList<int> lruIndices;
        private Dictionary<int, LinkedListNode<int>> lruIndicesLookupTable;

        private double[][] matrix;

        /// <summary>
        ///     Constructs a new <see cref="KernelFunctionCache" />.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="inputs">The inputs values.</param>
        public KernelFunctionCache(TKernel kernel, TInput[] inputs)
            : this(kernel, inputs, inputs.Length)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="KernelFunctionCache" />.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="inputs">The inputs values.</param>
        /// <param name="cacheSize">
        ///     The size for the cache, measured in number of
        ///     rows from the <paramref name="inputs" /> set. Default is to use all
        ///     rows. In order to know how many rows can fit under an amount of memory,
        ///     use <see cref="KernelFunctionCache.GetNumberOfRowsForMaximumSizeInBytes(int)" />.
        /// </param>
        public KernelFunctionCache(TKernel kernel, TInput[] inputs, int cacheSize)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            if (cacheSize < 0)
            {
                throw new ArgumentOutOfRangeException("cacheSize",
                    "The cache size must be non-negative.");
            }

            this.kernel = kernel;
            this.inputs = inputs;

            Size = cacheSize;
            if (Size > inputs.Length)
            {
                Size = inputs.Length;
            }


            if (cacheSize >= inputs.Length)
            {
                // Create whole cache.
                Trace.WriteLine(string.Format("Creating whole cache: {0} rows", inputs.Length));

                matrix = new double[inputs.Length][];
                for (var i = 0; i < inputs.Length; i++)
                {
                    matrix[i] = new double[i];
                    for (var j = 0; j < matrix[i].Length; j++)
                    {
                        matrix[i][j] = kernel.Function(inputs[i], inputs[j]);
                    }
                }

#if DEBUG
                var total = matrix.GetNumberOfElements();
                var expected = inputs.Length * (inputs.Length - 1) / 2;
                Debug.Assert(total == expected);
#endif
                Enabled = true;
            }

            else if (cacheSize > 0)
            {
                var collectionCapacity = (int)(1.1f * Size);

                // Create lookup tables
                lruIndices = new LinkedList<int>();
                lruIndicesLookupTable = new Dictionary<int, LinkedListNode<int>>(collectionCapacity);

                // Create cache for rows
                Trace.WriteLine(string.Format("Creating cache with capacity for {0} rows ({1} rows total)",
                    Size, inputs.Length));
                rows = new Dictionary<int, double[]>(collectionCapacity);

                Enabled = true;
            }
            else // cacheSize == 0
            {
                Trace.WriteLine("Cache disabled");
                Enabled = false; // Values will be computed on-the-fly
            }

            // Create cache for diagonal elements
            diagonal = new double[inputs.Length];
            for (var i = 0; i < inputs.Length; i++)
            {
                diagonal[i] = kernel.Function(inputs[i], inputs[i]);
            }
        }

        /// <summary>
        ///     Gets the size of the cache, measured in number of rows.
        /// </summary>
        /// <value>The size of this cache.</value>
        public int Size { get; }

        /// <summary>
        ///     Gets the current number of rows stored in this cache.
        /// </summary>
        public int Count => rows.Count;

        /// <summary>
        ///     Gets the maximum size of the cache, measured in bytes.
        /// </summary>
        public int MaximumBytes => Size * Size * sizeof(double);

        /// <summary>
        ///     Gets the minimum size of the cache, measured in bytes.
        /// </summary>
        public int MinimumBytes => Size * (Size - 1) / 2 * sizeof(double);

        /// <summary>
        ///     Gets the total number of cache hits.
        /// </summary>
        public int Hits { get; private set; }

        /// <summary>
        ///     Gets the total number of cache misses.
        /// </summary>
        public int Misses { get; private set; }

        /// <summary>
        ///     Gets the percentage of the cache currently in use.
        /// </summary>
        public double Usage
        {
            get
            {
                if (rows == null)
                {
                    return 0;
                }

                return rows.Count / (double)Size;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the cache is enabled. If the value
        ///     is false, it means the kernel function is being evaluated on-the-fly.
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        ///     Attempts to retrieve the value of the kernel function
        ///     from the diagonal of the kernel matrix. If the value
        ///     is not available, it is immediately computed and inserted
        ///     in the cache.
        /// </summary>
        /// <param name="i">Index of the point to compute.</param>
        /// <remarks>The result of the kernel function k(p[i], p[i]).</remarks>
        public double this[int i] => GetOrCompute(i);

        /// <summary>
        ///     Attempts to retrieve the kernel function evaluated between point at index i
        ///     and j. If it is not cached, it will be computed and the cache will be updated.
        /// </summary>
        /// <param name="i">The index of the first point <c>p</c> to compute.</param>
        /// <param name="j">The index of the second point <c>p</c> to compute.</param>
        /// <remarks>The result of the kernel function k(p[i], p[j]).</remarks>
        public double this[int i, int j] => GetOrCompute(i, j);

        /// <summary>
        ///     Attempts to retrieve the value of the kernel function
        ///     from the diagonal of the kernel matrix. If the value
        ///     is not available, it is immediately computed and inserted
        ///     in the cache.
        /// </summary>
        /// <param name="i">Index of the point to compute.</param>
        /// <remarks>The result of the kernel function k(p[i], p[i]).</remarks>
        public double GetOrCompute(int i)
        {
            return diagonal[i];
        }

        /// <summary>
        ///     Attempts to retrieve the kernel function evaluated between point at index i
        ///     and j. If it is not cached, it will be computed and the cache will be updated.
        /// </summary>
        /// <param name="i">The index of the first point <c>p</c> to compute.</param>
        /// <param name="j">The index of the second point <c>p</c> to compute.</param>
        /// <remarks>The result of the kernel function k(p[i], p[j]).</remarks>
        public double GetOrCompute(int i, int j)
        {
            if (i == j)
            {
                return diagonal[i];
            }

            if (matrix != null)
            {
                return j > i ? matrix[j][i] : matrix[i][j];
            }

            if (rows == null)
            {
                return kernel.Function(inputs[i], inputs[j]);
            }


            // Keys should always be given as in
            // the order (i, j) with i > j, so we
            // always have (higher{i}, lower{j})

            if (j > i)
            {
                var t = i;
                i = j;
                j = t;
            }


            var key = i;

            double[] value;

            // Check if the data is in the cache
            if (!rows.TryGetValue(key, out value))
            {
                // It is not. Compute the function and update

                // If we are over capacity,
                if (rows.Count >= Size)
                {
                    // The first entry must be removed to leave
                    // room for the previously computed value

                    var first = lruIndices.First;
                    var discardedKey = first.Value;

                    // Attempt to reuse memory by trying to use the same array that 
                    // is about to be discarded, in case it has enough capacity
                    value = rows[discardedKey];
                    if (value.Length < i)
                    {
                        var maxSize = System.Math.Max(10, System.Math.Min(2 * i, inputs.Length));
                        value = new double[maxSize];
                    }

                    // Remove the cached value for
                    // the least recently used key

                    rows.Remove(discardedKey);
                    lruIndicesLookupTable.Remove(discardedKey);

                    // Avoid allocating memory by reusing the
                    // previously first node to hold the new
                    // data value and re-insert it at the end

                    lruIndices.RemoveFirst();
                    first.Value = key;
                    lruIndices.AddLast(first);

                    // Update the index lookup table
                    lruIndicesLookupTable[key] = first;
                }
                else
                {
                    // Register the use of the variable in the LRU list
                    lruIndicesLookupTable[key] = lruIndices.AddLast(key);

                    var maxSize = System.Math.Max(10, System.Math.Min(2 * i, inputs.Length));
                    value = new double[maxSize]; // create a new array with enough size
                }

                for (var k = 0; k < i; k++)
                {
                    value[k] = kernel.Function(inputs[i], inputs[k]);
                }

                // Save evaluation
                rows[key] = value;

                Misses++;
            }
            else // The data was in the cache
            {
                // It is. Update the LRU list to indicate that the item has been used.
                var node = lruIndicesLookupTable[key];

                // Remove from middle and add to the end
                lruIndices.Remove(node);
                lruIndices.AddLast(node);

                // lruIndicesLookupTable[key] = node;

                Hits++;
            }

            return value[j];
        }

        /// <summary>
        ///     Clears the cache.
        /// </summary>
        public void Clear()
        {
            Trace.WriteLine("Clearing the cache.");

            if (rows != null)
            {
                rows.Clear();
            }

            if (lruIndices != null)
            {
                lruIndices.Clear();
            }

            if (lruIndicesLookupTable != null)
            {
                lruIndicesLookupTable.Clear();
            }
        }

        /// <summary>
        ///     Resets cache statistics.
        /// </summary>
        public void Reset()
        {
            Hits = 0;
            Misses = 0;
        }

        /// <summary>
        ///     Gets the key from the given indices.
        /// </summary>
        /// <param name="i">The index i.</param>
        /// <param name="j">The index j.</param>
        /// <returns>The key associated with the given indices.</returns>
        public int GetKeyFromIndex(int i, int j)
        {
            if (i < 0 || i >= inputs.Length)
            {
                throw new ArgumentOutOfRangeException("i");
            }

            if (j < 0 || j >= inputs.Length)
            {
                throw new ArgumentOutOfRangeException("j");
            }

            if (j > i)
            {
                i = j;
            }

            return i;
        }

        /// <summary>
        ///     Gets a copy of the data cache.
        /// </summary>
        /// <returns>A copy of the data cache.</returns>
        public IDictionary<Tuple<int, int>, double> GetDataCache()
        {
            var dict = new Dictionary<Tuple<int, int>, double>();

            if (rows == null)
            {
                foreach (var idx in matrix.GetIndices(true))
                {
                    dict.Add(Tuple.Create(idx[0], idx[1]), matrix[idx[0]][idx[1]]);
                }
            }
            else
            {
                foreach (var entry in rows)
                {
                    var i = entry.Key;
                    for (var j = 0; j < i; j++)
                    {
                        dict.Add(Tuple.Create(i, j), entry.Value[j]);
                    }
                }
            }

            return dict;
        }

        /// <summary>
        ///     Gets a copy of the Least Recently Used (LRU) List of
        ///     Kernel Matrix elements. Elements on the start of the
        ///     list have been used most; elements at the end are
        ///     about to be discarded from the cache.
        /// </summary>
        /// <returns>The Least Recently Used list of kernel matrix elements.</returns>
        public IList<Tuple<int, int>> GetLeastRecentlyUsedList()
        {
            if (lruIndices == null)
            {
                throw new InvalidOperationException("The cache is not using a LRU list.");
            }

            var list = new List<Tuple<int, int>>();
            foreach (var key in lruIndices)
            {
                for (var j = 0; j < key; j++)
                {
                    list.Add(Tuple.Create(key, j));
                }
            }

            return list;
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (rows != null)
                    {
                        rows.Clear();
                    }

                    if (lruIndices != null)
                    {
                        lruIndices.Clear();
                    }

                    if (lruIndicesLookupTable != null)
                    {
                        lruIndicesLookupTable.Clear();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                lruIndices = null;
                lruIndicesLookupTable = null;

                matrix = null;
                diagonal = null;
                inputs = null;

                disposedValue = true;
            }
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}