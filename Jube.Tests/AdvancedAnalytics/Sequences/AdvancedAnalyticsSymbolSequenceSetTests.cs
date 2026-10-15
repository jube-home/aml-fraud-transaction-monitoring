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
using Jube.Engine.AdvancedAnalytics.Sequences;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Sequences
{
    public class AdvancedAnalyticsSymbolSequenceSetTests
    {
        [Fact]
        public void OffsetsAndSequencesAreDerivedFromLengths()
        {
            var set = new AdvancedAnalyticsSymbolSequenceSet([0, 1, 2, 2, 1], [3, 2]);

            set.SequenceCount.Should().Be(2);
            set.SymbolCount.Should().Be(5);
            set.MaximumSymbol.Should().Be(2);
            set.Offset(1).Should().Be(3);
            set.GetSequence(0).Should().Equal(0, 1, 2);
            set.GetSequence(1).Should().Equal(2, 1);
        }

        [Fact]
        public void JaggedRoundTripIsLossless()
        {
            var jagged = new[] { new[] { 3, 1 }, new[] { 0 }, new[] { 2, 2, 2 } };

            var set = AdvancedAnalyticsSymbolSequenceSet.FromJagged(jagged);

            set.ToJagged().Should().BeEquivalentTo(jagged, o => o.WithStrictOrdering());
        }

        [Fact]
        public void InputIsCopiedAndReturnedSequencesAreCopies()
        {
            var symbols = new[] { 1, 2 };
            var set = new AdvancedAnalyticsSymbolSequenceSet(symbols, [2]);

            symbols[0] = 9;
            set.GetSequence(0)[1] = 9;

            set.Symbols.Should().Equal(1, 2);
        }

        [Theory]
        [InlineData(new[] { 1, 2, 3 }, new[] { 1, 1 }, "add up")]
        [InlineData(new[] { 1, 2 }, new[] { 2, 0 }, "empty")]
        [InlineData(new[] { 1, -2 }, new[] { 2 }, "negative")]
        public void InvalidLayoutsAreRejected(int[] symbols, int[] lengths, string message)
        {
            var act = () => new AdvancedAnalyticsSymbolSequenceSet(symbols, lengths);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage($"*{message}*");
        }

        [Fact]
        public void NoSequencesIsRejected()
        {
            var act = () => new AdvancedAnalyticsSymbolSequenceSet([], []);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void JsonRoundTripKeepsTheAgreedShape()
        {
            var set = new AdvancedAnalyticsSymbolSequenceSet([0, 1, 2], [1, 2]);

            var json = JsonConvert.SerializeObject(set);
            var back = JsonConvert.DeserializeObject<AdvancedAnalyticsSymbolSequenceSet>(json)!;

            json.Should().Be("{\"symbols\":[0,1,2],\"sequenceLengths\":[1,2]}");
            back.Symbols.Should().Equal(set.Symbols);
            back.SequenceLengths.Should().Equal(set.SequenceLengths);
        }
    }
}