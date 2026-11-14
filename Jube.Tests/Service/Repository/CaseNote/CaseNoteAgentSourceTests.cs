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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Repository.CaseNote;
using Jube.Service.Agent;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseNote;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.CaseNote
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseNoteAgentSourceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private JubeDynamicEnvironment Environment() => TestDynamicEnvironment.Create(
            new Dictionary<string, string> { ["ConnectionString"] = fx.ConnectionString });

        private async Task<CaseNoteService> ServiceAsync(Jube.Data.Context.DbContext dbContext)
        {
            return await CaseNoteService.CreateAsync(dbContext, fx.Seed.UserWithPermission, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), Environment(), new EngineJsonSerializationHelper(), TestLog.NoOp);
        }

        private static CaseNoteDto Draft(CaseNoteSeed seed, string text) => new()
        {
            Note = text, ActionId = seed.ActionId, PriorityId = 2, CaseId = seed.CaseId, CaseKey = seed.CaseKey,
            CaseKeyValue = seed.CaseKeyValue, Payload = "{}"
        };

        [Fact]
        public async Task APlainCallWritesAUserNoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var saved = await service.InsertAsync(Draft(seed, "plain"));

            (await dbContext.CaseNote.FirstAsync(w => w.Id == saved.Id)).SourceId.Should().Be(1);
            AgentInvocationScope.Active.Should().BeFalse();
        }

        [Fact]
        public async Task ACallInsideAnAgentScopeWritesAnAskJoobyNoteAuthoredByTheCallerAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            Jube.Dto.Repository.CaseNote.CaseNoteDto saved;

            using (AgentInvocationScope.Enter())
            {
                saved = await service.InsertAsync(Draft(seed, "from the agent"));
            }

            var stored = await dbContext.CaseNote.FirstAsync(w => w.Id == saved.Id);
            stored.SourceId.Should().Be(3);
            stored.KindId.Should().Be(1);
            stored.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            AgentInvocationScope.Active.Should().BeFalse();
        }

        [Fact]
        public async Task TheScopeNeverLeaksIntoConcurrentCallsAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);

            async Task<(bool Agent, int Source)> WriteAsync(int index)
            {
                var agent = index % 2 == 0;
                await using var dbContext = fx.GetDbContext();
                var service = await ServiceAsync(dbContext);

                using var scope = agent ? AgentInvocationScope.Enter() : null;
                await Task.Delay(Random.Shared.Next(1, 25));

                var saved = await service.InsertAsync(Draft(seed, $"concurrent {index}"));

                return (agent, (await dbContext.CaseNote.FirstAsync(w => w.Id == saved.Id)).SourceId);
            }

            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => Task.Run(() => WriteAsync(i))));

            results.Where(r => r.Agent).Should().OnlyContain(r => r.Source == 3);
            results.Where(r => !r.Agent).Should().OnlyContain(r => r.Source == 1);
            AgentInvocationScope.Active.Should().BeFalse();
        }

        [Fact]
        public async Task TheBoundCaseNoteInsertToolWritesAnAskJoobyNoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            var services = new ServiceCollection();
            services.AddSingleton<ILog>(TestLog.NoOp);
            services.AddSingleton(localizers);
            services.AddSingleton<IServiceChangeBus>(new NullServiceChangeBus());
            services.AddSingleton(Environment());
            services.AddSingleton(new EngineJsonSerializationHelper());

            var context = new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission,
                services.BuildServiceProvider(), CancellationToken.None, false, fx.ConnectionString);

            var tool = PageAgentToolBinder.Bind(context, ["CaseNoteInsert"], TestLog.NoOp).Single();
            var json = JsonSerializer.Serialize(Draft(seed, "written by a tool"));

            await tool.Function.InvokeAsync(
                new AIFunctionArguments { ["model"] = JsonSerializer.Deserialize<JsonElement>(json) },
                CancellationToken.None);

            var stored = await dbContext.CaseNote.SingleAsync(w => w.CaseId == seed.CaseId);
            stored.SourceId.Should().Be(3);
            stored.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            AgentInvocationScope.Active.Should().BeFalse();
        }

        [Fact]
        public async Task AnAgentWrittenNoteIsReadOnlyToItsAuthorAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            Jube.Dto.Repository.CaseNote.CaseNoteDto saved;

            using (AgentInvocationScope.Enter())
            {
                saved = await service.InsertAsync(Draft(seed, "from the agent"));
            }

            var act = () => service.SetFindingAsync(new CaseNoteFindingRequestDto { Id = saved.Id, IsFinding = true });

            await act.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseNote.NotFoundException>();
        }
    }
}
