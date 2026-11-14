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
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Data.Helpers;
using Jube.Engine.Observability;
using Jube.Data.Helpers.Models;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Engine.Observability
{
    [Trait("Category", "Unit")]
    public sealed class LogCounterRuleCacheTests
    {
        private static string UniqueName(string label)
        {
            return $"unit.test.{label}.{Guid.NewGuid():N}";
        }

        private static LogCounterRuleCache NewCache(OpenTelemetryExcludeCache? exclude = null)
        {
            return new LogCounterRuleCache(string.Empty, TestLog.NoOp) { ExcludeCache = exclude };
        }

        private static (MeterListener Listener, Func<long> Total) Watch(string instrumentName)
        {
            long total = 0;

            var listener = new MeterListener
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == EngineDiagnostics.Name && instrument.Name == instrumentName)
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                }
            };

            listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => total += measurement);
            listener.Start();

            return (listener, () => total);
        }

        [Fact]
        public void AMatchingLineIncrementsTheRuleCounterOncePerLine()
        {
            var name = UniqueName("match");
            var cache = NewCache();
            cache.ApplyRules([(name, "deadlock detected")]);
            var (listener, total) = Watch(name);

            using (listener)
            {
                cache.CheckAndIncrement("ERROR: deadlock detected while updating");
                cache.CheckAndIncrement("ERROR: deadlock detected again");
                cache.CheckAndIncrement("all quiet");
            }

            total().Should().Be(2);
        }

        [Fact]
        public void AnExcludedRuleNeverCreatesOrIncrementsItsCounter()
        {
            var name = UniqueName("excluded");
            var exclude = new OpenTelemetryExcludeCache(string.Empty, TestLog.NoOp);
            exclude.Apply([name.ToUpperInvariant()]);
            var cache = NewCache(exclude);
            cache.ApplyRules([(name, "boom")]);
            var (listener, total) = Watch(name);

            using (listener)
            {
                cache.CheckAndIncrement("boom");
            }

            total().Should().Be(0);
        }

        [Fact]
        public void LiftingTheExclusionResumesCounting()
        {
            var name = UniqueName("lifted");
            var exclude = new OpenTelemetryExcludeCache(string.Empty, TestLog.NoOp);
            exclude.Apply([name]);
            var cache = NewCache(exclude);
            cache.ApplyRules([(name, "boom")]);
            var (listener, total) = Watch(name);

            using (listener)
            {
                cache.CheckAndIncrement("boom");
                exclude.Apply([]);
                cache.CheckAndIncrement("boom");
            }

            total().Should().Be(1);
        }

        [Fact]
        public void ARuleRemovedFromTheDefinitionsStopsCounting()
        {
            var name = UniqueName("removed");
            var cache = NewCache();
            cache.ApplyRules([(name, "x")]);
            cache.ApplyRules([]);
            var (listener, total) = Watch(name);

            using (listener)
            {
                cache.CheckAndIncrement("x");
            }

            total().Should().Be(0);
        }

        [Fact]
        public void InvalidBlankAndNullDefinitionsAreSkippedWithoutBlockingValidOnes()
        {
            var good = UniqueName("good");
            var cache = NewCache();

            cache.ApplyRules([("unit.bad", "("), ("", "x"), ("unit.blank", ""), (null!, "x"), (good, "ok")]);

            var (listener, total) = Watch(good);

            using (listener)
            {
                cache.CheckAndIncrement("ok");
            }

            total().Should().Be(1);
        }

        [Fact]
        public void NullAndEmptyTextAreIgnored()
        {
            var cache = NewCache();
            cache.ApplyRules([(UniqueName("empty"), ".*")]);

            var act = () =>
            {
                cache.CheckAndIncrement(null!);
                cache.CheckAndIncrement(string.Empty);
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void AnUnchangedRuleKeepsItsCompiledMatcherAcrossARefresh()
        {
            var name = UniqueName("kept");
            var cache = NewCache();
            var definitions = new List<(string, string)> { (name, "a+") };
            cache.ApplyRules(definitions);
            var (listener, total) = Watch(name);

            using (listener)
            {
                cache.CheckAndIncrement("a");
                cache.ApplyRules(definitions);
                cache.CheckAndIncrement("a");
                cache.ApplyRules([(name, "b+")]);
                cache.CheckAndIncrement("a");
                cache.CheckAndIncrement("b");
            }

            total().Should().Be(3);
        }

        [Fact]
        public void ARuleSuspendsAfterFiveConsecutiveTimeoutsAndResumesOnRefresh()
        {
            var rule = new LogCounterRule("unit.rule", "x", new Regex("x"));

            for (var i = 1; i < LogCounterRule.MaxConsecutiveTimeouts; i++)
            {
                rule.RecordTimeout().Should().Be(i);
                rule.IsSuspended.Should().BeFalse();
            }

            rule.RecordTimeout();
            rule.IsSuspended.Should().BeTrue();

            rule.Resume();
            rule.IsSuspended.Should().BeFalse();
        }

        [Fact]
        public void ACompletedMatchResetsTheConsecutiveTimeoutRun()
        {
            var rule = new LogCounterRule("unit.rule", "x", new Regex("x"));

            for (var i = 0; i < LogCounterRule.MaxConsecutiveTimeouts - 1; i++)
            {
                rule.RecordTimeout();
            }

            rule.RecordCompletion();
            rule.RecordTimeout();

            rule.IsSuspended.Should().BeFalse();
        }

        [Theory]
        [InlineData("(a+)+$")]
        [InlineData("(a|aa)+$")]
        [InlineData("^(([a-z])+.)+[A-Z]([a-z])+$")]
        [InlineData("(.*a){20}")]
        [InlineData("(?i)warn(ing)?")]
        [InlineData(@"\d{3,5}")]
        public void CatastrophicPatternsAreMatchedInLinearTime(string pattern)
        {
            LogCounterRegex.IsLinearTime(LogCounterRegex.Create(pattern)).Should().BeTrue();
        }

        [Theory]
        [InlineData("^(?!x).*$")]
        [InlineData("(?<=a)b")]
        [InlineData(@"(a)\1")]
        [InlineData("(?>a+)b")]
        public void PatternsNeedingBacktrackingFallBackToABoundedMatcher(string pattern)
        {
            var regex = LogCounterRegex.Create(pattern);

            LogCounterRegex.IsLinearTime(regex).Should().BeFalse();
            regex.MatchTimeout.Should().Be(LogCounterRegex.MatchTimeout);
        }

        [Theory]
        [InlineData("(")]
        [InlineData("[")]
        [InlineData("*")]
        [InlineData("a{2,1}")]
        [InlineData("[z-a]")]
        [InlineData("(?<n>a)\\k<m>")]
        public void InvalidPatternsAreRefused(string pattern)
        {
            var act = () => LogCounterRegex.Create(pattern);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EveryCreatedMatcherCarriesTheBoundedTimeout()
        {
            new[] { "a", "^(?!x).*$", "(a+)+$" }
                .Select(LogCounterRegex.Create)
                .Should().OnlyContain(r => r.MatchTimeout == LogCounterRegex.MatchTimeout);
        }
    }
}
