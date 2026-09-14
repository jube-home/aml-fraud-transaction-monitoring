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
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using EntityAnalysisModelAbstractionRule =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionRule;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ExecuteAbstractionRulesWithoutSearchKeysExtensionsTests
    {
        private static EntityAnalysisModelAbstractionRule NewRule(string name,
            EntityAnalysisModelAbstractionRule.Match match,
            bool search = false, bool reportTable = false)
        {
            return new EntityAnalysisModelAbstractionRule
            {
                Id = 1,
                Name = name,
                Search = search,
                ReportTable = reportTable,
                AbstractionRuleCompileDelegate = match
            };
        }

        private static Context NewContext(params EntityAnalysisModelAbstractionRule[] rules)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Collections.ModelAbstractionRules.AddRange(rules);

            var payload = new DictionaryNoBoxing<string>();
            payload.Add("Currency", "AED");
            payload.Add("CurrencyAmount", 123.45);

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = payload,
                    Dictionary = new PooledDictionary<string, double>(),
                    Abstraction = new PooledDictionary<string, double>(),
                    ArchiveKeys = [],
                    InvokeTaskPerformance = new InvokeTaskPerformance(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
                },
                Random = new Random(),
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        [Fact]
        public void ARuleThatMatchesAddsOneToTheAbstractionDictionary()
        {
            var rule = NewRule("HighAmount", (data, _, _, _) => (string)data["Currency"] == "AED");
            var context = NewContext(rule);

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["HighAmount"].Should().Be(1);
        }

        [Fact]
        public void ARuleThatDoesNotMatchAddsZeroToTheAbstractionDictionary()
        {
            var rule = NewRule("NeverMatches", (_, _, _, _) => false);
            var context = NewContext(rule);

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["NeverMatches"].Should().Be(0);
        }

        [Fact]
        public void ARuleFlaggedAsSearchIsSkippedEntirelyByThisExtension()
        {
            var wasEvaluated = false;
            var searchRule = NewRule("SearchRule", (_, _, _, _) =>
            {
                wasEvaluated = true;
                return true;
            }, true);
            var context = NewContext(searchRule);

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            wasEvaluated.Should().BeFalse();
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction.ContainsKey("SearchRule").Should().BeFalse();
        }

        [Fact]
        public void MultipleNonSearchRulesAllRunIndependently()
        {
            var first = NewRule("First", (_, _, _, _) => true);
            var second = NewRule("Second", (_, _, _, _) => false);
            var context = NewContext(first, second);

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["First"].Should().Be(1);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Second"].Should().Be(0);
        }

        [Fact]
        public void AnExceptionThrownByARuleIsCaughtAndSubsequentRulesStillRun()
        {
            var secondEvaluated = false;
            var throwing = NewRule("Throws", (_, _, _, _) => throw new InvalidOperationException("boom"));
            var second = NewRule("Second", (_, _, _, _) =>
            {
                secondEvaluated = true;
                return true;
            });
            var context = NewContext(throwing, second);

            var act = context.ExecuteAbstractionRulesWithoutSearchKeys;

            act.Should().NotThrow();
            secondEvaluated.Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction.ContainsKey("Throws").Should().BeFalse();
        }

        [Fact]
        public void AReportTableRuleAddsAnArchiveKeyWithTheAbstractionValue()
        {
            var rule = NewRule("Reported", (_, _, _, _) => true, reportTable: true);
            var context = NewContext(rule);

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle();
            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys[0];
            archiveKey.Key.Should().Be("Reported");
            archiveKey.KeyValueFloat.Should().Be(1);
            archiveKey.ProcessingTypeId.Should().Be(5);
        }

        [Fact]
        public void ARuleWithoutReportTableDoesNotAddAnArchiveKey()
        {
            var rule = NewRule("NotReported", (_, _, _, _) => true, reportTable: false);
            var context = NewContext(rule);

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().BeEmpty();
        }

        [Fact]
        public void WhenSampledTheStageTimingRecordsOneItemPerNonSearchRule()
        {
            var rule = NewRule("Rule", (_, _, _, _) => true);
            var context = NewContext(rule);

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            stages.Should().NotBeNull();
            stages!.AbstractionRulesWithoutSearchKeys.Should().NotBeNull();
            stages.AbstractionRulesWithoutSearchKeys!.Items.Should().ContainKey("Rule");
        }

        [Fact]
        public void WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns()
        {
            var rule = NewRule("Rule", (_, _, _, _) => true);
            var context = NewContext(rule);
            context.LogSampled = false;

            context.ExecuteAbstractionRulesWithoutSearchKeys();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Rule"].Should().Be(1);
        }

        [Fact]
        public void WithNoRulesConfiguredNothingThrowsAndAbstractionStaysEmpty()
        {
            var context = NewContext();

            var act = context.ExecuteAbstractionRulesWithoutSearchKeys;

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction.Should().BeEmpty();
        }
    }
}