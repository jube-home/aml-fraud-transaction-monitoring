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

using System;
using FluentAssertions;
using Jube.HttpAdaptationProtocol.Model;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Model
{
    [Trait("Category", "Unit")]
    public sealed class ValidationDescriptorTests
    {
        [Fact]
        public void HoldsAssignedFields()
        {
            var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextReview = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var descriptor = new ValidationDescriptor
            {
                Date = date,
                Sample = 25000,
                Auc = 0.82,
                Gini = 0.64,
                Ks = 0.45,
                Brier = 0.09,
                PopulationStabilityIndex = 0.03,
                NextReviewDate = nextReview,
                Stale = false
            };

            descriptor.Date.Should().Be(date);
            descriptor.Sample.Should().Be(25000);
            descriptor.Auc.Should().Be(0.82);
            descriptor.Gini.Should().Be(0.64);
            descriptor.Ks.Should().Be(0.45);
            descriptor.Brier.Should().Be(0.09);
            descriptor.PopulationStabilityIndex.Should().Be(0.03);
            descriptor.NextReviewDate.Should().Be(nextReview);
            descriptor.Stale.Should().BeFalse();
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var descriptor = new ValidationDescriptor();

            descriptor.Date.Should().BeNull();
            descriptor.Sample.Should().BeNull();
            descriptor.Auc.Should().BeNull();
            descriptor.Gini.Should().BeNull();
            descriptor.Ks.Should().BeNull();
            descriptor.Brier.Should().BeNull();
            descriptor.PopulationStabilityIndex.Should().BeNull();
            descriptor.NextReviewDate.Should().BeNull();
            descriptor.Stale.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJson()
        {
            var original = new ValidationDescriptor
            {
                Date = new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                Sample = 1000,
                Auc = 0.9,
                Gini = 0.8,
                Ks = 0.5,
                Brier = 0.05,
                PopulationStabilityIndex = 0.01,
                NextReviewDate = new DateTime(2024, 8, 2, 0, 0, 0, DateTimeKind.Utc),
                Stale = true
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ValidationDescriptor>(json);

            deserialized.Should().Be(original);
        }
    }
}