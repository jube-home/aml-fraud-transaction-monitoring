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

namespace Accord.Math.Optimization.Losses
{
    /// <summary>
    ///     Base class for <see cref="ILoss{T}">loss functions</see>.
    /// </summary>
    /// <typeparam name="TInput">The type for the expected data.</typeparam>
    /// <typeparam name="TScore">The type for the predicted score values.</typeparam>
    /// <typeparam name="TLoss">The type for the loss value. Default is double.</typeparam>
    [Serializable]
    public abstract class LossBase<TInput, TScore, TLoss> : ILoss<TScore, TLoss>
    {
        private TInput expected;

        /// <summary>
        ///     Gets the expected outputs (the ground truth).
        /// </summary>
        public TInput Expected
        {
            get => expected;
            protected set => expected = value;
        }

        /// <summary>
        ///     Computes the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public abstract TLoss Loss(TScore actual);
    }

    /// <summary>
    ///     Base class for <see cref="ILoss{T}">loss functions</see>.
    /// </summary>
    /// <typeparam name="T">The type for the expected data.</typeparam>
    [Serializable]
    public abstract class LossBase<T> : LossBase<T, T, double>, ILoss<T>
    {
    }
}