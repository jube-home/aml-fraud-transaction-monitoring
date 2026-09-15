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
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class GatewayRulesExtensionsTests
    {
        private static EntityModelGatewayRule NewRule(string name, double gatewaySample,
            EntityModelGatewayRule.Match match, double maxResponseElevation = 0)
        {
            return new EntityModelGatewayRule
            {
                EntityAnalysisModelGatewayRuleId = 1,
                Name = name,
                GatewaySample = gatewaySample,
                GatewayRuleCompileDelegate = match,
                MaxResponseElevation = maxResponseElevation
            };
        }

        private static Context NewContext(double randomDraw = 0.0, params EntityModelGatewayRule[] rules)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Collections.ModelGatewayRules.AddRange(rules);

            var payload = new DictionaryNoBoxing<string>();
            payload.Add("Currency", "AED");
            payload.Add("CurrencyAmount", 123.45);
            payload.Add("AccountId", "Test5");

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = payload,
                    Dictionary = new PooledDictionary<string, double>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Random = new FixedRandom(randomDraw),
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        [Fact]
        public void ARuleThatMatchesSetsMatchedGatewayRuleAndTheResponseElevationLimit()
        {
            var rule = NewRule("HighAmount", 1.0,
                (data, _, _, _) => (string)data["Currency"] == "AED", 42);
            var context = NewContext(0.0, rule);

            context.ExecuteGatewayRules();

            context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule.Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit.Should().Be(42);
        }

        [Fact]
        public void ARuleThatDoesNotMatchLeavesMatchedGatewayRuleFalse()
        {
            var rule = NewRule("NeverMatches", 1.0, (_, _, _, _) => false);
            var context = NewContext(0.0, rule);

            context.ExecuteGatewayRules();

            context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule.Should().BeFalse();
        }

        [Fact]
        public void ARuleIsSkippedEntirelyWhenTheRandomGatewaySampleIsAtOrAboveItsThreshold()
        {
            var wasEvaluated = false;
            var rule = NewRule("LowSample", 0.1, (_, _, _, _) =>
            {
                wasEvaluated = true;
                return true;
            });

            var context = NewContext(0.5, rule);

            context.ExecuteGatewayRules();

            wasEvaluated.Should().BeFalse();
            context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule.Should().BeFalse();
        }

        [Fact]
        public void ARuleRunsWhenTheRandomGatewaySampleIsBelowItsThreshold()
        {
            var wasEvaluated = false;
            var rule = NewRule("HighSample", 0.9, (_, _, _, _) =>
            {
                wasEvaluated = true;
                return false;
            });
            var context = NewContext(0.5, rule);

            context.ExecuteGatewayRules();

            wasEvaluated.Should().BeTrue();
        }

        [Fact]
        public void TheFirstMatchingRuleStopsEvaluationOfSubsequentRules()
        {
            var secondRuleEvaluated = false;
            var first = NewRule("First", 1.0, (_, _, _, _) => true, 10);
            var second = NewRule("Second", 1.0, (_, _, _, _) =>
            {
                secondRuleEvaluated = true;
                return true;
            }, 99);
            var context = NewContext(0.0, first, second);

            context.ExecuteGatewayRules();

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit.Should().Be(10);
            secondRuleEvaluated.Should().BeFalse();
        }

        [Fact]
        public void ARuleThatDoesNotMatchAllowsTheNextRuleToStillBeEvaluated()
        {
            var secondRuleEvaluated = false;
            var first = NewRule("First", 1.0, (_, _, _, _) => false);
            var second = NewRule("Second", 1.0, (_, _, _, _) =>
            {
                secondRuleEvaluated = true;
                return true;
            }, 5);
            var context = NewContext(0.0, first, second);

            context.ExecuteGatewayRules();

            secondRuleEvaluated.Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit.Should().Be(5);
        }

        [Fact]
        public void AnExceptionThrownByARuleIsCaughtAndSubsequentRulesStillRun()
        {
            var secondRuleEvaluated = false;
            var throwingRule = NewRule("Throws", 1.0,
                (_, _, _, _) => throw new InvalidOperationException("boom"));
            var second = NewRule("Second", 1.0, (_, _, _, _) =>
            {
                secondRuleEvaluated = true;
                return false;
            });
            var context = NewContext(0.0, throwingRule, second);

            var act = context.ExecuteGatewayRules;

            act.Should().NotThrow();
            secondRuleEvaluated.Should().BeTrue();
        }

        [Fact]
        public void EvaluationCounterIncrementsForEveryRuleTestedRegardlessOfMatch()
        {
            var rule = NewRule("Counted", 1.0, (_, _, _, _) => false);
            var context = NewContext(0.0, rule);

            context.ExecuteGatewayRules();
            context.ExecuteGatewayRules();

            rule.EvaluationCounter.Should().Be(2);
        }

        [Fact]
        public void ActivationCounterOnlyIncrementsWhenTheRuleActuallyMatches()
        {
            var matching = NewRule("Matches", 1.0, (_, _, _, _) => true);
            var nonMatching = NewRule("DoesNotMatch", 1.0, (_, _, _, _) => false);
            var context = NewContext(0.0, matching, nonMatching);

            context.ExecuteGatewayRules();

            matching.ActivationCounter.Should().Be(1);
            nonMatching.ActivationCounter.Should().Be(0);
        }

        [Fact]
        public void ModelInvokeGatewayCounterIncrementsOnTheModelWhenARuleMatches()
        {
            var rule = NewRule("Matches", 1.0, (_, _, _, _) => true);
            var context = NewContext(0.0, rule);

            context.ExecuteGatewayRules();

            context.EntityAnalysisModel.Counters.ModelInvokeGatewayCounter.Should().Be(1);
        }

        [Fact]
        public void WhenSampledTheGatewayStageTimingIsPopulatedWithOneItemPerRuleTested()
        {
            var matching = NewRule("Matches", 1.0, (_, _, _, _) => true);
            var context = NewContext(0.0, matching);

            context.ExecuteGatewayRules();

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            stages.Should().NotBeNull();
            stages!.Gateway.Should().NotBeNull();
            stages.Gateway!.Items.Should().ContainKey("Matches");
        }

        [Fact]
        public void WhenNotSampledNoStageTimingIsBuiltAtAll()
        {
            var rule = NewRule("Rule", 1.0, (_, _, _, _) => true);
            var context = NewContext(0.0, rule);
            context.LogSampled = false;

            context.ExecuteGatewayRules();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule.Should().BeTrue();
        }

        [Fact]
        public void WithNoRulesConfiguredAtAllNothingMatchesAndNoExceptionIsThrown()
        {
            var context = NewContext();

            var act = context.ExecuteGatewayRules;

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule.Should().BeFalse();
        }

        private sealed class FixedRandom(double fixedNextDouble) : Random
        {
            public override double NextDouble()
            {
                return fixedNextDouble;
            }
        }
    }
}