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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ActivationRuleCreateCaseObjectExtensionsTests
    {
        private static Context NewContext(double randomDraw = 0.5,
            IReadOnlyDictionary<string, string>? environmentOverrides = null)
        {
            return new Context
            {
                EntityAnalysisModel = new EntityAnalysisModel(),
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    EntityInstanceEntryId = "fallback-entity-id"
                },
                Random = new FixedRandom(randomDraw),
                Environment = TestDynamicEnvironment.Create(environmentOverrides),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };
        }

        private static EntityAnalysisModelActivationRule NewRule(bool enableCaseWorkflow = true,
            double bypassSuspendSample = 0, char bypassSuspendInterval = 'n', int bypassSuspendValue = 30,
            string? caseKey = null)
        {
            return new EntityAnalysisModelActivationRule
            {
                EnableCaseWorkflow = enableCaseWorkflow,
                CaseWorkflowGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                BypassSuspendSample = bypassSuspendSample,
                BypassSuspendInterval = bypassSuspendInterval,
                BypassSuspendValue = bypassSuspendValue,
                CaseKey = caseKey
            };
        }

        [Fact]
        public async Task ReturnsNullWhenCaseWorkflowIsDisabledAsync()
        {
            var context = NewContext();
            var rule = NewRule(false);

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ReturnsNullWhenSuppressedAsync()
        {
            var context = NewContext();
            var rule = NewRule();

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, true, null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task PopulatesTenantAndCaseWorkflowIdentifiersFromTheRuleAsync()
        {
            var context = NewContext();
            var rule = NewRule();
            context.EntityAnalysisModel.Instance.TenantRegistryId = 77;

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.TenantRegistryId.Should().Be(77);
            result.EntityAnalysisModelInstanceEntryGuid.Should()
                .Be(context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid);
            result.CaseWorkflowGuid.Should().Be(rule.CaseWorkflowGuid);
            result.CaseWorkflowStatusGuid.Should().Be(rule.CaseWorkflowStatusGuid);
        }

        [Theory]
        [InlineData('n', 30)]
        [InlineData('h', 4)]
        [InlineData('d', 2)]
        [InlineData('m', 1)]
        public async Task SelectsSuspendBypassAndComputesTheDateForEachIntervalAsync(
            char interval, int value)
        {
            var context = NewContext(0.1);
            var rule = NewRule(bypassSuspendSample: 1, bypassSuspendInterval: interval, bypassSuspendValue: value);
            var before = DateTime.UtcNow;

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.SuspendBypass.Should().BeTrue();
            var expected = interval switch
            {
                'n' => before.AddMinutes(value),
                'h' => before.AddHours(value),
                'd' => before.AddDays(value),
                'm' => before.AddMonths(value),
                _ => before
            };
            result.SuspendBypassDate.Should().BeCloseTo(expected, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DoesNotSetASuspendBypassDateForAnUnrecognisedIntervalAsync()
        {
            var context = NewContext(0.1);
            var rule = NewRule(bypassSuspendSample: 1, bypassSuspendInterval: 'x');

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.SuspendBypass.Should().BeTrue();
            result.SuspendBypassDate.Should().Be(default);
        }

        [Fact]
        public async Task OpensImmediatelyWhenTheBypassSampleDrawFailsAsync()
        {
            var context = NewContext(0.99);
            var rule = NewRule(bypassSuspendSample: 0);
            var before = DateTime.UtcNow;

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.SuspendBypass.Should().BeFalse();
            result.SuspendBypassDate.Should().BeOnOrAfter(before);
        }

        [Fact]
        public async Task UsesTheRulesCaseKeyAndPayloadValueWhenTheCaseKeyIsPresentAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "Test5");
            var rule = NewRule(caseKey: "AccountId");

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.CaseKey.Should().Be("AccountId");
            result.CaseKeyValue.Should().Be("Test5");
        }

        [Fact]
        public async Task FallsBackToTheEntityInstanceEntryIdWhenTheCaseKeyIsNullAsync()
        {
            var context = NewContext();
            var rule = NewRule(caseKey: null);

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.CaseKey.Should().BeNull();
            result.CaseKeyValue.Should().Be("fallback-entity-id");
        }

        [Fact]
        public async Task FallsBackToTheEntityInstanceEntryIdWhenTheCaseKeyIsNotInThePayloadAsync()
        {
            var context = NewContext();
            var rule = NewRule(caseKey: "MissingField");

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.CaseKey.Should().BeNull();
            result.CaseKeyValue.Should().Be("fallback-entity-id");
        }

        [Fact]
        public async Task SkipsTheIdempotencyCacheEntirelyWhenActivationRuleIdempotencyIsDisabledAsync()
        {
            var context = NewContext();
            var rule = NewRule();

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, null);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task ReturnsNullWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationRuleIdempotency"] = "True"
            });
            var cacheService = TestCacheService.Create(out _);
            var rule = NewRule();
            rule.Guid = Guid.NewGuid();

            var first = await context.ActivationRuleCreateCaseObjectAsync(rule, false, cacheService);
            var second = await context.ActivationRuleCreateCaseObjectAsync(rule, false, cacheService);

            first.Should().NotBeNull();
            second.Should().BeNull();
        }

        [Fact]
        public async Task CreatesTheCaseWhenIdempotencyIsEnabledAndTheClaimSucceedsAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationRuleIdempotency"] = "True"
            });
            var cacheService = TestCacheService.Create(out _);
            var rule = NewRule();
            rule.Guid = Guid.NewGuid();

            var result = await context.ActivationRuleCreateCaseObjectAsync(rule, false, cacheService);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task
            DifferentActivationRulesClaimIndependentIdempotencySlotsForTheSameEntryAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationRuleIdempotency"] = "True"
            });
            var cacheService = TestCacheService.Create(out _);
            var ruleOne = NewRule();
            ruleOne.Guid = Guid.NewGuid();
            var ruleTwo = NewRule();
            ruleTwo.Guid = Guid.NewGuid();

            var resultOne = await context.ActivationRuleCreateCaseObjectAsync(ruleOne, false, cacheService);
            var resultTwo = await context.ActivationRuleCreateCaseObjectAsync(ruleTwo, false, cacheService);

            resultOne.Should().NotBeNull();
            resultTwo.Should().NotBeNull();
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