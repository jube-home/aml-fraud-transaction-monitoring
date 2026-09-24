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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Parser;
using Jube.Parser.Compiler;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleSandboxTests
    {
        private static RuleParseResult Parse(double threshold)
        {
            var result = RuleParse.Execute($"If (Payload.Amount > {threshold}) Then\n   Return True\nEnd If",
                RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp, RuleParse.DefaultReferences());
            result.Compiled.Should().BeTrue(result.Message);
            return result;
        }

        private static bool Invoke(EntityModelGatewayRule.Match match, double amount)
        {
            var data = new DictionaryNoBoxing<string>();
            data.TryAdd("Amount", amount);
            return match(data, new Dictionary<string, List<string>>(), new PooledDictionary<string, double>(),
                TestLog.NoOp);
        }

        [Theory]
        [InlineData(150, true)]
        [InlineData(100, false)]
        public async Task RunsAParsedRuleThroughTheEngineDelegateTypeAsync(double amount, bool expected)
        {
            using var sandbox = new RuleSandbox();
            var match = await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(Parse(100));

            var matched = await sandbox.RunAsync(() => Invoke(match, amount));

            matched.Should().Be(expected);
        }

        [Fact]
        public async Task TheSameRuleIsLoadedOnceAndThenServedFromTheCacheAsync()
        {
            using var sandbox = new RuleSandbox();

            var first = await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(Parse(100));
            var second = await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(Parse(100));

            second.Should().BeSameAs(first);
            sandbox.Loads.Should().Be(1);
            sandbox.Hits.Should().Be(1);
            sandbox.Count.Should().Be(1);
            sandbox.Bytes.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CapacityEvictsTheLeastRecentlyUsedRuleAsync()
        {
            using var sandbox = new RuleSandbox(2);

            await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(Parse(1));
            await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(Parse(2));
            await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(Parse(1));
            await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(Parse(3));

            sandbox.Count.Should().Be(2);
            sandbox.Evictions.Should().Be(1);
            sandbox.Evict(Parse(2), typeof(EntityModelGatewayRule.Match)).Should().BeFalse();
            sandbox.Evict(Parse(1), typeof(EntityModelGatewayRule.Match)).Should().BeTrue();
        }

        [Fact]
        public async Task AnEvictedRuleAssemblyIsUnloadedByTheGarbageCollectorAsync()
        {
            using var sandbox = new RuleSandbox();
            var parsed = Parse(100);

            var assembly = await LoadRunAndEvictAsync(sandbox, parsed);

            for (var i = 0; i < 200 && assembly.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (assembly.IsAlive)
                {
                    await Task.Delay(50);
                }
            }

            assembly.IsAlive.Should().BeFalse();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<WeakReference> LoadRunAndEvictAsync(RuleSandbox sandbox, RuleParseResult parsed)
        {
            var match = await sandbox.GetDelegateAsync<EntityModelGatewayRule.Match>(parsed);
            Invoke(match, 150).Should().BeTrue();
            var assembly = new WeakReference(match.Method.DeclaringType!.Assembly);
            sandbox.Evict(parsed, typeof(EntityModelGatewayRule.Match)).Should().BeTrue();
            return assembly;
        }

        [Fact]
        public async Task ARuleThatHasNotCompiledCannotBeLoadedAsync()
        {
            using var sandbox = new RuleSandbox();
            var parsedOnly = RuleParse.Execute("If (Payload.Amount > 1) Then\n   Return True\nEnd If",
                RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp, null);

            await sandbox.Awaiting(s => s.GetDelegateAsync<EntityModelGatewayRule.Match>(parsedOnly))
                .Should().ThrowAsync<InvalidOperationException>();
            sandbox.Count.Should().Be(0);
        }

        [Fact]
        public async Task ARunThatExceedsTheTimeoutIsAbandonedAsync()
        {
            using var sandbox = new RuleSandbox(executionTimeout: TimeSpan.FromMilliseconds(50));

            await sandbox.Awaiting(s => s.RunAsync(() =>
            {
                SpinWait.SpinUntil(() => false, 1000);
                return true;
            })).Should().ThrowAsync<TimeoutException>();
        }

        [Fact]
        public async Task ARuntimeExceptionSurfacesToTheCallerAsync()
        {
            using var sandbox = new RuleSandbox();

            await sandbox.Awaiting(s => s.RunAsync<bool>(() => throw new InvalidCastException("bad value")))
                .Should().ThrowAsync<InvalidCastException>().WithMessage("bad value");
        }
    }
}