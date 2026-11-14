/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Analytics.Aggregation;
using Jube.Dto.Options;
using Xunit;

namespace Jube.Test.Analytics.Aggregation
{
    [Trait("Category", "Unit")]
    public sealed class AggregationCatalogueTests
    {
        private const int FirstCatalogueId = 17;

        [Fact]
        public void EveryCatalogueFunctionIsASearchFunctionTypeOfTheSameNameAndTheReverse()
        {
            var catalogue = AggregationCatalogue.Entries.ToDictionary(e => e.Id, e => e.Name);

            var enumeration = System.Enum.GetValues<SearchFunctionType>()
                .Where(v => (int)v >= FirstCatalogueId && v != SearchFunctionType.CursorResult)
                .ToDictionary(v => (int)v, v => v.ToString());

            catalogue.Should().BeEquivalentTo(enumeration);
        }

        [Fact]
        public void TheFunctionsThatPredateTheCatalogueStayOutOfIt()
        {
            AggregationCatalogue.Entries.Select(e => e.Id).Should().OnlyContain(id => id >= FirstCatalogueId);
            AggregationCatalogue.TryGet(3, out _).Should().BeFalse();
        }

        [Fact]
        public void EveryFunctionAndParameterIsDescribedAndEveryParameterHasADefaultInsideItsRange()
        {
            foreach (var descriptor in AggregationCatalogue.Entries)
            {
                descriptor.Description.Length.Should().BeGreaterThan(30, descriptor.Name);

                foreach (var parameter in descriptor.Parameters)
                {
                    parameter.Description.Length.Should().BeGreaterThan(8, descriptor.Name + "." + parameter.Name);
                    parameter.Check(parameter.Default).Should().BeNull(descriptor.Name + "." + parameter.Name);
                }
            }
        }

        [Fact]
        public void ParametersAreReflectedWithTheirKindDefaultAndRange()
        {
            AggregationCatalogue.TryGet((int)SearchFunctionType.TrimmedMean, out var trimmed).Should().BeTrue();

            var fraction = trimmed.Parameters.Should().ContainSingle().Subject;

            fraction.Should().BeEquivalentTo(new
            {
                Name = "trimFraction", Kind = AggregationParameterKind.Number, Default = 0.1, Minimum = 0d,
                Maximum = 0.5, MinimumIsExclusive = false, MaximumIsExclusive = true
            });

            AggregationCatalogue.TryGet((int)SearchFunctionType.Autocorrelation, out var autocorrelation);
            autocorrelation.Parameters.Single().Kind.Should().Be(AggregationParameterKind.WholeNumber);
            autocorrelation.OrderSensitive.Should().BeTrue();

            AggregationCatalogue.TryGet((int)SearchFunctionType.Variance, out var variance);
            variance.Parameters.Single().Kind.Should().Be(AggregationParameterKind.Flag);
            variance.Parameters.Single().Default.Should().Be(1);
            variance.OrderSensitive.Should().BeFalse();
        }

        [Fact]
        public void BindingAppliesSuppliedParametersWithoutRegardToCaseAndDefaultsTheRest()
        {
            double[] values = [100, 1, 4, 2, 3];

            AggregationParameters.Bind((int)SearchFunctionType.TrimmedMean, null)!(values).Should().Be(22);
            AggregationParameters.Bind((int)SearchFunctionType.TrimmedMean, "{\"TRIMFRACTION\": 0.2}")!(values)
                .Should().Be(3);
            AggregationParameters.Bind((int)SearchFunctionType.Variance, "{\"unbiased\": false}")!([1, 3])
                .Should().Be(1);
            AggregationParameters.Bind((int)SearchFunctionType.NthValue, "{\"position\": 2}")!(values)
                .Should().Be(1);
        }

        [Fact]
        public void BindingAFunctionOutsideTheCatalogueGivesNothing()
        {
            AggregationParameters.Bind((int)SearchFunctionType.Sum, null).Should().BeNull();
        }

        [Theory]
        [InlineData(19, "{\"trimFraction\": 0.5}", "less than 0.5")]
        [InlineData(19, "{\"trimFraction\": -0.1}", "at least 0")]
        [InlineData(19, "{\"fraction\": 0.1}", "no parameter named fraction")]
        [InlineData(19, "{\"trimFraction\": \"a lot\"}", "must be a number or true or false")]
        [InlineData(19, "[0.1]", "must be a JSON object")]
        [InlineData(19, "{trimFraction", "not valid JSON")]
        [InlineData(21, "{\"alpha\": 0}", "greater than 0")]
        [InlineData(21, "{\"alpha\": 1.5}", "at most 1")]
        [InlineData(32, "{\"position\": 1.5}", "whole number")]
        [InlineData(32, "{\"position\": 0}", "at least 1")]
        [InlineData(23, "{\"unbiased\": 2}", "true or false")]
        [InlineData(24, "{\"anything\": 1}", "takes no parameters")]
        [InlineData(3, "{\"anything\": 1}", "takes no parameters")]
        public void InvalidParametersAreRefusedWithTheReason(int id, string json, string reason)
        {
            AggregationParameters.Validate(id, json).Should().ContainSingle().Which.Should().Contain(reason);
        }

        [Theory]
        [InlineData(19, null)]
        [InlineData(19, "")]
        [InlineData(19, "null")]
        [InlineData(19, "{}")]
        [InlineData(19, "{\"trimFraction\": 0}")]
        [InlineData(21, "{\"alpha\": 1}")]
        [InlineData(23, "{\"unbiased\": true}")]
        [InlineData(23, "{\"unbiased\": 0}")]
        [InlineData(26, "{\"percentile\": 100}")]
        [InlineData(3, null)]
        [InlineData(3, "{}")]
        public void ValidOrAbsentParametersAreAccepted(int id, string? json)
        {
            AggregationParameters.Validate(id, json).Should().BeEmpty();
        }

        [Fact]
        public void ATypedMapOfParametersIsValidatedTheSameWayAsStoredJson()
        {
            AggregationParameters.Validate(19, new Dictionary<string, double> { ["trimFraction"] = 0.2 })
                .Should().BeEmpty();
            AggregationParameters.Validate(19, (IReadOnlyDictionary<string, double>?)null).Should().BeEmpty();
            AggregationParameters.Validate(3, new Dictionary<string, double>()).Should().BeEmpty();

            AggregationParameters.Validate(19, new Dictionary<string, double> { ["trimFraction"] = 0.5 })
                .Should().ContainSingle().Which.Should().Contain("less than 0.5");

            AggregationParameters.Validate(3, new Dictionary<string, double> { ["anything"] = 1 })
                .Should().ContainSingle().Which.Should().Contain("takes no parameters");
        }

        [Fact]
        public void ParametersAreWrittenAsSortedJsonAndReadBackWithTrueAndFalseAsOneAndZero()
        {
            AggregationParameters.Write(new Dictionary<string, double> { ["b"] = 2, ["a"] = 0.5 })
                .Should().Be("{\"a\":0.5,\"b\":2.0}");

            AggregationParameters.Write(new Dictionary<string, double>()).Should().BeNull();
            AggregationParameters.Write(null).Should().BeNull();

            AggregationParameters.Read("{\"unbiased\": true, \"lag\": 3}")
                .Should().Equal(new Dictionary<string, double> { ["unbiased"] = 1, ["lag"] = 3 });

            AggregationParameters.Read(null).Should().BeNull();
            AggregationParameters.Read("{}").Should().BeNull();
            AggregationParameters.Read("not json").Should().BeNull();
        }

        [Fact]
        public void BindingInvalidParametersThrowsRatherThanGuessing()
        {
            var bind = () => AggregationParameters.Bind((int)SearchFunctionType.TrimmedMean, "{\"trimFraction\": 9}");

            bind.Should().Throw<System.ArgumentException>();
        }
    }
}
