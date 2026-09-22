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
using System.Diagnostics;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Jube.Dictionary.Fuzzy;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowAuditTests
    {
        [Fact]
        public void ADefaultFlowIsUndecidedAndConvertsFalse()
        {
            var flow = default(Flow<double>);

            flow.Outcome.Should().Be(FlowOutcome.Undecided);
            flow.IsDecided.Should().BeFalse();
            flow.Score.Should().Be(0d);
            flow.Label.Should().BeNull();
            flow.ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void ADefaultFlowCanStillBeDecided()
        {
            default(Flow<double>).Match().Outcome.Should().Be(FlowOutcome.Matched);
            default(Flow<double>).RequireGreater(1).Outcome.Should().Be(FlowOutcome.Rejected);
        }

        [Fact]
        public void EveryParseStepLeavesADecidedFlowDecidedWithoutParsing()
        {
            "abc".Start().Reject().ParseDouble().Outcome.Should().Be(FlowOutcome.Rejected);
            "abc".Start().Break().ParseInteger().Outcome.Should().Be(FlowOutcome.Broken);
            "abc".Start().Match().ParseDate().Outcome.Should().Be(FlowOutcome.Matched);
            "abc".Start().Fail("x").ParseBoolean().Outcome.Should().Be(FlowOutcome.Errored);
        }

        [Fact]
        public void ParseStepsKeepTheScoreAndLabelOfTheFlow()
        {
            var flow = "5".Start().AddScore(3).Label("l").ParseInteger();

            flow.Score.Should().Be(3d);
            flow.Label.Should().Be("l");
            flow.Value.Should().Be(5);
        }

        [Fact]
        public void AFailedParseKeepsTheScoreButRecordsTheReason()
        {
            var flow = "x".Start().AddScore(3).ParseBoolean();

            flow.Outcome.Should().Be(FlowOutcome.Errored);
            flow.Score.Should().Be(3d);
            flow.Label.Should().Contain("boolean");
        }

        [Fact]
        public void RawTransformersBeginAPipelineOrReturnAPlainNumberWithoutStart()
        {
            "ABC".Lower().Value.Should().Be("abc");
            "abc".Upper().Value.Should().Be("ABC");
            "12.5".ParseDouble().Value.Should().Be(12.5);
            "12".ParseInteger().Value.Should().Be(12);
            "2024-03-15".ParseDate().Value.Should().Be(new DateTime(2024, 3, 15));
            "true".ParseBoolean().Value.Should().BeTrue();
            new DateTime(2024, 3, 15, 10, 0, 0).HourOfDay().Value.Should().Be(10);
            new DateTime(2024, 3, 15, 14, 0, 0).ToZone("Asia/Tokyo").Value.Hour.Should().Be(23);
            5d.Plus(2).Should().Be(7d);
            5d.Minus(2).Should().Be(3d);
            5d.Times(2).Should().Be(10d);
            5d.DividedBy(2).Should().Be(2.5d);
        }

        [Fact]
        public void RawTransformersFailClosedOnBadInput()
        {
            double.IsNaN(5d.DividedBy(0)).Should().BeTrue();
            double.IsNaN(double.NaN.Plus(1)).Should().BeTrue();
            double.IsNaN(double.PositiveInfinity.Times(2)).Should().BeTrue();
            new DateTime(2024, 3, 15).ToZone("Not/AZone").Outcome.Should().Be(FlowOutcome.Errored);
            "abc".ParseInteger().Outcome.Should().Be(FlowOutcome.Errored);
            ((string?)null).ParseDate().Outcome.Should().Be(FlowOutcome.Errored);
            ((string?)null).ParseBoolean().Outcome.Should().Be(FlowOutcome.Errored);
            ((string?)null).Upper().Value.Should().BeNull();
        }

        [Theory]
        [InlineData(0.5, 0.5)]
        [InlineData(0d, 0d)]
        [InlineData(-3d, -3d)]
        public void OrNaNAndOrZeroPassAValueThrough(double value, double expected)
        {
            ((double?)value).OrNaN().Should().Be(expected);
            ((double?)value).OrZero().Should().Be(expected);
        }

        [Fact]
        public void ANullNullableIsNaNForOrNaNAndZeroForOrZero()
        {
            double.IsNaN(((double?)null).OrNaN()).Should().BeTrue();
            ((double?)null).OrZero().Should().Be(0d);
        }

        [Fact]
        public void ANonFiniteNullableIsNaNForOrNaNAndZeroForOrZero()
        {
            double.IsNaN(((double?)double.PositiveInfinity).OrNaN()).Should().BeTrue();
            ((double?)double.NaN).OrZero().Should().Be(0d);
        }

        [Fact]
        public void OrBetweenTwoPipelinesIsTrueWhenEitherMatched()
        {
            var matched = 5d.MatchGreater(1);
            var notMatched = 5d.MatchLess(1);

            (matched | notMatched).Should().BeTrue();
            (notMatched | matched).Should().BeTrue();
            (notMatched | notMatched).Should().BeFalse();
            (matched | matched).Should().BeTrue();
        }

        [Fact]
        public void AndBetweenTwoPipelinesIsTrueOnlyWhenBothMatched()
        {
            var matched = 5d.MatchGreater(1);
            var notMatched = 5d.MatchLess(1);

            (matched & matched).Should().BeTrue();
            (matched & notMatched).Should().BeFalse();
            (notMatched & matched).Should().BeFalse();
        }

        [Fact]
        public void OperatorsAcceptPipelinesOfDifferentTypesThroughTheBooleanForm()
        {
            var number = 5d.MatchGreater(1);
            var text = "abc".MatchContains("z");

            ((bool)number | text.ToBoolean()).Should().BeTrue();
            (number | text.ToBoolean()).Should().BeTrue();
            (number & text.ToBoolean()).Should().BeFalse();
        }

        [Fact]
        public void AnUndecidedRejectedBrokenOrErroredPipelineIsFalseInACombination()
        {
            var matched = 5d.MatchGreater(1);

            (matched & 5d.Start()).Should().BeFalse();
            (matched & 5d.Start().Reject()).Should().BeFalse();
            (matched & 5d.Start().Break()).Should().BeFalse();
            (matched & 5d.Start().Fail("x")).Should().BeFalse();
            (matched | 5d.Start().Fail("x")).Should().BeTrue();
        }

        [Fact]
        public void NotIsTrueForEverythingExceptAMatchedPipeline()
        {
            (!5d.MatchGreater(1)).Should().BeFalse();
            (!5d.MatchLess(1)).Should().BeTrue();
            (!5d.Start().Reject()).Should().BeTrue();
            (!5d.Start().Fail("x")).Should().BeTrue();
        }

        [Fact]
        public void PlainArithmeticOnANumberReturnsANumberSoItFitsANumericRule()
        {
            double total = 100d.Plus(5).Minus(1).Times(2).DividedBy(4);

            total.Should().Be(52d);
        }

        [Theory]
        [InlineData("12.5", 12.5)]
        [InlineData(" 7 ", 7)]
        [InlineData("1e3", 1000)]
        [InlineData("-3", -3)]
        public void ToDoubleOrNaNReadsInvariantNumbers(string text, double expected)
        {
            text.ToDoubleOrNaN().Should().Be(expected);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("1,5")]
        [InlineData("Infinity")]
        [InlineData("NaN")]
        [InlineData(null)]
        public void ToDoubleOrNaNIsNaNForAnythingThatIsNotAFiniteNumber(string? text)
        {
            double.IsNaN(text.ToDoubleOrNaN()).Should().BeTrue();
        }

        [Fact]
        public void ToNumberReadsTheValueOfANumericFlowAndIsNaNWhenErrored()
        {
            5d.Start().ToNumber().Should().Be(5d);
            5.Start().ToNumber().Should().Be(5d);
            double.IsNaN(5d.Start().Fail("x").ToNumber()).Should().BeTrue();
            double.IsNaN(5.Start().Fail("x").ToNumber()).Should().BeTrue();
            "12".ParseDouble().ToNumber().Should().Be(12d);
            double.IsNaN("abc".ParseDouble().ToNumber()).Should().BeTrue();
            5d.Start().Reject().ToNumber().Should().Be(5d);
        }

        [Fact]
        public void APathologicalRegexTimesOutQuicklyAndIsFalseNotAHang()
        {
            var input = new string('a', 60) + "b";
            var stopwatch = Stopwatch.StartNew();

            var flow = input.MatchMatches("^(a+)+$");

            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
            flow.ToBoolean().Should().BeFalse();
            flow.Outcome.Should().Be(FlowOutcome.Undecided);
        }

        [Fact]
        public void ARegexThatIsInvalidIsFalseNotAnException()
        {
            "abc".MatchMatches("[").ToBoolean().Should().BeFalse();
            "abc".RequireMatches("[").Outcome.Should().Be(FlowOutcome.Rejected);
        }

        [Theory]
        [InlineData("ß", "ss")]
        [InlineData("æ", "ae")]
        [InlineData("Æ", "AE")]
        [InlineData("œ", "oe")]
        [InlineData("Œ", "OE")]
        [InlineData("ø", "o")]
        [InlineData("Ø", "O")]
        [InlineData("đ", "d")]
        [InlineData("Đ", "D")]
        [InlineData("ł", "l")]
        [InlineData("Ł", "L")]
        [InlineData("ı", "i")]
        [InlineData("é", "e")]
        [InlineData("Ñ", "N")]
        public void EveryFoldedLetterMapsToItsPlainForm(string input, string expected)
        {
            FuzzyNormaliser.FoldDiacritics(input).Should().Be(expected);
        }

        [Fact]
        public void AMoreThanTheCacheLimitOfDistinctOptionStringsStillParsesCorrectly()
        {
            for (var i = 0; i < 1200; i++)
            {
                var options = FuzzyOptions.Parse("lev=" + i % 11 + ",minlength=" + i);

                options.IsValid.Should().BeTrue();
                options.MinLength.Should().Be(i);
            }

            FuzzyOptions.Parse("lev=2,minlength=1199").MinLength.Should().Be(1199);
        }

        [Fact]
        public void ATimeZoneWithDaylightSavingDoesNotShiftAWinterHour()
        {
            new DateTime(2024, 1, 15, 14, 0, 0).MatchWithinBusinessHoursInZone("Europe/London", 14, 15).ToBoolean()
                .Should().BeTrue();
            new DateTime(2024, 7, 15, 14, 0, 0).MatchWithinBusinessHoursInZone("Europe/London", 14, 15).ToBoolean()
                .Should().BeFalse();
        }

        [Fact]
        public void AZoneBoundaryCrossingMidnightChangesTheWeekday()
        {
            new DateTime(2024, 3, 15, 23, 30, 0).MatchIsWeekdayInZone("Asia/Tokyo").ToBoolean().Should().BeFalse();
            new DateTime(2024, 3, 15, 23, 30, 0).MatchIsWeekendInZone("Asia/Tokyo").ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void ALocalKindValueIsConvertedViaUniversalTimeForZoneSteps()
        {
            var local = new DateTime(2024, 3, 15, 14, 0, 0, DateTimeKind.Local);
            var expected = TimeZoneInfo.ConvertTimeFromUtc(local.ToUniversalTime(),
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo")).Hour;

            local.ToZone("Asia/Tokyo").Value.Hour.Should().Be(expected);
        }

        [Fact]
        public void AnUnspecifiedAndAUtcValueAreTreatedTheSameByZoneSteps()
        {
            var unspecified = new DateTime(2024, 3, 15, 14, 0, 0, DateTimeKind.Unspecified);
            var utc = new DateTime(2024, 3, 15, 14, 0, 0, DateTimeKind.Utc);

            unspecified.ToZone("Asia/Tokyo").Value.Should().Be(utc.ToZone("Asia/Tokyo").Value);
        }
    }
}
