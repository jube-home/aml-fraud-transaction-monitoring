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
using System.Linq;
using FluentAssertions;
using Jube.Analytics.Categorical;
using Jube.Analytics.Categorical.Models;
using Xunit;

namespace Jube.Test.Analytics.Categorical
{
    using static CategoricalTestData;

    public class CategoricalFieldEncoderTests
    {
        [Fact]
        public void Fit_RanksByFrequencyAndUsesTheMostFrequentAsReference()
        {
            var encoder = Country();

            encoder.Reference.Key.Should().Be("gb");
            encoder.Reference.Label.Should().Be("GB");
            encoder.Levels.Select(s => s.Key).Should().Equal("gb", "us", "ng", "fr");

            encoder.Indicators.Select(s => s.Name).Should().Equal(
                "Payload.Country=US", "Payload.Country=NG", "Payload.Country=FR",
                "Payload.Country=[Other]", "Payload.Country=[Missing]");

            encoder.Indicators.Select(s => s.Kind).Should().Equal(
                CategoricalIndicatorKind.Level, CategoricalIndicatorKind.Level, CategoricalIndicatorKind.Level,
                CategoricalIndicatorKind.Other, CategoricalIndicatorKind.Missing);
        }

        [Fact]
        public void Fit_CountsAreExact()
        {
            var encoder = Country();

            encoder.TotalCount.Should().Be(219);
            encoder.MissingCount.Should().Be(12);
            encoder.PooledCount.Should().Be(7);
            encoder.Levels.Sum(s => s.Count).Should().Be(200);
        }

        [Fact]
        public void Apply_KnownLevelSetsExactlyOneIndicator()
        {
            var encoder = Country();
            var destination = new double[encoder.Indicators.Count];

            encoder.Encode("US", destination);
            destination.Should().Equal(1, 0, 0, 0, 0);

            encoder.Encode("nG", destination);
            destination.Should().Equal(0, 1, 0, 0, 0);
        }

        [Fact]
        public void Apply_ReferenceLevelIsAllZero()
        {
            var encoder = Country();
            var destination = new double[encoder.Indicators.Count];
            destination[0] = 9;

            encoder.Encode(" gb ", destination);

            destination.Should().OnlyContain(v => v == 0);
        }

        [Fact]
        public void Apply_UnseenAndPooledLevelsMapToOther()
        {
            var encoder = Country();
            var destination = new double[encoder.Indicators.Count];

            encoder.Encode("XX", destination);
            destination.Should().Equal(0, 0, 0, 1, 0);

            encoder.Encode("never seen before", destination);
            destination.Should().Equal(0, 0, 0, 1, 0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Apply_MissingMapsToTheMissingIndicator(string? raw)
        {
            var encoder = Country();
            var destination = new double[encoder.Indicators.Count];

            encoder.Encode(raw, destination);

            destination.Should().Equal(0, 0, 0, 0, 1);
        }

        [Fact]
        public void Apply_WithoutOtherOrMissingIndicatorUnseenAndMissingAreAllZero()
        {
            var counter = Count(("A", 50), ("B", 30), ("C", 1), (null, 2));
            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, Options(10))!;
            var destination = new double[encoder.Indicators.Count];

            encoder.Indicators.Select(s => s.Name).Should().Equal("Payload.X=B");
            encoder.HasOther.Should().BeFalse();
            encoder.HasMissing.Should().BeFalse();

            encoder.Encode("unseen", destination);
            destination.Should().Equal(0);
            encoder.Encode(null, destination);
            destination.Should().Equal(0);
            encoder.Encode("B", destination);
            destination.Should().Equal(1);
        }

        [Fact]
        public void Indicator_MatchesEncodeForEveryIndex()
        {
            var encoder = Country();

            foreach (var raw in Sample().Concat(["US", "NG", "FR", "XX"]))
            {
                var destination = new double[encoder.Indicators.Count];
                encoder.Encode(raw, destination);

                for (var i = 0; i < destination.Length; i++)
                {
                    encoder.Indicator(raw, i).Should().Be(destination[i]);
                }
            }
        }

        [Fact]
        public void Apply_IsAtMostOneHot()
        {
            var encoder = Country();

            foreach (var raw in Sample())
            {
                var destination = new double[encoder.Indicators.Count];
                encoder.Encode(raw, destination);
                destination.Sum().Should().BeLessThanOrEqualTo(1);
            }
        }

        [Fact]
        public void Apply_NormalisationVariantsGiveTheSameIndicators()
        {
            var encoder = Country();
            var expected = new double[encoder.Indicators.Count];
            encoder.Encode("US", expected);

            foreach (var variant in new[] { "us", "Us", " US", "US ", "ＵＳ", "U​S", "\tU S".Replace(" ", "") })
            {
                var destination = new double[encoder.Indicators.Count];
                encoder.Encode(variant, destination);
                destination.Should().Equal(expected, variant);
            }
        }

        [Fact]
        public void Fit_MaxLevelsPoolsTheTail()
        {
            var counter = Count(("A", 100), ("B", 80), ("C", 60), ("D", 40), ("E", 20));
            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, Options(5, 3))!;

            encoder.Levels.Select(s => s.Key).Should().Equal("a", "b", "c");
            encoder.HasOther.Should().BeTrue();
            encoder.PooledCount.Should().Be(60);
        }

        [Fact]
        public void Fit_MinimumFrequencyPools()
        {
            var counter = Count(("A", 100), ("B", 80), ("C", 6), ("D", 4));
            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, Options())!;

            encoder.Levels.Select(s => s.Key).Should().Equal("a", "b", "c");
            encoder.HasOther.Should().BeFalse();
            encoder.PooledCount.Should().Be(4);
        }

        [Fact]
        public void Fit_MinimumFractionRaisesTheMinimum()
        {
            var counter = Count(("A", 1000), ("B", 500), ("C", 40));
            var options = Options();
            options.MinLevelFraction = 0.05;

            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, options)!;

            encoder.Levels.Select(s => s.Key).Should().Equal("a", "b");
            encoder.HasOther.Should().BeFalse();
        }

        [Fact]
        public void Fit_CoverageTargetStopsEarly()
        {
            var counter = Count(("A", 900), ("B", 60), ("C", 30), ("D", 10));
            var options = Options();
            options.CoverageTarget = 0.95;

            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, options)!;

            encoder.Levels.Select(s => s.Key).Should().Equal("a", "b");
            encoder.HasOther.Should().BeTrue();
            encoder.PooledCount.Should().Be(40);
        }

        [Fact]
        public void Fit_TiesAreBrokenByOrdinalKeyOrder()
        {
            var counter = Count(("b", 10), ("a", 10), ("c", 10));
            var first = CategoricalFieldEncoder.Fit("Payload.X", counter, Options())!;

            first.Levels.Select(s => s.Key).Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Fit_ConstantFieldProducesNoEncoder()
        {
            var counter = Count(("A", 100));
            CategoricalFieldEncoder.Fit("Payload.X", counter, Options()).Should().BeNull();
        }

        [Fact]
        public void Fit_NoValuesProducesNoEncoder()
        {
            var counter = Count((null, 100));
            CategoricalFieldEncoder.Fit("Payload.X", counter, Options()).Should().BeNull();
            CategoricalFieldEncoder.Fit("Payload.X", new CategoricalCounter(), Options()).Should().BeNull();
        }

        [Fact]
        public void Fit_SingleLevelWithEnoughMissingKeepsTheMissingIndicator()
        {
            var counter = Count(("A", 100), (null, 50));
            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, Options())!;

            encoder.Indicators.Select(s => s.Name).Should().Equal("Payload.X=[Missing]");
        }

        [Fact]
        public void Fit_LabelIsTheMostFrequentSpelling()
        {
            var counter = Count(("Gb", 3), ("GB", 10), ("gb", 4), ("US", 20));
            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, Options(2))!;

            encoder.Levels.Single(s => s.Key == "gb").Label.Should().Be("GB");
        }

        [Fact]
        public void Fit_IsDeterministicAndOrderIndependent()
        {
            var values = Enumerable.Range(0, 5000).Select(i => "L" + i * 7919 % 37).ToList();
            var forward = new CategoricalCounter();
            var backward = new CategoricalCounter();
            values.ForEach(forward.Add);
            Enumerable.Reverse(values).ToList().ForEach(backward.Add);

            var a = CategoricalFieldEncoder.Fit("Payload.X", forward, Options())!.ToJson();
            var b = CategoricalFieldEncoder.Fit("Payload.X", backward, Options())!.ToJson();
            var c = CategoricalFieldEncoder.Fit("Payload.X", forward, Options())!.ToJson();

            a.Should().Be(b).And.Be(c);
        }

        [Fact]
        public void Counter_CapsTheDistinctKeysAndCountsTheOverflow()
        {
            var counter = new CategoricalCounter(10);

            for (var i = 0; i < 1000; i++)
            {
                counter.Add("k" + i);
            }

            counter.Distinct.Should().Be(10);
            counter.Overflow.Should().Be(990);
            counter.DistinctTruncated.Should().BeTrue();
            counter.Total.Should().Be(1000);
        }

        [Fact]
        public void Serialisation_RoundTripsToIdenticalBehaviour()
        {
            var encoder = Country();
            var restored = CategoricalFieldEncoder.FromJson(encoder.ToJson());

            restored.ToJson().Should().Be(encoder.ToJson());
            restored.Indicators.Should().Equal(encoder.Indicators);

            foreach (var raw in Sample().Concat(["US", "NG", "FR", "XX", "Nowhere"]))
            {
                restored.Resolve(raw).Should().Be(encoder.Resolve(raw));
            }
        }

        [Fact]
        public void Serialisation_HostileLabelsSurviveARoundTrip()
        {
            var counter = Count(("<b>x</b>\"'; --", 50), ("😀", 30), ("plain", 20));
            var encoder = CategoricalFieldEncoder.Fit("Payload.X", counter, Options())!;

            var restored = CategoricalFieldEncoder.FromJson(encoder.ToJson());

            restored.ToJson().Should().Be(encoder.ToJson());
            restored.Resolve("😀").Should().Be(encoder.Resolve("😀"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{}")]
        [InlineData("{\"version\":1,\"sourceName\":\"x\",\"levels\":[]}")]
        [InlineData(
            "{\"version\":1,\"sourceName\":\"x\",\"levels\":[{\"key\":\"UPPER\",\"label\":\"a\",\"count\":1}]}")]
        [InlineData("{\"version\":1,\"sourceName\":\"x\",\"levels\":[{\"key\":\"a\"},{\"key\":\"a\"}]}")]
        [InlineData("{\"version\":1,\"sourceName\":\"\",\"levels\":[{\"key\":\"a\"}]}")]
        [InlineData("{\"version\":1,\"sourceName\":\"x\",\"levels\":[{\"key\":\"a\",\"count\":-1}]}")]
        public void FromJson_RejectsTamperedOrInvalidState(string json)
        {
            var act = () => CategoricalFieldEncoder.FromJson(json);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void FromJson_RejectsUnknownVersions()
        {
            var act = () => CategoricalFieldEncoder.FromJson(
                "{\"version\":2,\"sourceName\":\"x\",\"levels\":[{\"key\":\"a\",\"label\":\"a\",\"count\":1}]}");

            act.Should().Throw<NotSupportedException>();

            var normalisation = () => CategoricalFieldEncoder.FromJson(
                "{\"version\":1,\"normalisationVersion\":9,\"sourceName\":\"x\",\"levels\":[{\"key\":\"a\",\"label\":\"a\",\"count\":1}]}");

            normalisation.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void Encode_ShortDestinationThrows()
        {
            var encoder = Country();

            var act = () => encoder.Encode("US", new double[2]);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Resolve_HostileInputNeverThrows()
        {
            var encoder = Country();

            foreach (var raw in new[]
                     {
                         new string('x', 5_000_000), "\uD800", "\0", "'; DROP TABLE x;--", "😀\uD83D",
                         new string('​', 100000)
                     })
            {
                var act = () => encoder.Resolve(raw);
                act.Should().NotThrow();
            }
        }
    }
}