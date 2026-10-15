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
using System.Reflection;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions.Reflection
{
    /// <summary>
    ///     Discovers the parameters of a <see cref="IDistribution">probability distribution</see>
    ///     and helps determine their range and whether then can be constructed automatically from
    ///     their indicated parameter ranges.
    /// </summary>
    public class DistributionInfo
    {
        private bool? isBuildable;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributionInfo" /> class.
        /// </summary>
        /// <param name="type">The type for the distribution.</param>
        public DistributionInfo(Type type)
        {
            var baseType = typeof(IDistribution);
#if NETSTANDARD
            var typeDesc = type.GetTypeInfo();
            if (!baseType.GetTypeInfo().IsAssignableFrom(typeDesc) || typeDesc.IsAbstract || typeDesc.IsInterface)
                throw new ArgumentException("The type must be a concrete class implementing IDistribution.", "type");
#else
            if (!baseType.IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
            {
                throw new ArgumentException("The type must be a concrete class implementing IDistribution.", "type");
            }
#endif
            DistributionType = type;
            Name = GetDistributionName(type);
        }

        /// <summary>
        ///     Gets the distribution's type information.
        /// </summary>
        public Type DistributionType { get; protected set; }

        /// <summary>
        ///     Gets the name of this distribution in a more human-readable form.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        ///     Gets a value indicating whether it is possible to discover enough
        ///     information about this constructor such that the distribution can
        ///     be constructed using reflection.
        /// </summary>
        /// <value><c>true</c> if this instance is buildable; otherwise, <c>false</c>.</value>
        public bool IsBuildable
        {
            get
            {
                if (isBuildable == null)
                {
                    isBuildable = GetConstructors().Any(x => x.IsBuildable);
                }

                return isBuildable.Value;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the distribution modeled by <see cref="DistributionType" /> is a discrete-valued
        ///     distribution.
        ///     Discrete distributions are assumed to inherit from <see cref="UnivariateDiscreteDistribution" /> or
        ///     <see cref="MultivariateDiscreteDistribution" />.
        /// </summary>
        public bool IsDiscrete
        {
            get
            {
#if NETSTANDARD
                var distributionTypeInfo = DistributionType.GetTypeInfo();
                return typeof(UnivariateDiscreteDistribution).GetTypeInfo().IsAssignableFrom(distributionTypeInfo)
                  || typeof(MultivariateDiscreteDistribution).GetTypeInfo().IsAssignableFrom(distributionTypeInfo);
#else
                return typeof(UnivariateDiscreteDistribution).IsAssignableFrom(DistributionType)
                       || typeof(MultivariateDiscreteDistribution).IsAssignableFrom(DistributionType);
#endif
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the distribution modeled by <see cref="DistributionType" /> is a continuous-valued
        ///     distribution.
        ///     Discrete distributions are assumed to inherit from <see cref="UnivariateContinuousDistribution" /> or
        ///     <see cref="MultivariateContinuousDistribution" />.
        /// </summary>
        public bool IsContinuous
        {
            get
            {
#if NETSTANDARD
                var distributionTypeInfo = DistributionType.GetTypeInfo();
                return typeof(UnivariateContinuousDistribution).GetTypeInfo().IsAssignableFrom(distributionTypeInfo)
                  || typeof(MultivariateContinuousDistribution).GetTypeInfo().IsAssignableFrom(distributionTypeInfo);
#else
                return typeof(UnivariateContinuousDistribution).IsAssignableFrom(DistributionType)
                       || typeof(MultivariateContinuousDistribution).IsAssignableFrom(DistributionType);
#endif
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the distribution modeled by <see cref="DistributionType" /> is univariate.
        ///     A distribution is assumed to be univariate if it implements the <see cref="IUnivariateDistribution" /> interface.
        /// </summary>
        public bool IsUnivariate
        {
#if NETSTANDARD
            get { return typeof(IUnivariateDistribution).GetTypeInfo().IsAssignableFrom(DistributionType.GetTypeInfo()); }
#else
            get { return typeof(IUnivariateDistribution).IsAssignableFrom(DistributionType); }
#endif
        }

        /// <summary>
        ///     Gets a value indicating whether the distribution modeled by <see cref="DistributionType" /> is multivariate.
        ///     A distribution is assumed to be univariate if it implements the <see cref="IMultivariateDistribution" /> interface.
        /// </summary>
        public bool IsMultivariate
        {
#if NETSTANDARD
            get { return typeof(IMultivariateDistribution).GetTypeInfo().IsAssignableFrom(DistributionType.GetTypeInfo()); }
#else
            get { return typeof(IMultivariateDistribution).IsAssignableFrom(DistributionType); }
#endif
        }

        /// <summary>
        ///     Gets the public constructors of the distribution.
        /// </summary>
        public DistributionConstructorInfo[] GetConstructors()
        {
#if NETSTANDARD
            var constructors = new List<DistributionConstructorInfo>();
            var typeInfo = DistributionType.GetTypeInfo();
            var declaredConstructors = typeInfo.DeclaredConstructors;
            foreach (ConstructorInfo ctor in declaredConstructors)
                if (ctor.IsPublic)
                    constructors.Add(new DistributionConstructorInfo(ctor));
#else
            var constructors = new List<DistributionConstructorInfo>();
            foreach (var ctor in DistributionType.GetConstructors())
            {
                constructors.Add(new DistributionConstructorInfo(ctor));
            }
#endif

            return constructors.ToArray();
        }

        /// <summary>
        ///     Gets the fitting options object that are expected by the distribution, if any. An
        ///     Accord.NET distribution object can be fitted to a set of observed values. However,
        ///     some distributions have different settings on how this fitting can be done. This
        ///     function creates an object that contains those possible settings that can be configured
        ///     for a given distribution type.
        /// </summary>
        public IFittingOptions GetFittingOptions()
        {
            return GetFittingOptions(DistributionType);
        }

        /// <summary>
        ///     Gets the name of the distribution modeled by a given Accord.NET type.
        ///     The name is returned in a normalized form (i.e. given a type whose name
        ///     is  NormalDistribution, the function would return "Normal").
        /// </summary>
        public static string GetDistributionName(Type type)
        {
            // Extract the real distribution name from the class name
            var name = type.Name;

            if (name.Contains('`'))
            {
                name = name.Remove(name.IndexOf("`", StringComparison.Ordinal));
            }

            // Remove the trailing "Distribution" from the name
            if (name.EndsWith("Distribution"))
            {
                name = name.Remove(name.IndexOf("Distribution", StringComparison.Ordinal));
            }

            return name.Trim();
        }

        /// <summary>
        ///     Gets the fitting options object that are expected by one distribution, if any. An
        ///     Accord.NET distribution object can be fit to a set of observed values. However,
        ///     some distributions have different settings on how this fitting can be done. This
        ///     function creates an object that contains those possible settings that can be configured
        ///     for a given distribution type.
        /// </summary>
        public static IFittingOptions GetFittingOptions(Type type)
        {
            // Try to create a fitting options object
            var fittingOptions = typeof(IFittingOptions);
#if NETSTANDARD
            var interfaces = type.GetTypeInfo().ImplementedInterfaces.Select(t => t.GetTypeInfo())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFittableDistribution<,>));

            var fittingOptionsInfo = fittingOptions.GetTypeInfo();
            foreach (TypeInfo i in interfaces)
                foreach (Type arg in i.GenericTypeArguments)
                    if (fittingOptionsInfo.IsAssignableFrom(arg.GetTypeInfo()) && arg != fittingOptions)
                        return (IFittingOptions)Activator.CreateInstance(arg);
#else
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFittableDistribution<,>));

            foreach (var i in interfaces)
            {
                foreach (var arg in i.GetGenericArguments())
                {
                    if (fittingOptions.IsAssignableFrom(arg) && arg != fittingOptions)
                    {
                        return (IFittingOptions)Activator.CreateInstance(arg);
                    }
                }
            }
#endif
            return null;
        }

        /// <summary>
        ///     Gets the fitting options object that are expected by one distribution, if any. An
        ///     Accord.NET distribution object can be fit to a set of observed values. However,
        ///     some distributions have different settings on how this fitting can be done. This
        ///     function creates an object that contains those possible settings that can be configured
        ///     for a given distribution type.
        /// </summary>
        public static IFittingOptions GetFittingOptions<T>()
        {
            return GetFittingOptions(typeof(T));
        }

        internal static Type[] GetDistributionsInheritingFromBaseType(Type baseType)
        {
            // This function iterates the Accord.Statistics assembly looking for
            // classes that are concrete (not abstract) and that implement the
            // given interface. 

            // Get all univariate distributions in Accord.NET:
#if NETSTANDARD1_4
            var baseInfo = baseType.GetTypeInfo();
            var distributions = baseInfo.Assembly.DefinedTypes
                .Where(p => baseInfo.IsAssignableFrom(p) && !p.IsAbstract && !p.IsInterface)
                .Select(p => p.AsType());
#else
            var distributions = Assembly.GetAssembly(baseType).GetTypes().Where(p =>
                baseType.IsAssignableFrom(p) && !p.IsAbstract && !p.IsInterface);
#endif

            return distributions.ToArray();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}