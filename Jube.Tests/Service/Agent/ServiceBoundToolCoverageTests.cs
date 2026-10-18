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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using Jube.Service.EntityAnalysisModelTag;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ServiceBoundToolCoverageTests(DatabaseFixture fx)
    {
        private static readonly HashSet<string> notBindableYet =
        [
            "AskJoobyConversationCreate", "AskJoobyConversationGetMessages", "AskJoobyConversationList",
            "AskJoobyConversationSendMessage",
            "IconsGet",
            "UserRegistryApiKeyDelete", "UserRegistryApiKeyListByUserRegistryId"
        ];

        private static ServiceAgentBindingContext Context(DatabaseFixture fx, bool readOnly)
        {
            var services = new ServiceAgentServiceProvider(TestLog.NoOp,
                new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                    NullLoggerFactory.Instance),
                new NullServiceChangeBus(), null, new Jube.DynamicEnvironment.DynamicEnvironment());

            return new ServiceAgentBindingContext(fx.GetDbContext(), fx.Seed.UserWithPermission, services,
                CancellationToken.None, readOnly,
                Environment.GetEnvironmentVariable("JubeTestConnectionString"));
        }

        private static Dictionary<string, IReadOnlyList<string>> BindAll(DatabaseFixture fx, bool readOnly)
        {
            var context = Context(fx, readOnly);
            var bound = new Dictionary<string, IReadOnlyList<string>>();

            foreach (var (agentType, toolNames) in AgentDefinitions.HierarchyToolNames)
            {
                bound[agentType] =
                [
                    .. PageAgentToolBinder.Bind(context, toolNames, TestLog.NoOp)
                        .Select(t => t.Function.Name)
                ];
            }

            return bound;
        }

        [Fact]
        public async Task NoMoreToolCallsRunAtOnceThanTheConfiguredCapAsync()
        {
            const int cap = 2;
            const int callers = 12;

            var context = Context(fx, false) with { MaxConcurrentToolCalls = cap };
            var cache = new ServiceInstanceCache(context, TestLog.NoOp);

            var inFlight = 0;
            var peak = 0;
            var peakGate = new object();

            async Task<bool> BodyAsync()
            {
                var now = Interlocked.Increment(ref inFlight);

                lock (peakGate)
                {
                    peak = Math.Max(peak, now);
                }

                await Task.Delay(25);
                Interlocked.Decrement(ref inFlight);

                return true;
            }

            await Task.WhenAll(Enumerable.Range(0, callers).Select(_ =>
                cache.InvokeIsolatedAsync(typeof(EntityAnalysisModelTagService),
                    BodyAsync, CancellationToken.None)));

            peak.Should().BeLessThanOrEqualTo(cap,
                "the cap is what stands between a wide fan-out and the database connection budget");
        }

        [Fact]
        public void EveryLeafAgentBindsAtLeastOneToolWithTheDependenciesTheConversationServiceCanSupply()
        {
            var bound = BindAll(fx, false);

            var empty = bound.Where(p => p.Value.Count == 0).Select(p => p.Key).OrderBy(k => k).ToList();

            empty.Should().BeEmpty(
                "an agent that binds nothing is registered but useless: " + string.Join(", ", empty));
        }

        [Fact]
        public void EveryCataloguedToolBindsRatherThanBeingSkippedForAnUnresolvableDependency()
        {
            var bound = BindAll(fx, false);
            var boundNames = bound.SelectMany(p => p.Value).ToHashSet();

            var skipped = AgentDefinitions.HierarchyToolNames.SelectMany(p => p.Value)
                .Where(name => !boundNames.Contains(name))
                .Where(name => !notBindableYet.Contains(name)).OrderBy(n => n).ToList();

            skipped.Should().BeEmpty(
                "a tool skipped here is one whose service needs a dependency ServiceAgentServiceProvider does "
                + "not supply, so the agent silently loses it: " + string.Join(", ", skipped));
        }

        [Fact]
        public void TheOnlyUnbindableToolsAreTheSevenWhoseDependenciesAreNotYetSupplied()
        {
            var boundNames = BindAll(fx, false).SelectMany(p => p.Value).ToHashSet();

            var skipped = AgentDefinitions.HierarchyToolNames.SelectMany(p => p.Value)
                .Where(name => !boundNames.Contains(name)).OrderBy(n => n).ToList();

            skipped.Should().BeEquivalentTo(notBindableYet,
                "narrowing this set is progress and widening it is a regression, so it is asserted exactly");

            boundNames.Count.Should().Be(733);
        }

        private static ServiceAgentBindingContext ContextWith(DatabaseFixture fx,
            ServiceAgentServiceProvider services)
        {
            return new ServiceAgentBindingContext(fx.GetDbContext(), fx.Seed.UserWithPermission, services,
                CancellationToken.None, false,
                Environment.GetEnvironmentVariable("JubeTestConnectionString"));
        }

        [Fact]
        public async Task ApprovalsRequiredBindsFromDynamicEnvironmentRatherThanADefaultAsync()
        {
            var services = new ServiceAgentServiceProvider(TestLog.NoOp,
                new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                    NullLoggerFactory.Instance),
                new NullServiceChangeBus(), null,
                TestDynamicEnvironment.Create(new Dictionary<string, string> { ["ApprovalsRequired"] = "2" }));

            var cache = new ServiceInstanceCache(ContextWith(fx, services), TestLog.NoOp);

            var approvalsRequired = await cache.InvokeIsolatedAsync(typeof(ApprovalsRequiredProbeService),
                () => Task.FromResult(
                    ((ApprovalsRequiredProbeService)cache.Get(typeof(ApprovalsRequiredProbeService)))
                    .ApprovalsRequired), CancellationToken.None);

            approvalsRequired.Should().Be(2,
                "the agent path must enforce the same approval rule the pages enforce");
        }

        [Fact]
        public async Task AMissingDynamicEnvironmentFailsTheToolCallInsteadOfWeakeningApprovalsRequiredAsync()
        {
            var services = new ServiceAgentServiceProvider(TestLog.NoOp,
                new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                    NullLoggerFactory.Instance),
                new NullServiceChangeBus());

            var cache = new ServiceInstanceCache(ContextWith(fx, services), TestLog.NoOp);

            var act = () => cache.InvokeIsolatedAsync(typeof(ApprovalsRequiredProbeService),
                () => Task.FromResult(true), CancellationToken.None);

            (await act.Should().ThrowAsync<InvalidOperationException>(
                    "a quiet fallback to one approval is a two-man-rule bypass"))
                .WithMessage("*approvalsRequired*");
        }

        [Fact]
        public void AReadOnlyDeploymentBindsStrictlyFewerToolsAndKeepsEveryRead()
        {
            var writable = BindAll(fx, false).SelectMany(p => p.Value).ToHashSet();
            var readOnly = BindAll(fx, true).SelectMany(p => p.Value).ToHashSet();

            readOnly.Should().BeSubsetOf(writable);
            readOnly.Count.Should().BeLessThan(writable.Count);
        }
    }
}