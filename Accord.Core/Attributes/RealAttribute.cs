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
using System.ComponentModel.DataAnnotations;

namespace Accord
{
    /// <summary>
    ///     Specifies that an argument, in a method or function,
    ///     must be greater than zero.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class PositiveAttribute : RealAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PositiveAttribute" /> class.
        /// </summary>
        public PositiveAttribute(double minimum = double.Epsilon, double maximum = double.MaxValue)
            : base(minimum, maximum)
        {
            if (minimum <= 0)
            {
                throw new ArgumentOutOfRangeException("minimum");
            }
        }
    }

    /// <summary>
    ///     Specifies that an argument, in a method or function,
    ///     must be lesser than zero.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class NegativeAttribute : RealAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NegativeAttribute" /> class.
        /// </summary>
        public NegativeAttribute(double minimum = double.MinValue, double maximum = -double.Epsilon)
            : base(minimum, maximum)
        {
            if (maximum >= 0)
            {
                throw new ArgumentOutOfRangeException("maximum");
            }
        }
    }

    /// <summary>
    ///     Specifies that an argument, in a method or function,
    ///     must be lesser than or equal to zero.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class NonpositiveAttribute : RealAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NonpositiveAttribute" /> class.
        /// </summary>
        public NonpositiveAttribute(double minimum = double.MinValue, double maximum = 0)
            : base(minimum, maximum)
        {
            if (maximum < 0)
            {
                throw new ArgumentOutOfRangeException("maximum");
            }
        }
    }

    /// <summary>
    ///     Specifies that an argument, in a method or function,
    ///     must be greater than or equal to zero.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class NonnegativeAttribute : RangeAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NonnegativeAttribute" /> class.
        /// </summary>
        public NonnegativeAttribute(double minimum = 0, double maximum = double.MaxValue)
            : base(minimum, maximum)
        {
            if (minimum < 0)
            {
                throw new ArgumentOutOfRangeException("minimum");
            }

            if (maximum < 0)
            {
                throw new ArgumentOutOfRangeException("maximum");
            }
        }
    }

    /// <summary>
    ///     Specifies that an argument, in a method or function,
    ///     must be real (double).
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RealAttribute : RangeAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RealAttribute" /> class.
        /// </summary>
        public RealAttribute(double minimum = double.MinValue, double maximum = double.MaxValue)
            : base(minimum, maximum)
        {
        }

        /// <summary>
        ///     Gets the minimum allowed field value.
        /// </summary>
        public new double Minimum => (double)base.Minimum;

        /// <summary>
        ///     Gets the maximum allowed field value.
        /// </summary>
        public new double Maximum => (double)base.Maximum;
    }

    /// <summary>
    ///     Specifies that an argument, in a method or function,
    ///     must be real between 0 and 1.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class UnitAttribute : RangeAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UnitAttribute" /> class.
        /// </summary>
        public UnitAttribute()
            : base(0, 1)
        {
        }
    }
}