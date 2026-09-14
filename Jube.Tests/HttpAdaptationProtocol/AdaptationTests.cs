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

using FluentAssertions;
using Jube.HttpAdaptationProtocol;
using Jube.HttpAdaptationProtocol.Calibration;
using Jube.HttpAdaptationProtocol.Contribution;
using Jube.HttpAdaptationProtocol.Journey;
using Jube.HttpAdaptationProtocol.Model;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol
{
    [Trait("Category", "Unit")]
    public sealed class AdaptationTests
    {
        [Fact]
        public void IsSuppressedIsTrueWhenValueIsNull()
        {
            var adaptation = new Adaptation { Value = null };

            adaptation.IsSuppressed.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressedIsTrueWhenErrorIsSet()
        {
            var adaptation = new Adaptation { Value = 0.5, Error = "Some downstream failure." };

            adaptation.IsSuppressed.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressedIsFalseWhenValueIsSetAndErrorIsNull()
        {
            var adaptation = new Adaptation { Value = 0.5, Error = null };

            adaptation.IsSuppressed.Should().BeFalse();
        }

        [Fact]
        public void IsSuppressedIsFalseWhenValueIsSetAndErrorIsEmpty()
        {
            var adaptation = new Adaptation { Value = 0.5, Error = "" };

            adaptation.IsSuppressed.Should().BeFalse();
        }

        [Fact]
        public void IsSuppressedIsTrueWhenValueIsNullAndErrorIsAlsoSet()
        {
            var adaptation = new Adaptation { Value = null, Error = "Timeout." };

            adaptation.IsSuppressed.Should().BeTrue();
        }

        [Fact]
        public void ImplicitConversionToNullableDoubleReturnsValue()
        {
            var adaptation = new Adaptation { Value = 0.85 };

            double? converted = adaptation;

            converted.Should().Be(0.85);
        }

        [Fact]
        public void ImplicitConversionToNullableDoubleReturnsNullWhenValueIsNull()
        {
            var adaptation = new Adaptation { Value = null };

            double? converted = adaptation;

            converted.Should().BeNull();
        }

        [Fact]
        public void ImplicitConversionToNullableDoubleReturnsNullWhenAdaptationIsNull()
        {
            Adaptation? adaptation = null;

            double? converted = adaptation;

            converted.Should().BeNull();
        }

        [Fact]
        public void HoldsAssignedFields()
        {
            var model = new ModelDescriptor { Name = "model-1" };
            var result = new ResultDescriptor { Activated = true };
            var calibration = new CalibrationDescriptor { Method = "Platt" };
            var contribution = new ContributionSet { Space = "Relative" };
            var journey = new JourneyDescriptor();

            var adaptation = new Adaptation
            {
                Value = 0.42,
                Error = null,
                Narrative = "High risk narrative.",
                HumanLabel = "High Risk",
                ProtocolVersion = "1.1",
                Model = model,
                Result = result,
                Calibration = calibration,
                Contribution = contribution,
                Journey = journey
            };

            adaptation.Value.Should().Be(0.42);
            adaptation.Error.Should().BeNull();
            adaptation.Narrative.Should().Be("High risk narrative.");
            adaptation.HumanLabel.Should().Be("High Risk");
            adaptation.ProtocolVersion.Should().Be("1.1");
            adaptation.Model.Should().BeSameAs(model);
            adaptation.Result.Should().BeSameAs(result);
            adaptation.Calibration.Should().BeSameAs(calibration);
            adaptation.Contribution.Should().BeSameAs(contribution);
            adaptation.Journey.Should().BeSameAs(journey);
        }

        [Fact]
        public void IsSuppressedIsIgnoredWhenSerializedToJson()
        {
            var adaptation = new Adaptation { Value = 0.5 };

            var json = JsonConvert.SerializeObject(adaptation);

            json.Should().NotContain("IsSuppressed");
        }

        [Fact]
        public void RecordsWithEqualValuesAreEqual()
        {
            var first = new Adaptation { Value = 0.5, HumanLabel = "Medium" };
            var second = new Adaptation { Value = 0.5, HumanLabel = "Medium" };

            first.Should().Be(second);
        }

        [Fact]
        public void RecordsWithDifferentValuesAreNotEqual()
        {
            var first = new Adaptation { Value = 0.5 };
            var second = new Adaptation { Value = 0.6 };

            first.Should().NotBe(second);
        }

        [Fact]
        public void WithExpressionProducesAModifiedCopyWithoutMutatingTheOriginal()
        {
            var original = new Adaptation { Value = 0.5, Error = "boom" };

            var updated = original with { Value = null };

            updated.Value.Should().BeNull();
            updated.Error.Should().Be("boom");
            original.Value.Should().Be(0.5);
        }
    }
}