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

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Training-Validation-Testing data split.
    /// </summary>
    /// <typeparam name="TInput">The type of the input being partitioned into splits.</typeparam>
    /// <typeparam name="TOutput">The type of the output being partitioned into splits.</typeparam>
    public class TrainValTestDataSplit<TInput, TOutput> : TrainValTestSplit<DataSubset<TInput, TOutput>>,
        IDataSplit<TInput, TOutput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TrainValTestDataSplit{TInput, TOutput}" /> class.
        /// </summary>
        public TrainValTestDataSplit()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TrainValTestDataSplit{TInput, TOutput}" /> class.
        /// </summary>
        /// <param name="index">The index associated with this subset, if any.</param>
        /// <param name="inputs">The input instances in this subset.</param>
        /// <param name="outputs">The output instances in this subset.</param>
        /// <param name="weights">The weights associated with the input instances.</param>
        /// <param name="trainIndices">The indices of the training instances in relation to the original dataset.</param>
        /// <param name="validationIndices">The indices of the validation instances in relation to the original dataset.</param>
        /// <param name="testingIndices">The indices of the testing instances in relation to the original dataset.</param>
        public TrainValTestDataSplit(int index, TInput[] inputs, TOutput[] outputs, double[] weights,
            int[] trainIndices, int[] validationIndices, int[] testingIndices)
        {
            SplitIndex = index;
            Training = new DataSubset<TInput, TOutput>(index, inputs, outputs, weights, trainIndices);
            Validation = new DataSubset<TInput, TOutput>(index, inputs, outputs, weights, validationIndices);
            Testing = new DataSubset<TInput, TOutput>(index, inputs, outputs, weights, testingIndices);
        }

        /// <summary>
        ///     Gets or sets the index of the split in relation to the original dataset, if applicable.
        /// </summary>
        public int SplitIndex { get; set; }
    }
}