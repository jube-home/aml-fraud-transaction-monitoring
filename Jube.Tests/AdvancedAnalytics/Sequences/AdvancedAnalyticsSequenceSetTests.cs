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
using System.Collections.Generic;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Sequences;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Sequences
{
    public class AdvancedAnalyticsSequenceSetTests
    {
        private static AdvancedAnalyticsSequenceSet Sample()
        {
            return new AdvancedAnalyticsSequenceSet([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10], 2, [2, 1, 2]);
        }

        [Fact]
        public void OffsetsAreDerivedFromLengths()
        {
            var set = Sample();

            set.SequenceCount.Should().Be(3);
            set.ObservationCount.Should().Be(5);
            set.Offset(0).Should().Be(0);
            set.Offset(1).Should().Be(2);
            set.Offset(2).Should().Be(3);
        }

        [Fact]
        public void SequencesComeBackAsRows()
        {
            var set = Sample();

            set.GetSequence(0).Should()
                .BeEquivalentTo([[1.0, 2], new[] { 3.0, 4 }], o => o.WithStrictOrdering());

            set.GetSequence(1).Should().BeEquivalentTo([new[] { 5.0, 6 }]);
            set.GetSequence(2)[1].Should().Equal(9.0, 10.0);
        }

        [Fact]
        public void JaggedRoundTripIsLossless()
        {
            var set = Sample();

            var again = AdvancedAnalyticsSequenceSet.FromJagged(set.ToJagged());

            again.Values.Should().Equal(set.Values);
            again.SequenceLengths.Should().Equal(set.SequenceLengths);
            again.FeatureCount.Should().Be(2);
        }

        [Fact]
        public void InputArraysAreCopiedSoTheSetIsImmutable()
        {
            var values = new[] { 1.0, 2.0 };
            var lengths = new[] { 2 };
            var set = new AdvancedAnalyticsSequenceSet(values, 1, lengths);

            values[0] = 99;
            lengths[0] = 7;

            set.Values[0].Should().Be(1.0);
            set.SequenceLengths[0].Should().Be(2);
            set.Values.Should().BeAssignableTo<IReadOnlyList<double>>();
            ((IList<double>)set.Values).IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void ReturnedSequencesAreCopies()
        {
            var set = Sample();

            set.GetSequence(0)[0][0] = 100;

            set.GetSequence(0)[0][0].Should().Be(1.0);
        }

        [Fact]
        public void LengthsMustSumToTheObservationCount()
        {
            var act = () => new AdvancedAnalyticsSequenceSet([1.0, 2, 3, 4], 2, [1, 2]);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*add up*");
        }

        [Fact]
        public void EmptySequencesAreRejected()
        {
            var act = () => new AdvancedAnalyticsSequenceSet([1.0, 2], 1, [2, 0]);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*empty*");
        }

        [Fact]
        public void NoSequencesIsRejected()
        {
            var act = () => new AdvancedAnalyticsSequenceSet([], 1, []);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void FeatureCountMustBePositive(int featureCount)
        {
            var act = () => new AdvancedAnalyticsSequenceSet([1.0], featureCount, [1]);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void ValueCountMustBeAWholeNumberOfObservations()
        {
            var act = () => new AdvancedAnalyticsSequenceSet([1.0, 2, 3], 2, [1]);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*whole number*");
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NonFiniteValuesAreRejected(double bad)
        {
            var act = () => new AdvancedAnalyticsSequenceSet([1.0, bad], 1, [2]);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*finite*");
        }

        [Fact]
        public void LengthsThatOverflowAnIntAreRejectedNotWrapped()
        {
            var act = () =>
                new AdvancedAnalyticsSequenceSet([1.0, 2], 1, [int.MaxValue, int.MaxValue, 2]);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*add up*");
        }

        [Fact]
        public void RaggedJaggedInputWithInconsistentFeaturesIsRejected()
        {
            var act = () => AdvancedAnalyticsSequenceSet.FromJagged([
                [[1.0, 2.0]],
                [[1.0]]
            ]);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void SequenceIndexIsRangeChecked()
        {
            var set = Sample();

            ((Action)(() => set.GetSequence(3))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => set.Offset(-1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void JsonShapeUsesTheAgreedNamesAndRoundTripsThroughValidation()
        {
            var set = Sample();

            var json = JsonConvert.SerializeObject(set,
                new JsonSerializerSettings
                    { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            var back = JsonConvert.DeserializeObject<AdvancedAnalyticsSequenceSet>(json)!;

            json.Should().Contain("\"values\"").And.Contain("\"featureCount\"").And.Contain("\"sequenceLengths\"");
            json.Should().NotContain("sequenceCount");
            back.Values.Should().Equal(set.Values);
            back.SequenceLengths.Should().Equal(set.SequenceLengths);
        }

        [Fact]
        public void DeserialisingBrokenLengthsFailsThroughTheConstructor()
        {
            var act = () => JsonConvert.DeserializeObject<AdvancedAnalyticsSequenceSet>(
                "{\"values\":[1,2,3],\"featureCount\":1,\"sequenceLengths\":[1,1]}");

            act.Should().Throw<Exception>();
        }
    }
}