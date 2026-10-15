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

namespace Accord.Math.Integration
{
    /// <summary>
    ///     Common interface for numeric integration methods.
    /// </summary>
    public interface INumericalIntegration : ICloneable
    {
        /// <summary>
        ///     Gets the numerically computed result of the
        ///     definite integral for the specified function.
        /// </summary>
        double Area { get; }

        /// <summary>
        ///     Computes the area of the function under the selected
        ///     range. The computed value will be available at this
        ///     class's <see cref="Area" /> property.
        /// </summary>
        /// <returns>True if the integration method succeeds, false otherwise.</returns>
        bool Compute();
    }

    /// <summary>
    ///     Common interface for numeric integration methods.
    /// </summary>
    public interface INumericalIntegration<TCode> : INumericalIntegration
        where TCode : struct
    {
        /// <summary>
        ///     Get the exit code returned in the last call to the
        ///     <see cref="INumericalIntegration.Compute()" /> method.
        /// </summary>
        TCode Status { get; }
    }
}