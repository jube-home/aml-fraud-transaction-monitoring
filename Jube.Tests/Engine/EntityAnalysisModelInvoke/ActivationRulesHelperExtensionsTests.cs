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
using System.Diagnostics;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models.CaseManagement;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ActivationRulesHelperExtensionsTests
    {
        private static Context NewContext()
        {
            return new Context
            {
                EntityAnalysisModel = new EntityAnalysisModel(),
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>()
                },
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };
        }

        private static EntityAnalysisModelActivationRule NewRule(int id = 1, bool visible = true,
            bool enableResponseElevation = true, double responseElevation = 0)
        {
            return new EntityAnalysisModelActivationRule
            {
                Id = id,
                Visible = visible,
                EnableResponseElevation = enableResponseElevation,
                ResponseElevation = responseElevation
            };
        }

        [Fact]
        public void ActivationRuleFinishResponseElevationIncrementsCounterWhenElevationIsPositive()
        {
            var context = NewContext();

            context.ActivationRuleFinishResponseElevation(5);

            context.EntityAnalysisModel.Counters.ModelResponseElevationCounter.Should().Be(1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ActivationRuleFinishResponseElevationDoesNotIncrementCounterWhenElevationIsZeroOrNegative(
            double responseElevation)
        {
            var context = NewContext();

            context.ActivationRuleFinishResponseElevation(responseElevation);

            context.EntityAnalysisModel.Counters.ModelResponseElevationCounter.Should().Be(0);
        }

        [Fact]
        public void ActivationRuleResponseElevationAddToCountersIncrementsAndJournalsWhenElevationIsPositive()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value = 10;

            context.ActivationRuleResponseElevationAddToCounters();

            context.EntityAnalysisModel.Counters.ResponseElevationCount.Should().Be(1);
            context.EntityAnalysisModel.ConcurrentQueues.BillingResponseElevationJournal.Should().ContainSingle();
        }

        [Fact]
        public void ActivationRuleResponseElevationAddToCountersDoesNothingWhenElevationIsZero()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value = 0;

            context.ActivationRuleResponseElevationAddToCounters();

            context.EntityAnalysisModel.Counters.ResponseElevationCount.Should().Be(0);
            context.EntityAnalysisModel.ConcurrentQueues.BillingResponseElevationJournal.Should().BeEmpty();
        }

        [Fact]
        public void UpdateContextStateWithActivationRulesOutcomeSetsAllThreeFieldsOnThePayload()
        {
            var context = NewContext();
            var createCase = new CreateCase();

            context.UpdateContextStateWithActivationRulesOutcome(3, 42, createCase);

            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelActivationRuleCount.Should().Be(3);
            context.EntityAnalysisModelInstanceEntryPayload.PrevailingEntityAnalysisModelActivationRuleId.Should()
                .Be(42);
            context.EntityAnalysisModelInstanceEntryPayload.CreateCase.Should().BeSameAs(createCase);
        }

        [Fact]
        public void UpdateContextStateWithActivationRulesOutcomeAllowsANullPrevailingId()
        {
            var context = NewContext();

            context.UpdateContextStateWithActivationRulesOutcome(0, null, null);

            context.EntityAnalysisModelInstanceEntryPayload.PrevailingEntityAnalysisModelActivationRuleId.Should()
                .BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.CreateCase.Should().BeNull();
        }

        [Fact]
        public void CheckSuppressedResponseElevationIsFalseWhenQueueCountIsAtOrBelowTheFrequencyLimitCounter()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.ResponseElevationFrequencyLimitCounter = 5;

            context.CheckSuppressedResponseElevation().Should().BeFalse();
        }

        [Fact]
        public void CheckSuppressedResponseElevationIsTrueWhenQueueCountExceedsTheFrequencyLimitCounter()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.ResponseElevationFrequencyLimitCounter = 0;
            context.EntityAnalysisModel.ConcurrentQueues.ResponseElevationEntries.Enqueue(new ResponseElevation());
            context.EntityAnalysisModel.ConcurrentQueues.ResponseElevationEntries.Enqueue(new ResponseElevation());

            context.CheckSuppressedResponseElevation().Should().BeTrue();
        }

        [Fact]
        public void ProcessResponseElevationDoesNothingWhenSuppressed()
        {
            var context = NewContext();
            var rule = NewRule(responseElevation: 50);
            double highWaterMark = 0;

            context.ProcessResponseElevation(rule, ref highWaterMark, true);

            highWaterMark.Should().Be(0);
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value.Should().Be(0);
        }

        [Fact]
        public void ProcessResponseElevationDoesNothingWhenResponseElevationIsDisabledOnTheRule()
        {
            var context = NewContext();
            var rule = NewRule(enableResponseElevation: false, responseElevation: 50);
            double highWaterMark = 0;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            highWaterMark.Should().Be(0);
        }

        [Fact]
        public void ProcessResponseElevationDoesNothingWhenTheRulesElevationDoesNotExceedTheCurrentHighWaterMark()
        {
            var context = NewContext();
            var rule = NewRule(responseElevation: 10);
            double highWaterMark = 20;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            highWaterMark.Should().Be(20);
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value.Should().Be(0);
        }

        [Fact]
        public void ProcessResponseElevationRaisesTheHighWaterMarkAndCarriesTheValueForwardWithinAllLimits()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxResponseElevation = 1000;
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit = 1000;
            var rule = NewRule(responseElevation: 30);
            double highWaterMark = 10;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            highWaterMark.Should().Be(30);
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value.Should().Be(30);
        }

        [Fact]
        public void ProcessResponseElevationTruncatesToTheModelsMaxResponseElevationAndIncrementsThatCounter()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxResponseElevation = 25;
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit = 1000;
            var rule = NewRule(responseElevation: 100);
            double highWaterMark = 0;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value.Should().Be(25);
            context.EntityAnalysisModel.Counters.ResponseElevationValueLimitCounter.Should().Be(1);
        }

        [Fact]
        public void ProcessResponseElevationTruncatesToTheGatewaysResponseElevationLimitAndIncrementsThatCounter()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxResponseElevation = 1000;
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit = 15;
            var rule = NewRule(responseElevation: 100);
            double highWaterMark = 0;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value.Should().Be(15);
            context.EntityAnalysisModel.Counters.ResponseElevationFrequencyLimitCounter.Should().Be(1);
        }

        [Fact]
        public void ProcessResponseElevationCopiesContentRedirectAndColoursFromTheRule()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxResponseElevation = 1000;
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit = 1000;
            var rule = NewRule(responseElevation: 10);
            rule.ResponseElevationContent = "Blocked";
            rule.ResponseElevationRedirect = "https://example.test/blocked";
            rule.ResponseElevationForeColor = "#000000";
            rule.ResponseElevationBackColor = "#ff0000";
            double highWaterMark = 0;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            var elevation = context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation;
            elevation.Content.Should().Be("Blocked");
            elevation.Redirect.Should().Be("https://example.test/blocked");
            elevation.ForeColor.Should().Be("#000000");
            elevation.BackColor.Should().Be("#ff0000");
        }

        [Fact]
        public void ProcessResponseElevationEnqueuesAJournalEntryOnlyWhenTheLimitFlagIsEnabled()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxResponseElevation = 1000;
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit = 1000;
            context.EntityAnalysisModel.Flags.EnableResponseElevationLimit = true;
            var rule = NewRule(responseElevation: 10);
            double highWaterMark = 0;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            context.EntityAnalysisModel.ConcurrentQueues.ResponseElevationEntries.Should().ContainSingle();
        }

        [Fact]
        public void ProcessResponseElevationDoesNotEnqueueAJournalEntryWhenTheLimitFlagIsDisabled()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxResponseElevation = 1000;
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit = 1000;
            context.EntityAnalysisModel.Flags.EnableResponseElevationLimit = false;
            var rule = NewRule(responseElevation: 10);
            double highWaterMark = 0;

            context.ProcessResponseElevation(rule, ref highWaterMark, false);

            context.EntityAnalysisModel.ConcurrentQueues.ResponseElevationEntries.Should().BeEmpty();
        }

        [Fact]
        public void ActivationRuleCountsAndArchiveHighWatermarkDoesNothingWhenSuppressed()
        {
            var context = NewContext();
            var rule = NewRule(7);
            var count = 0;
            int? prevailingId = null;
            string prevailingName = null!;

            context.ActivationRuleCountsAndArchiveHighWatermark(rule, true, ref count, ref prevailingId,
                ref prevailingName);

            count.Should().Be(0);
            prevailingId.Should().BeNull();
        }

        [Fact]
        public void ActivationRuleCountsAndArchiveHighWatermarkIncrementsCountAndSetsPrevailingWhenVisible()
        {
            var context = NewContext();
            var rule = NewRule(7);
            rule.Name = "Rule7";
            var count = 0;
            int? prevailingId = null;
            string prevailingName = null!;

            context.ActivationRuleCountsAndArchiveHighWatermark(rule, false, ref count, ref prevailingId,
                ref prevailingName);

            count.Should().Be(1);
            prevailingId.Should().Be(7);
            prevailingName.Should().Be("Rule7");
        }

        [Fact]
        public void ActivationRuleCountsAndArchiveHighWatermarkDoesNotIncrementCountWhenNotVisible()
        {
            var context = NewContext();
            var rule = NewRule(7, false);
            var count = 0;
            int? prevailingId = null;
            string prevailingName = null!;

            context.ActivationRuleCountsAndArchiveHighWatermark(rule, false, ref count, ref prevailingId,
                ref prevailingName);

            count.Should().Be(0);
            prevailingId.Should().BeNull();
            prevailingName.Should().BeNull();
        }

        [Fact]
        public void ActivationRuleCountsAndArchiveHighWatermarkAccumulatesAcrossMultipleVisibleRules()
        {
            var context = NewContext();
            var first = NewRule();
            var second = NewRule(2);
            var count = 0;
            int? prevailingId = null;
            string prevailingName = null!;

            context.ActivationRuleCountsAndArchiveHighWatermark(first, false, ref count, ref prevailingId,
                ref prevailingName);
            context.ActivationRuleCountsAndArchiveHighWatermark(second, false, ref count, ref prevailingId,
                ref prevailingName);

            count.Should().Be(2);
            prevailingId.Should().Be(2);
        }

        [Fact]
        public void ActivationRuleGetSuppressedModelReturnsFalseWhenNoSuppressionXPathsAreConfigured()
        {
            var context = NewContext();
            var suppressedRules = new List<string>();

            var result = context.ActivationRuleGetSuppressedModel(ref suppressedRules);

            result.Should().BeFalse();
        }

        [Fact]
        public void ActivationRuleGetSuppressedModelReturnsFalseWhenTheSuppressionKeyIsNotInThePayload()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "Country",
                    EnableSuppression = true
                });
            var suppressedRules = new List<string>();

            var result = context.ActivationRuleGetSuppressedModel(ref suppressedRules);

            result.Should().BeFalse();
        }

        [Fact]
        public void ActivationRuleGetSuppressedModelReturnsTrueWhenThePayloadValueIsInTheSuppressionList()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "Country",
                    EnableSuppression = true
                });
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels =
                new Dictionary<string, List<string>>
                {
                    ["Country"] = new() { "IR", "KP" }
                };
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "IR");
            var suppressedRules = new List<string>();

            var result = context.ActivationRuleGetSuppressedModel(ref suppressedRules);

            result.Should().BeTrue();
        }

        [Fact]
        public void ActivationRuleGetSuppressedModelReturnsFalseWhenThePayloadValueIsNotInTheSuppressionList()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "Country",
                    EnableSuppression = true
                });
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels =
                new Dictionary<string, List<string>>
                {
                    ["Country"] = new() { "IR", "KP" }
                };
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "GB");
            var suppressedRules = new List<string>();

            var result = context.ActivationRuleGetSuppressedModel(ref suppressedRules);

            result.Should().BeFalse();
        }

        [Fact]
        public void ActivationRuleGetSuppressedModelPopulatesSuppressedActivationRulesWhenAMatchIsConfigured()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "Country",
                    EnableSuppression = true
                });
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels =
                new Dictionary<string, List<string>>
                {
                    ["Country"] = new() { "IR" }
                };
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionRules["Country"] =
                new Dictionary<string, List<string>>
                {
                    ["IR"] = new() { "RuleA", "RuleB" }
                };
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "IR");
            var suppressedRules = new List<string>();

            context.ActivationRuleGetSuppressedModel(ref suppressedRules);

            suppressedRules.Should().Equal("RuleA", "RuleB");
        }

        [Fact]
        public void ActivationRuleGetSuppressedModelIgnoresXPathsThatDoNotHaveSuppressionEnabled()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "Country",
                    EnableSuppression = false
                });
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels =
                new Dictionary<string, List<string>>
                {
                    ["Country"] = new() { "IR" }
                };
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "IR");
            var suppressedRules = new List<string>();

            var result = context.ActivationRuleGetSuppressedModel(ref suppressedRules);

            result.Should().BeFalse();
        }
    }
}