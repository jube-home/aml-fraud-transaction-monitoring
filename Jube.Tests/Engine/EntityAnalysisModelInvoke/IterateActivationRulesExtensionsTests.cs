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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.HttpAdaptationProtocol;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class IterateActivationRulesExtensionsTests
    {
        private static readonly Dictionary<int, EntityAnalysisModel> noAvailableModels = new();

        private static Context NewContext(double randomDraw = 0.1)
        {
            return new Context
            {
                EntityAnalysisModel = new EntityAnalysisModel(),
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Activation = new PooledDictionary<string, EntityModelActivationRulePayload>(),
                    TtlCounter = new PooledDictionary<string, double>(),
                    Dictionary = new PooledDictionary<string, double>(),
                    Abstraction = new PooledDictionary<string, double>(),
                    AbstractionCalculation = new PooledDictionary<string, double>(),
                    Sanction = new PooledDictionary<string, double>(),
                    HttpAdaptation = new PooledDictionary<string, Adaptation>(),
                    ExhaustiveAdaptation = new PooledDictionary<string, double>(),
                    ArchiveKeys = [],
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
                },
                Random = new FixedRandom(randomDraw),
                Environment = TestDynamicEnvironment.Create(),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };
        }

        private static EntityAnalysisModelActivationRule NewRule(string name, bool matched, bool visible = true,
            double activationSample = 1, bool reportTable = false, bool enableCaseWorkflow = false,
            bool enableReprocessing = true)
        {
            return new EntityAnalysisModelActivationRule
            {
                Id = Math.Abs(name.GetHashCode()) % 100000,
                Guid = Guid.NewGuid(),
                Name = name,
                Visible = visible,
                ActivationSample = activationSample,
                ReportTable = reportTable,
                EnableCaseWorkflow = enableCaseWorkflow,
                EnableReprocessing = enableReprocessing,
                CaseWorkflowGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                ActivationRuleCompileDelegate = (_, _, _, _, _, _, _, _, _, _, _) => matched
            };
        }

        [Fact]
        public async Task ReturnsZeroCountAndNoCaseWhenThereAreNoActivationRulesAsync()
        {
            var context = NewContext();
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, createCase, prevailingActivationRuleId, items) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
            createCase.Should().BeNull();
            prevailingActivationRuleId.Should().BeNull();
            items.Should().BeEmpty();
        }

        [Fact]
        public async Task ARuleThatDoesNotMatchIsNotAddedToActivationsAndDoesNotCountAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(NewRule("NoMatch", false));
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, prevailingActivationRuleId, items) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
            prevailingActivationRuleId.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.Activation.Should().BeEmpty();
            items.Should().ContainKey("NoMatch");
        }

        [Fact]
        public async Task AMatchedVisibleRuleIsAddedToActivationsAndBecomesPrevailingAsync()
        {
            var context = NewContext();
            var rule = NewRule("HighAmount", true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, prevailingActivationRuleId, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(1);
            prevailingActivationRuleId.Should().Be(rule.Id);
            context.EntityAnalysisModelInstanceEntryPayload.Activation.Should().ContainKey("HighAmount");
        }

        [Fact]
        public async Task AMatchedButNotVisibleRuleIsAddedToActivationsButDoesNotCountOrBecomePrevailingAsync()
        {
            var context = NewContext();
            var rule = NewRule("Hidden", true, false);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, prevailingActivationRuleId, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
            prevailingActivationRuleId.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.Activation.Should().ContainKey("Hidden");
        }

        [Fact]
        public async Task TheLastMatchedVisibleRuleInIterationOrderBecomesPrevailingAsync()
        {
            var context = NewContext();
            var first = NewRule("First", true);
            var second = NewRule("Second", true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(first);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(second);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, prevailingActivationRuleId, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(2);
            prevailingActivationRuleId.Should().Be(second.Id);
        }

        [Fact]
        public async Task ARuleThatFailsActivationSamplingIsSkippedEntirelyAsync()
        {
            var context = NewContext(0.99);
            var rule = NewRule("RareRule", true, activationSample: 0.01);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, _, items) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
            context.EntityAnalysisModelInstanceEntryPayload.Activation.Should().BeEmpty();
            rule.EvaluationCounter.Should().Be(0);

            items.Should().ContainKey("RareRule");
            items["RareRule"].ResponseElevation.Should().BeNull();
            items["RareRule"].Notification.Should().BeNull();
            items["RareRule"].CaseCreation.Should().BeNull();
        }

        [Fact]
        public async Task ARuleSuppressedAtTheModelLevelIsStillEvaluatedButNotCountedTowardsPrevailingAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath { Name = "Country", EnableSuppression = true });
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels =
                new Dictionary<string, List<string>> { ["Country"] = ["IR"] };
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "IR");

            var rule = NewRule("SanctionedCountryRule", true, reportTable: true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, prevailingActivationRuleId, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
            prevailingActivationRuleId.Should().BeNull();

            context.EntityAnalysisModelInstanceEntryPayload.Activation.Should().ContainKey("SanctionedCountryRule");
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should()
                .ContainSingle(k => k.Key == "SanctionedCountryRule");
        }

        [Fact]
        public async Task ARuleSuppressedAtTheRuleLevelViaAnotherModelsSuppressionListIsNotCountedAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath { Name = "Country", EnableSuppression = true });
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels =
                new Dictionary<string, List<string>> { ["Country"] = [] };
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionRules["Country"] =
                new Dictionary<string, List<string>> { ["IR"] = ["TargetedRule"] };
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "IR");

            var rule = NewRule("TargetedRule", true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, _, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
        }

        [Fact]
        public async Task ARuleDisabledForReprocessingIsSuppressedDuringAReprocessingRunAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId = 99;
            var rule = NewRule("NoReprocessRule", true, enableReprocessing: false);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, _, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
        }

        [Fact]
        public async Task AddsAnArchiveKeyWhenReportTableIsEnabledOnAMatchedRuleAsync()
        {
            var context = NewContext();
            var rule = NewRule("ReportedRule", true, reportTable: true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Single();
            archiveKey.Key.Should().Be("ReportedRule");
            archiveKey.KeyValueBoolean.Should().Be(1);
            archiveKey.ProcessingTypeId.Should().Be(11);
        }

        [Fact]
        public async Task DoesNotAddAnArchiveKeyWhenReportTableIsDisabledAsync()
        {
            var context = NewContext();
            var rule = NewRule("UnreportedRule", true, reportTable: false);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().BeEmpty();
        }

        [Fact]
        public async Task TwoRulesSharingTheSameNameOnlyRecordTheFirstMatchAsync()
        {
            var context = NewContext();
            var first = NewRule("DuplicateName", true, reportTable: true);
            var second = NewRule("DuplicateName", true, reportTable: true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(first);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(second);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, prevailingActivationRuleId, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(1);
            prevailingActivationRuleId.Should().Be(first.Id);
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle();
        }

        [Fact]
        public async Task AnInlineScriptOverrideCanFlipANonMatchingRuleToMatchedAsync()
        {
            var context = NewContext();
            var rule = NewRule("OverriddenRule", false);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);

            var script = new EntityAnalysisModelInlineScript
            {
                Id = 1,
                InlineScriptCode = "Script1",
                ActivatorDelegate = () => new object(),
                ExecuteAsyncDelegate = (_, _) => Task.FromResult(true)
            };
            script.EntityAnalysisModelInlineScriptEvents.Add(new EntityAnalysisModelInlineScriptEvent
            {
                EntityAnalysisModelInlineScriptEventType =
                    EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride,
                Guid = rule.Guid,
                Name = rule.Name
            });
            context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(script);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, _, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(1);
            context.EntityAnalysisModelInstanceEntryPayload.Activation.Should().ContainKey("OverriddenRule");
        }

        [Fact]
        public async Task AnInlineScriptThatReturnsFalseDoesNotFlipAMatchAsync()
        {
            var context = NewContext();
            var rule = NewRule("StillNoMatch", false);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);

            var script = new EntityAnalysisModelInlineScript
            {
                Id = 1,
                InlineScriptCode = "Script1",
                ActivatorDelegate = () => new object(),
                ExecuteAsyncDelegate = (_, _) => Task.FromResult(false)
            };
            script.EntityAnalysisModelInlineScriptEvents.Add(new EntityAnalysisModelInlineScriptEvent
            {
                EntityAnalysisModelInlineScriptEventType =
                    EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride,
                Guid = rule.Guid,
                Name = rule.Name
            });
            context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(script);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, _, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(0);
            context.EntityAnalysisModelInstanceEntryPayload.Activation.Should().BeEmpty();
        }

        [Fact]
        public async Task OnlyTheFirstMatchedRuleWithCaseWorkflowEnabledCreatesACaseAsync()
        {
            var context = NewContext();
            var first = NewRule("CaseRuleOne", true, enableCaseWorkflow: true);
            var second = NewRule("CaseRuleTwo", true, enableCaseWorkflow: true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(first);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(second);
            var cacheService = TestCacheService.Create(out _);

            var (_, createCase, _, _) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            createCase.Should().NotBeNull();
            createCase!.CaseWorkflowGuid.Should().Be(first.CaseWorkflowGuid);
        }

        [Fact]
        public async Task ATtlCounterIsStillIncrementedForAMatchedRuleEvenWhenSuppressedAtTheModelLevelAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath { Name = "Country", EnableSuppression = true });
            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels =
                new Dictionary<string, List<string>> { ["Country"] = ["IR"] };
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Country", "IR");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "1");

            var targetModel = new EntityAnalysisModel
            {
                Instance =
                {
                    Guid = Guid.NewGuid()
                },
                Flags =
                {
                    EnableTtlCounter = true
                }
            };

            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "TxnCount",
                TtlCounterDataName = "CurrencyAmount",
                EnableLiveForever = true
            };

            targetModel.Collections.ModelTtlCounters.Add(counter);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [1] = targetModel };

            var rule = NewRule("SuppressedTtlRule", true);
            rule.EnableTtlCounter = true;
            rule.EntityAnalysisModelGuidTtlCounter = targetModel.Instance.Guid;
            rule.EntityAnalysisModelTtlCounterGuid = counter.Guid;
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, _, _) =
                await context.IterateAndProcessAsync(cacheService, availableModels, null);

            activationRuleCount.Should().Be(0, "the rule is suppressed at the model level");
            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().ContainKey("TxnCount");
        }

        [Fact]
        public async Task ARuleThatThrowsDuringProcessingIsLoggedAndDoesNotStopSubsequentRulesAsync()
        {
            var context = NewContext();
            var failing = NewRule("FailingRule", true);
            failing.ActivationRuleCompileDelegate = (_, _, _, _, _, _, _, _, _, _, _) =>
                throw new InvalidOperationException("boom");
            var healthy = NewRule("HealthyRule", true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(failing);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(healthy);
            var cacheService = TestCacheService.Create(out _);

            var (activationRuleCount, _, prevailingActivationRuleId, items) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            activationRuleCount.Should().Be(1);
            prevailingActivationRuleId.Should().Be(healthy.Id);
            items.Should().ContainKey("FailingRule");
            items.Should().ContainKey("HealthyRule");
        }

        [Fact]
        public async Task EveryEvaluatedRuleGetsATimingEntryWithRuleDurationAsync()
        {
            var context = NewContext();
            var rule = NewRule("TimedRule", true);
            context.EntityAnalysisModel.Collections.ModelActivationRules.Add(rule);
            var cacheService = TestCacheService.Create(out _);

            var (_, _, _, items) =
                await context.IterateAndProcessAsync(cacheService, noAvailableModels, null);

            items.Should().ContainKey("TimedRule");
            items["TimedRule"].Rule.Should().NotBeNull();
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