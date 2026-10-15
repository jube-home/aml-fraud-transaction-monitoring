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

using Accord.Math;

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Subset of a larger dataset.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data in this dataset.</typeparam>
    public class DataSubset<TInput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DataSubset{TInput}" /> class.
        /// </summary>
        public DataSubset()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataSubset{TInput}" /> class.
        /// </summary>
        /// <param name="subsetSize">The size of the data subset.</param>
        /// <param name="totalSize">The total size of the dataset that contains this subset.</param>
        public DataSubset(int subsetSize, int totalSize)
        {
            Index = 0;
            Proportion = subsetSize / (double)totalSize;
            Indices = new int[subsetSize];
            Inputs = new TInput[subsetSize];
            Weights = Vector.Ones(subsetSize);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataSubset{TInput}" /> class.
        /// </summary>
        /// <param name="index">The index associated with this subset, if any.</param>
        /// <param name="inputs">The input instances in this subset.</param>
        /// <param name="weights">The weights associated with the input instances.</param>
        /// <param name="indices">The indices of the input instances in relation to the original dataset.</param>
        public DataSubset(int index, TInput[] inputs, double[] weights, int[] indices)
        {
            Index = index;
            Proportion = indices.Length / (double)inputs.Length;
            Indices = indices;
            Inputs = inputs.Get(indices);
            Weights = weights != null ? weights.Get(indices) : null;
        }

        /// <summary>
        ///     Gets or sets the input data in the dataset.
        /// </summary>
        public TInput[] Inputs { get; set; }

        /// <summary>
        ///     Gets or sets the weights associated with each input sample in the dataset.
        /// </summary>
        public double[] Weights { get; set; }

        /// <summary>
        ///     Gets or sets the indices of the samples of this subset
        ///     in relation to the original dataset they belong to.
        /// </summary>
        public int[] Indices { get; set; }

        /// <summary>
        ///     Gets or sets a user-defined tag that can be associated with this instance.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        ///     Gets or sets the size of this subset as a proportion in
        ///     relation to the original dataset this subset comes from.
        /// </summary>
        public double Proportion { get; set; }

        /// <summary>
        ///     Gets or sets an index associated with this subset, if applicable.
        /// </summary>
        public int Index { get; set; }
    }

    /// <summary>
    ///     Subset of a larger dataset.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data in this dataset.</typeparam>
    /// <typeparam name="TOutput"> The type of the output data in this dataset.</typeparam>
    public class DataSubset<TInput, TOutput>
        : DataSubset<TInput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DataSubset{TInput, TOutput}" /> class.
        /// </summary>
        public DataSubset()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataSubset{TInput, TOutput}" /> class.
        /// </summary>
        public DataSubset(int subsetSize, int totalSize)
            : base(subsetSize, totalSize)
        {
            Outputs = new TOutput[subsetSize];
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataSubset{TInput, TOutput}" /> class.
        /// </summary>
        /// <param name="index">The index associated with this subset, if any.</param>
        /// <param name="inputs">The input instances in this subset.</param>
        /// <param name="outputs">The output instances in this subset.</param>
        /// <param name="weights">The weights associated with the input instances.</param>
        /// <param name="indices">The indices of the input instances in relation to the original dataset.</param>
        public DataSubset(int index, TInput[] inputs, TOutput[] outputs, double[] weights, int[] indices)
            : base(index, inputs, weights, indices)
        {
            Outputs = outputs.Get(indices);
        }

        /// <summary>
        ///     Gets or sets the input data in the dataset.
        /// </summary>
        public TOutput[] Outputs { get; set; }
    }
}