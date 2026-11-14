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
using Jube.Dto.Repository.CaseResearch;
using Jube.Service.Agent;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using Jube.Service.Exceptions.Repository.CaseResearch;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseResearch;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.CaseNote;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseResearch
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseResearchServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static Task<CaseResearchService> ServiceAsync(Jube.Data.Context.DbContext dbContext, string? user) =>
            CaseResearchService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus());

        private static CaseResearchDto Draft(CaseNoteSeed seed, params CaseResearchSourceDto[] sources) => new()
        {
            CaseId = seed.CaseId, Topic = "Who owns ACME Ltd?", Findings = "Registered to a holding company.",
            Sources = [.. sources]
        };

        [Fact]
        public async Task ResearchCannotBeRecordedOnACaseLockedToSomeoneElseAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var other = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            await seed.LockToAsync(other);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await ((Func<Task>)(() => service.CreateAsync(Draft(seed)))).Should().ThrowAsync<CaseLockedException>();

            (await dbContext.CaseResearch.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);

            await seed.LockToAsync(fx.Seed.UserWithPermission);
            await service.CreateAsync(Draft(seed));

            (await dbContext.CaseResearch.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(1);
        }

        [Fact]
        public async Task CreateWithoutAUserOrAnUnknownTenantIsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await ((Func<Task>)(() => ServiceAsync(dbContext, null))).Should().ThrowAsync<NotAuthenticatedException>();
            await ((Func<Task>)(() => ServiceAsync(dbContext, fx.Seed.UnknownUser))).Should()
                .ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task EveryOperationWithoutPermissionIsForbiddenAndStoresNothingAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await ((Func<Task>)(() => service.CreateAsync(Draft(seed)))).Should().ThrowAsync<ForbiddenException>();
            await ((Func<Task>)(() => service.GetByCaseKeyValueAsync("a", "b"))).Should().ThrowAsync<ForbiddenException>();
            (await dbContext.CaseResearch.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task ResearchIsStoredWithItsSourcesTheCasesKeyAndTheCallerAsAUserAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.CreateAsync(Draft(seed,
                new CaseResearchSourceDto { Title = " Registry ", Url = " https://registry.example.org/acme " },
                new CaseResearchSourceDto
                {
                    Url = "http://example.com/a", RetrievedDate = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)
                }));

            var stored = await dbContext.CaseResearch.FirstAsync(w => w.Id == saved.Id);
            stored.SourceId.Should().Be(1);
            stored.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            stored.CaseKey.Should().Be(seed.CaseKey);
            stored.CaseKeyValue.Should().Be(seed.CaseKeyValue);

            var sources = await dbContext.CaseResearchCitation.Where(w => w.CaseResearchId == saved.Id)
                .OrderBy(w => w.Id).ToListAsync();

            sources.Should().HaveCount(2);
            sources[0].Title.Should().Be("Registry");
            sources[0].Url.Should().Be("https://registry.example.org/acme");
            sources[0].RetrievedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            sources[1].RetrievedDate.Should().Be(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
            saved.Sources.Should().HaveCount(2);
        }

        [Fact]
        public async Task AForgedSourceKeyOrAuthorInTheBodyIsIgnoredAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var draft = Draft(seed);
            draft.SourceId = 2;
            draft.CaseKey = "forged";
            draft.CaseKeyValue = "forged";
            draft.CreatedUser = "AskJooby";
            draft.Id = 1;

            var saved = await service.CreateAsync(draft);

            var stored = await dbContext.CaseResearch.FirstAsync(w => w.Id == saved.Id);
            stored.SourceId.Should().Be(1);
            stored.CaseKey.Should().Be(seed.CaseKey);
            stored.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Id.Should().NotBe(1);
        }

        [Fact]
        public async Task TextIsSanitisedLikeANoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var draft = Draft(seed, new CaseResearchSourceDto { Title = "<img src=x onerror=alert(1)>", Url = "https://e.org/" });
            draft.Topic = "<script>alert(1)</script>topic";
            draft.Findings = "found <b>it</b><script>alert(2)</script>";

            var saved = await service.CreateAsync(draft);

            var stored = await dbContext.CaseResearch.FirstAsync(w => w.Id == saved.Id);
            stored.Topic.Should().NotContain("<script");
            stored.Findings.Should().NotContain("<script").And.Contain("<b>it</b>");
            saved.Sources[0].Title.Should().NotContain("onerror");
        }

        [Theory]
        [InlineData("", "findings")]
        [InlineData("   ", "findings")]
        [InlineData("topic", "")]
        [InlineData("topic", "   ")]
        public async Task ATopicAndFindingsAreRequiredAsync(string topic, string findings)
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var draft = Draft(seed);
            draft.Topic = topic;
            draft.Findings = findings;

            await ((Func<Task>)(() => service.CreateAsync(draft))).Should().ThrowAsync<DtoValidationException>();
            (await dbContext.CaseResearch.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task ATopicOverFiveHundredOrFindingsOverTheLimitAreRefusedAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var topic = Draft(seed);
            topic.Topic = new string('q', 501);
            var findings = Draft(seed);
            findings.Findings = new string('f', 100001);

            await ((Func<Task>)(() => service.CreateAsync(topic))).Should().ThrowAsync<DtoValidationException>();
            await ((Func<Task>)(() => service.CreateAsync(findings))).Should().ThrowAsync<DtoValidationException>();
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:text/html;base64,AAAA")]
        [InlineData("file:///etc/passwd")]
        [InlineData("ftp://example.org/a")]
        [InlineData("vbscript:msgbox(1)")]
        [InlineData("https://user:pass@example.org/a")]
        [InlineData("//example.org/a")]
        [InlineData("not a url")]
        [InlineData("https://exa mple.org/")]
        [InlineData("https://example.org/<script>")]
        [InlineData("")]
        public async Task AHostileSourceAddressRefusesTheWholeResearchAndNothingIsStoredAsync(string url)
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var before = await dbContext.CaseResearchCitation.CountAsync();
            var act = () => service.CreateAsync(Draft(seed, new CaseResearchSourceDto { Url = url }));

            var thrown = (await act.Should().ThrowAsync<DtoValidationException>()).Which;
            thrown.Result.Errors.Should().OnlyContain(e => e.AttemptedValue == null);
            (await dbContext.CaseResearch.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
            (await dbContext.CaseResearchCitation.CountAsync()).Should().Be(before);
        }

        [Fact]
        public async Task AnOverlongAddressOrTitleOrElevenSourcesAreRefusedAndTenAreAllowedAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var address = () => service.CreateAsync(Draft(seed,
                new CaseResearchSourceDto { Url = "https://example.org/" + new string('a', 2000) }));

            var title = () => service.CreateAsync(Draft(seed,
                new CaseResearchSourceDto { Title = new string('t', 301), Url = "https://example.org/" }));

            var many = () => service.CreateAsync(Draft(seed, [.. Enumerable.Range(0, 11)
                .Select(i => new CaseResearchSourceDto { Url = $"https://example.org/{i}" })]));

            await address.Should().ThrowAsync<DtoValidationException>();
            await title.Should().ThrowAsync<DtoValidationException>();
            await many.Should().ThrowAsync<DtoValidationException>();
            (await dbContext.CaseResearch.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);

            var ten = await service.CreateAsync(Draft(seed, [.. Enumerable.Range(0, 10)
                .Select(i => new CaseResearchSourceDto { Url = $"https://example.org/{i}" })]));

            ten.Sources.Should().HaveCount(10);
        }

        [Fact]
        public async Task AForeignTenantsCaseAMissingCaseAndACaseWithoutTheRoleBehaveTheSameAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            var foreign = await ServiceAsync(dbContext, fx.Seed.UserTenantB);
            var noRole = await ServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var missing = Draft(seed);
            missing.CaseId = int.MaxValue;

            var a = (await ((Func<Task>)(() => foreign.CreateAsync(Draft(seed)))).Should()
                .ThrowAsync<NotFoundException>()).Which.Message;

            var b = (await ((Func<Task>)(() => foreign.CreateAsync(missing))).Should()
                .ThrowAsync<NotFoundException>()).Which.Message;

            var c = (await ((Func<Task>)(() => noRole.CreateAsync(Draft(seed)))).Should()
                .ThrowAsync<NotFoundException>()).Which.Message;

            a.Should().Be(b).And.Be(c);
            (await dbContext.CaseResearch.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task AnAgentCapturesResearchFlaggedAskJoobyAndAPlainCallIsAUserAndTheyNeverLeakAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);

            async Task<(bool Agent, int Source)> WriteAsync(int index)
            {
                var agent = index % 2 == 0;
                await using var dbContext = fx.GetDbContext();
                var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

                using var scope = agent ? AgentInvocationScope.Enter() : null;
                await Task.Delay(Random.Shared.Next(1, 25));

                var saved = await service.CreateAsync(Draft(seed, new CaseResearchSourceDto { Url = "https://example.org/a" }));

                return (agent, (await dbContext.CaseResearch.FirstAsync(w => w.Id == saved.Id)).SourceId);
            }

            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => Task.Run(() => WriteAsync(i))));

            results.Where(r => r.Agent).Should().OnlyContain(r => r.Source == 2);
            results.Where(r => !r.Agent).Should().OnlyContain(r => r.Source == 1);
            AgentInvocationScope.Active.Should().BeFalse();
        }

        [Fact]
        public async Task TheBoundAgentToolCapturesResearchFlaggedAskJoobyAuthoredByTheCallerAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            var services = new ServiceCollection();
            services.AddSingleton<ILog>(TestLog.NoOp);
            services.AddSingleton(localizers);
            services.AddSingleton<IServiceChangeBus>(new NullServiceChangeBus());

            var context = new ServiceAgentBindingContext(dbContext, fx.Seed.UserWithPermission,
                services.BuildServiceProvider(), CancellationToken.None, false, fx.ConnectionString);

            var tool = PageAgentToolBinder.Bind(context, ["CaseResearchCreate"], TestLog.NoOp).Single();

            var json = JsonSerializer.Serialize(Draft(seed, new CaseResearchSourceDto
                { Title = "Filing", Url = "https://filings.example.org/acme" }));

            await tool.Function.InvokeAsync(
                new AIFunctionArguments { ["model"] = JsonSerializer.Deserialize<JsonElement>(json) },
                CancellationToken.None);

            var stored = await dbContext.CaseResearch.SingleAsync(w => w.CaseId == seed.CaseId);
            stored.SourceId.Should().Be(2);
            stored.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            (await dbContext.CaseResearchCitation.CountAsync(w => w.CaseResearchId == stored.Id)).Should().Be(1);
        }

        [Fact]
        public async Task TheListReturnsNewestFirstWithSourcesAndOnlyWebAddressesAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.CreateAsync(Draft(seed));
            var second = await service.CreateAsync(Draft(seed, new CaseResearchSourceDto { Url = "https://example.org/a" }));

            await dbContext.InsertAsync(new Data.Poco.CaseResearchCitation
            {
                CaseResearchId = second.Id, Url = "javascript:alert(1)", RetrievedDate = DateTime.UtcNow
            });

            var listed = await service.GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue);

            listed.Should().HaveCount(2);
            listed[0].Id.Should().Be(second.Id);
            listed[0].Sources.Should().ContainSingle().Which.Url.Should().Be("https://example.org/a");
            listed[1].Sources.Should().BeEmpty();
        }

        [Fact]
        public async Task TheListIsScopedToTheCallersTenantAndWorkflowRolesAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission)).CreateAsync(Draft(seed));

            (await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission))
                .GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue)).Should().ContainSingle();

            (await (await ServiceAsync(dbContext, fx.Seed.UserTenantB))
                .GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue)).Should().BeEmpty();

            (await (await ServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval))
                .GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue)).Should().BeEmpty();
        }
    }
}
