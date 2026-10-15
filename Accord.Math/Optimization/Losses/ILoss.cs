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

namespace Accord.Math.Optimization.Losses
{
    /// <summary>
    ///     Common interface for loss functions, such as <see cref="SquareLoss" />,
    ///     <see cref="HingeLoss" />, <see cref="CategoryCrossEntropyLoss" /> and
    ///     <see cref="BinaryCrossEntropyLoss" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In mathematical optimization, statistics, decision theory and machine learning, a loss
    ///         function or cost function is a function that maps an event or values of one or more
    ///         variables onto a real number intuitively representing some "cost" associated with the
    ///         event. An optimization problem seeks to minimize a loss function. An objective function
    ///         is either a loss function or its negative (sometimes called a reward function, a profit
    ///         function, a utility function, a fitness function, etc.), in which case it is to be
    ///         maximized.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Loss_function">
    ///                         Wikipedia contributors. "Loss function." Wikipedia, The Free Encyclopedia.
    ///                         Wikipedia, The Free Encyclopedia, 18 Mar. 2016. Web.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="TScore">The type for the predicted score values.</typeparam>
    /// <typeparam name="TLoss">The type for the loss value. Default is double.</typeparam>
    public interface ILoss<TScore, TLoss>
    {
        /// <summary>
        ///     Computes the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        TLoss Loss(TScore actual);
    }

    /// <summary>
    ///     Common interface for differentiable loss functions, such as <see cref="SquareLoss" />,
    ///     <see cref="HingeLoss" />, <see cref="CategoryCrossEntropyLoss" /> and
    ///     <see cref="BinaryCrossEntropyLoss" />.
    /// </summary>
    public interface IDifferentiableLoss<TInput, TScore, TLoss>
    {
        /// <summary>
        ///     Computes the derivative of the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <param name="expected">The expected values that should have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        TLoss Loss(TInput expected, TScore actual);

        /// <summary>
        ///     Computes the derivative of the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <param name="expected">The expected values that should have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        TLoss Derivative(TInput expected, TScore actual);
    }

    /// <summary>
    ///     Common interface for loss functions, such as
    ///     <see cref="SquareLoss" />, <see cref="HingeLoss" /> and
    ///     <see cref="CategoryCrossEntropyLoss" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In mathematical optimization, statistics, decision theory and machine learning, a loss
    ///         function or cost function is a function that maps an event or values of one or more
    ///         variables onto a real number intuitively representing some "cost" associated with the
    ///         event. An optimization problem seeks to minimize a loss function. An objective function
    ///         is either a loss function or its negative (sometimes called a reward function, a profit
    ///         function, a utility function, a fitness function, etc.), in which case it is to be
    ///         maximized.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Loss_function">
    ///                         Wikipedia contributors. "Loss function." Wikipedia, The Free Encyclopedia.
    ///                         Wikipedia, The Free Encyclopedia, 18 Mar. 2016. Web.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type for the expected data.</typeparam>
    public interface ILoss<T> : ILoss<T, double>
    {
    }
}