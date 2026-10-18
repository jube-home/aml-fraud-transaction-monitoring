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
using Accord.Statistics.Distributions.Univariate;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Codebook learning statistics for <see cref="BagOfWords" /> models.
    /// </summary>
    [Serializable]
    public class BagOfWordsStatistics
    {
        /// <summary>
        ///     Gets or sets the number of instances (i.e. images or audio signals) in the training set.
        /// </summary>
        /// <value>The number of instances (i.e. images or audio signals).</value>
        public int TotalNumberOfInstances { get; set; }

        /// <summary>
        ///     Gets or sets the total number of descriptors seen in the training set.
        /// </summary>
        /// <value>The total number of descriptors.</value>
        public int TotalNumberOfDescriptors { get; set; }

        /// <summary>
        ///     Gets or sets the count distribution of the descriptors seen in the training set.
        /// </summary>
        public NormalDistribution TotalNumberOfDescriptorsPerInstance { get; set; }

        /// <summary>
        ///     Gets or sets the minimum and maximum number of descriptors per instance seen in the training set.
        /// </summary>
        public IntRange TotalNumberOfDescriptorsPerInstanceRange { get; set; }

        /// <summary>
        ///     Gets or sets the number of instances (i.e. images or audio signals)
        ///     actually used in the learning of the <see cref="BagOfWords" />.
        /// </summary>
        /// <value>The number of instances.</value>
        public int NumberOfInstancesTaken { get; set; }

        /// <summary>
        ///     Gets or sets the number of descriptors actually used
        ///     in the learning of the <see cref="BagOfWords" />.
        /// </summary>
        /// <value>The total number of descriptors.</value>
        public int NumberOfDescriptorsTaken { get; set; }

        /// <summary>
        ///     Gets or sets the count distribution of the descriptors actually
        ///     used in the learning of the <see cref="BagOfWords" />.
        /// </summary>
        public NormalDistribution NumberOfDescriptorsTakenPerInstance { get; set; }

        /// <summary>
        ///     Gets or sets the minimum and maximum number of descriptors per instance
        ///     actually used in the learning of the <see cref="BagOfWords" />.
        /// </summary>
        public IntRange NumberOfDescriptorsTakenPerInstanceRange { get; set; }
    }
}