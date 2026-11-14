/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using FluentAssertions;
using Jube.Engine.Helpers;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class AnchoredAssemblySerializationBinderTests
    {
        private static readonly JsonSerializationHelper helper = new();

        [Fact]
        public void ATypeOutsideTheAnchoredAssemblyIsNeverInstantiatedFromAStoredNetwork()
        {
            var network = new ActivationNetwork(new SigmoidFunction(1), 2, null, 2, 1);
            var json = JsonConvert.SerializeObject(network, helper.TopologyNetworkJsonSerializerSettings);

            var stored = typeof(SigmoidFunction).FullName + ", " + typeof(SigmoidFunction).Assembly.GetName().Name;
            var gadget = typeof(HostileActivationFunction).AssemblyQualifiedName;
            var tampered = json.Replace(stored, gadget);

            tampered.Should().NotBe(json, "the serialised network must carry the type name for the test to bite");

            HostileActivationFunction.Constructed = 0;
            var errors = new List<string>();
            var settings = helper.DeserializeTopologyNetworkJsonSerializerSettings;
            settings.Error += (_, args) => errors.Add(Describe(args.ErrorContext.Error));

            JsonConvert.DeserializeObject<ActivationNetwork>(tampered, settings);

            HostileActivationFunction.Constructed.Should().Be(0);
            errors.Should().Contain(e => e.Contains("not permitted"));
        }

        private static string Describe(Exception error)
        {
            return error.InnerException == null ? error.Message : error.Message + " <- " + Describe(error.InnerException);
        }

        [Theory]
        [InlineData("System.Diagnostics.Process, System.Diagnostics.Process")]
        [InlineData("System.IO.FileInfo, System.Private.CoreLib")]
        [InlineData("System.Collections.Generic.List`1[[System.Diagnostics.Process, System.Diagnostics.Process]], System.Private.CoreLib")]
        public void WellKnownGadgetTypesAreRefusedByTheBinder(string typeName)
        {
            var binder = new AnchoredAssemblySerializationBinder(typeof(ActivationNetwork));
            var split = typeName.LastIndexOf(", ", StringComparison.Ordinal);

            Action act = () => binder.BindToType(typeName[(split + 2)..], typeName[..split]);

            act.Should().Throw<JsonSerializationException>();
        }

        [Fact]
        public void TheTypesOfTheAnchoredAssemblyArePermitted()
        {
            var binder = new AnchoredAssemblySerializationBinder(typeof(ActivationNetwork));

            binder.BindToType(typeof(SigmoidFunction).Assembly.GetName().Name, typeof(SigmoidFunction).FullName)
                .Should().Be(typeof(SigmoidFunction));
        }

        [Fact]
        public void ABinderWithNoAnchorPermitsOnlyPrimitivesAndStrings()
        {
            var binder = new AnchoredAssemblySerializationBinder();

            binder.BindToType("System.Private.CoreLib", "System.String").Should().Be(typeof(string));
            binder.BindToType("System.Private.CoreLib", "System.Double[]").Should().Be(typeof(double[]));

            Action act = () => binder.BindToType(typeof(SigmoidFunction).Assembly.GetName().Name,
                typeof(SigmoidFunction).FullName);

            act.Should().Throw<JsonSerializationException>();
        }

        public sealed class HostileActivationFunction : IActivationFunction
        {
            public static int Constructed;

            public HostileActivationFunction()
            {
                Interlocked.Increment(ref Constructed);
            }

            public double Function(double x)
            {
                return x;
            }

            public double Derivative(double x)
            {
                return 1;
            }

            public double Derivative2(double y)
            {
                return 1;
            }
        }
    }
}
