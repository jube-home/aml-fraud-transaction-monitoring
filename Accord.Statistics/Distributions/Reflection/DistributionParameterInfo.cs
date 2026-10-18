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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions.Reflection
{
    /// <summary>
    ///     Discovers the parameters of a <see cref="IDistribution">probability distribution</see>
    ///     and helps determine their range and whether then can be constructed automatically from
    ///     their indicated parameter ranges.
    /// </summary>
    /// <seealso cref="DistributionConstructorInfo" />
    public class DistributionParameterInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributionParameterInfo" /> class.
        /// </summary>
        /// <param name="parameterInfo">The parameter information.</param>
        public DistributionParameterInfo(ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;
            IsBuildable = true;

            DoubleRange range;
            if (!TryGetRange(parameterInfo, out range))
            {
                IsBuildable = false;
            }

            double value;
            if (!TryGetDefault(parameterInfo, out value))
            {
                IsBuildable = false;
            }

            Range = range;
            DefaultValue = value;
        }

        /// <summary>
        ///     Gets the reflection parameter information.
        /// </summary>
        public ParameterInfo ParameterInfo { get; }

        /// <summary>
        ///     Gets the name of this parameter.
        /// </summary>
        public string Name => ParameterInfo.Name;

        /// <summary>
        ///     Gets the position of this parameter in the declaration of the constructor.
        /// </summary>
        public int Position => ParameterInfo.Position;

        /// <summary>
        ///     Gets the range of valid values for this parameter (i.e. in a <see cref="NormalDistribution" />,
        ///     the standard deviation parameter cannot be negative).
        /// </summary>
        public DoubleRange Range { get; }

        /// <summary>
        ///     Gets the default value for this parameter (i.e. in a <see cref="NormalDistribution" />,
        ///     the default value for the mean is 0).
        /// </summary>
        public double DefaultValue { get; }

        /// <summary>
        ///     Gets a value indicating whether it is possible to discover enough
        ///     information about this constructor such that the distribution can
        ///     be constructed using reflection.
        /// </summary>
        /// <value><c>true</c> if this instance is buildable; otherwise, <c>false</c>.</value>
        public bool IsBuildable { get; private set; }

        /// <summary>
        ///     Tries to get the valid range of a distribution's parameter.
        /// </summary>
        public static bool TryGetRange(ParameterInfo parameter, out DoubleRange range)
        {
            range = new DoubleRange(0, 0);

            var attrb = parameter.GetCustomAttribute<RangeAttribute>();
            if (attrb == null)
            {
#if NETSTANDARD
                if (!parameter.ParameterType.GetTypeInfo().IsEnum)
#else
                if (!parameter.ParameterType.IsEnum)
#endif
                {
                    return false;
                }

                var enumValues = Enum.GetValues(parameter.ParameterType);
                var intValues = new int[enumValues.Length];
                for (var i = 0; i < intValues.Length; i++)
                {
                    intValues[i] = (int)enumValues.GetValue(i);
                }

                Array.Sort(intValues);
                range = new DoubleRange(intValues[0], intValues[intValues.Length - 1]);
                return true;
            }

            var min = (double)Convert.ChangeType(attrb.Minimum, typeof(double));
            var max = (double)Convert.ChangeType(attrb.Maximum, typeof(double));

            range = new DoubleRange(min, max);

            return true;
        }

        /// <summary>
        ///     Tries to get the default value of a distribution's parameter.
        /// </summary>
        public static bool TryGetDefault(ParameterInfo parameter, out double value)
        {
            var attrb = parameter.GetCustomAttribute<DefaultValueAttribute>();

            if (attrb != null)
            {
                value = (double)Convert.ChangeType(attrb.Value, typeof(double));
                return true;
            }

            DoubleRange range;
            if (!TryGetRange(parameter, out range))
            {
                value = 0;
                return false;
            }

            var a = parameter.GetCustomAttribute<RangeAttribute>();

            value = 0;

            if (a is PositiveAttribute || a is PositiveIntegerAttribute)
            {
                value = 1;
            }

            else if (a is NegativeAttribute || a is NegativeIntegerAttribute)
            {
                value = -1;
            }

            else if (a is UnitAttribute)
            {
                value = 0.5;
            }


            if (value < range.Min)
            {
                value = range.Min;
            }

            if (value > range.Max)
            {
                value = range.Max;
            }

            return true;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("{0} ({1}-{2}, default: {3})", Name, Range.Min, Range.Max, DefaultValue);
        }
    }
}