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

namespace Accord.Statistics.Running
{
    /// <summary>
    ///     Running (range) statistics.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class computes the running minimum and maximum values in a stream of values.
    ///         Running statistics need only one pass over the data, and do not require all data
    ///         to be available prior to computing.
    ///     </para>
    /// </remarks>
    [Serializable]
    public class RunningRangeStatistics : IRunning<double>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RunningRangeStatistics" /> class.
        /// </summary>
        public RunningRangeStatistics()
        {
        }

        /// <summary>
        ///     Gets the number of samples seen.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        ///     Gets the minimum value seen.
        /// </summary>
        public double Min { get; private set; }

        /// <summary>
        ///     Gets the maximum value seen.
        /// </summary>
        public double Max { get; private set; }

        /// <summary>
        ///     Registers the occurrence of a value.
        /// </summary>
        /// <param name="value">The value to be registered.</param>
        public void Push(double value)
        {
            Count++;

            if (Count == 1)
            {
                Min = Max = value;
            }
            else
            {
                if (value > Max)
                {
                    Max = value;
                }

                if (value < Min)
                {
                    Min = value;
                }
            }
        }

        /// <summary>
        ///     Clears all measures previously computed.
        /// </summary>
        public void Clear()
        {
            Min = Max = Count = 0;
        }
    }
}