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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Engine.EntityAnalysisModelInvoke.Exceptions;
using Jube.Service.Invoke;
using Jube.Service.Query.InboundQueue;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.InboundQueue
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class InboundQueueServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private Guid modelGuid;
        private int modelId;
        private int keyId;
        private int roleId;
        private string User => fx.Seed.UserWithPermission;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = await new EntityAnalysisModelRepository(dbContext, User).InsertAsync(new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Iq{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                Locked = 0, Deleted = 0
            });

            (modelGuid, modelId) = (model.Guid, model.Id);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.TransactionInboundQueue.Where(w => w.EntityAnalysisModelGuid == modelGuid).DeleteAsync();
            await dbContext.EntityAnalysisModelRole.Where(w => w.EntityAnalysisModelGuid == modelGuid).DeleteAsync();

            if (keyId != 0)
            {
                await dbContext.UserRegistryApiKey.Where(w => w.Id == keyId).DeleteAsync();
            }

            await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
        }

        private async Task GrantApiAccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = await dbContext.UserRegistry.Where(u => u.Name == User).FirstAsync();

            keyId = await dbContext.InsertWithInt32IdentityAsync(new UserRegistryApiKey
            {
                Guid = Guid.NewGuid(), Name = "iq", UserRegistryId = user.Id, ApiKey = "x", Deleted = 0,
                CreatedDate = DateTime.UtcNow, CreatedUser = User
            });

            roleId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRole
            {
                EntityAnalysisModelGuid = modelGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = user.RoleRegistryGuid,
                Deleted = 0, CreatedDate = DateTime.UtcNow, CreatedUser = User
            });
        }

        private InboundQueueService Service(string? user = null, bool enabled = true, int maximum = 100000)
        {
            var environment = TestDynamicEnvironment.Create(new System.Collections.Generic.Dictionary<string, string>
            {
                ["EnablePublicInvokeController"] = enabled ? "True" : "False",
                ["MaxInvokeControllerRequestBytes"] = "1000",
                ["InboundQueueMaximumDepth"] = maximum.ToString()
            });

            return new InboundQueueService(environment, TestLog.NoOp, user ?? User, () => fx.GetDbContext());
        }

        private static Func<int, Task<MemoryStream>> Body(string text) =>
            max =>
            {
                var bytes = Encoding.UTF8.GetBytes(text);

                if (bytes.Length == 0)
                {
                    throw new ZeroBytesException();
                }

                if (bytes.Length > max)
                {
                    throw new ExceededBytesException();
                }

                return Task.FromResult(new MemoryStream(bytes));
            };

        [Fact]
        public async Task AnApiUserOfTheModelEnqueuesAndGetsTheRowIdBackAsync()
        {
            await GrantApiAccessAsync();

            var result = await Service().EnqueueAsync(modelGuid.ToString(), Body("{\"amount\":10}"));

            result.StatusCode.Should().Be(202);

            await using var dbContext = fx.GetDbContext();
            var row = await dbContext.TransactionInboundQueue.SingleAsync(w => w.EntityAnalysisModelGuid == modelGuid);
            (row.StatusId, row.EnqueuedBy, row.Attempts).Should()
                .Be(((short)TransactionInboundQueueStatus.Pending, User, 0));
            Encoding.UTF8.GetString(row.Payload).Should().Be("{\"amount\":10}");
            result.JsonValue.Should().BeEquivalentTo(new { id = row.Id });
        }

        [Fact]
        public async Task RefusalsAreTheSameAsForTheLiveInvokeRouteAsync()
        {
            await GrantApiAccessAsync();

            (await Service(enabled: false).EnqueueAsync(modelGuid.ToString(), Body("{}"))).StatusCode.Should().Be(404);
            (await Service().EnqueueAsync("not-a-guid", Body("{}"))).StatusCode.Should().Be(404);
            (await Service().EnqueueAsync(Guid.NewGuid().ToString(), Body("{}"))).StatusCode.Should().Be(404);
            (await Service().EnqueueAsync(modelGuid.ToString(), Body(""))).StatusCode.Should().Be(400);
            (await Service().EnqueueAsync(modelGuid.ToString(), Body(new string('x', 2000)))).StatusCode.Should().Be(400);
            (await Service().EnqueueAsync(modelGuid.ToString(), Body("{broken"))).StatusCode.Should().Be(400);
            (await Service(user: "").EnqueueAsync(modelGuid.ToString(), Body("{}"))).IsForbid.Should().BeTrue();

            await using var dbContext = fx.GetDbContext();
            (await dbContext.TransactionInboundQueue.CountAsync(w => w.EntityAnalysisModelGuid == modelGuid)).Should().Be(0);
        }

        [Fact]
        public async Task AUserWithoutAnApiKeyOnTheModelOrFromAnotherTenantCannotEnqueueAsync()
        {
            (await Service().EnqueueAsync(modelGuid.ToString(), Body("{}"))).IsForbid.Should().BeTrue();

            await GrantApiAccessAsync();

            (await Service(user: fx.Seed.UserTenantB).EnqueueAsync(modelGuid.ToString(), Body("{}"))).StatusCode
                .Should().Be(404);
            (await Service(user: fx.Seed.UserWithoutPermission).EnqueueAsync(modelGuid.ToString(), Body("{}")))
                .IsForbid.Should().BeTrue();
        }

        [Fact]
        public async Task AFullQueueAnswersTooManyRequestsAsync()
        {
            await GrantApiAccessAsync();
            await Service().EnqueueAsync(modelGuid.ToString(), Body("{}"));

            (await Service(maximum: 0).EnqueueAsync(modelGuid.ToString(), Body("{}"))).StatusCode.Should().Be(429);
        }

        [Fact]
        public async Task DepthDeadListRequeueAndDeleteNeedTheModelPermissionAndStayInTheTenantAsync()
        {
            await GrantApiAccessAsync();
            await Service().EnqueueAsync(modelGuid.ToString(), Body("{\"a\":1}"));

            await using var dbContext = fx.GetDbContext();
            var row = await dbContext.TransactionInboundQueue.SingleAsync(w => w.EntityAnalysisModelGuid == modelGuid);
            await dbContext.TransactionInboundQueue.Where(w => w.Id == row.Id)
                .Set(s => s.StatusId, (short)3).Set(s => s.Attempts, 5).Set(s => s.LastError, "NoResponse").UpdateAsync();

            var admin = await InboundQueueAdminService.CreateAsync(dbContext, User, TestLog.NoOp, new NullServiceChangeBus());
            (await admin.DepthAsync()).Dead.Should().BeGreaterThan(0);

            var dead = (await admin.ListDeadAsync()).Single(d => d.Id == row.Id);
            (dead.Attempts, dead.LastError, dead.EnqueuedBy).Should().Be((5, "NoResponse", User));

            var without = await InboundQueueAdminService.CreateAsync(dbContext, fx.Seed.UserWithoutPermission, TestLog.NoOp,
                new NullServiceChangeBus());
            await ((Func<Task>)(() => without.DepthAsync())).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.RequeueAsync(row.Id))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var other = await InboundQueueAdminService.CreateAsync(dbContext, fx.Seed.UserTenantB, TestLog.NoOp,
                new NullServiceChangeBus());
            (await other.ListDeadAsync()).Select(d => d.Id).Should().NotContain(row.Id);
            await ((Func<Task>)(() => other.RequeueAsync(row.Id))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => other.DeleteAsync(row.Id))).Should().ThrowAsync<CaseExceptions.NotFoundException>();

            await admin.RequeueAsync(row.Id);
            var requeued = await dbContext.TransactionInboundQueue.SingleAsync(w => w.Id == row.Id);
            (requeued.StatusId, requeued.Attempts, requeued.LastError).Should()
                .Be(((short)TransactionInboundQueueStatus.Pending, 0, null));
            await ((Func<Task>)(() => admin.RequeueAsync(row.Id))).Should().ThrowAsync<CaseExceptions.NotFoundException>();

            await dbContext.TransactionInboundQueue.Where(w => w.Id == row.Id).Set(s => s.StatusId, (short)3).UpdateAsync();
            await admin.DeleteAsync(row.Id);
            (await dbContext.TransactionInboundQueue.CountAsync(w => w.Id == row.Id)).Should().Be(0);
        }
    }
}
