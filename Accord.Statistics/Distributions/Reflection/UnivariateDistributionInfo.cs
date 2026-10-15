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
using System.Linq;

namespace Accord.Statistics.Distributions.Reflection
{
    /// <summary>
    ///     Discovers the parameters of a
    ///     <see cref="IUnivariateDistribution">
    ///         univariate probability
    ///         distribution
    ///     </see>
    ///     and helps determine their range and whether then can be constructed
    ///     automatically from their indicated parameter ranges.
    /// </summary>
    public class UnivariateDistributionInfo : DistributionInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UnivariateDistributionInfo" /> class.
        /// </summary>
        /// <param name="type">The type for the distribution.</param>
        public UnivariateDistributionInfo(Type type)
            : base(type)
        {
        }

        /// <summary>
        ///     Gets an array containing all univariate distributions
        ///     by inspecting Accord.NET assemblies using reflection.
        /// </summary>
        public static IEnumerable<UnivariateDistributionInfo> GetUnivariateDistributions(bool buildableOnly = false)
        {
            var distributions = GetDistributionsInheritingFromBaseType(typeof(IUnivariateDistribution))
                .Select(p => new UnivariateDistributionInfo(p));

            if (buildableOnly)
            {
                distributions = distributions.Where(d => d.IsBuildable);
            }

            return distributions;
        }

        /// <summary>
        ///     Creates a new instance of the distribution using the given arguments.
        /// </summary>
        /// <param name="arguments">The arguments to be passed to the distribution's constructor.</param>
        public IUnivariateDistribution CreateInstance(Dictionary<DistributionParameterInfo, object> arguments)
        {
            var parameters = new object[arguments.Count];
            foreach (var p in arguments)
            {
                var paramType = p.Key.ParameterInfo.ParameterType;
#if NETSTANDARD
                if (paramType.GetTypeInfo().IsEnum)
#else
                if (paramType.IsEnum)
#endif
                {
                    var i = (int)Convert.ChangeType(p.Value, typeof(int));
                    var v = Enum.ToObject(paramType, i);
                    parameters[p.Key.Position] = v;
                }
                else
                {
                    parameters[p.Key.Position] = Convert.ChangeType(p.Value, paramType);
                }
            }

            var distribution =
                (IUnivariateDistribution)Activator.CreateInstance(DistributionType, parameters);

            return distribution;
        }

        /// <summary>
        ///     Creates a new instance of the distribution using default arguments,
        ///     if the distribution declared them using parameter attributes.
        /// </summary>
        public static T CreateInstance<T>()
        {
            var info = new UnivariateDistributionInfo(typeof(T));

            var ctor = info.GetConstructors()
                .Where(x => x.IsBuildable)
                .OrderBy(x => x.GetParameters().Length)
                .First();

            // Build the argument list
            var arguments = new Dictionary<DistributionParameterInfo, object>();

            // Select the minimum value for the parameters
            foreach (var parameter in ctor.GetParameters())
            {
                arguments[parameter] = parameter.DefaultValue;
            }

            return (T)info.CreateInstance(arguments);
        }
    }
}