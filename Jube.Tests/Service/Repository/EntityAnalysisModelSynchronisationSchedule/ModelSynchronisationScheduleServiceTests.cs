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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ModelPoco = Jube.Data.Poco.EntityAnalysisModel;
using ScheduleService =
    Jube.Service.Repository.EntityAnalysisModelSynchronisationSchedule.
    EntityAnalysisModelSynchronisationScheduleService;

namespace Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ModelSynchronisationScheduleServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];
        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<ScheduleService> BuildServiceAsync(DbContext dbContext, string userName) =>
            ScheduleService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);

        private async Task<int> NewModelAsync(string user)
        {
            await using var dbContext = fx.GetDbContext();

            var model = await new EntityAnalysisModelRepository(dbContext, user).InsertAsync(
                new ModelPoco
                {
                    Name = $"{DatabaseFixture.Prefix}Ss{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                    Locked = 0, Deleted = 0
                });

            return model.Id;
        }

        private async Task<int> InsertAsync(int? modelId, DateTimeOffset? when = null)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = when,
                EntityAnalysisModelId = modelId
            });
            createdIds.Add(saved.Id);

            return saved.Id;
        }

        [Fact]
        public async Task AScheduleForOneModelIsStoredWithThatModelAsync()
        {
            var model = await NewModelAsync(fx.Seed.UserWithPermission);

            var id = await InsertAsync(model);

            await using var dbContext = fx.GetDbContext();

            (await dbContext.EntityAnalysisModelSynchronisationSchedule.SingleAsync(w => w.Id == id))
                .EntityAnalysisModelId.Should().Be(model);
        }

        [Fact]
        public async Task AScheduleWithNoModelStaysWholeTenantAsync()
        {
            var id = await InsertAsync(null);

            await using var dbContext = fx.GetDbContext();

            (await dbContext.EntityAnalysisModelSynchronisationSchedule.SingleAsync(w => w.Id == id))
                .EntityAnalysisModelId.Should().BeNull();
        }

        [Fact]
        public async Task AModelThatDoesNotExistIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                EntityAnalysisModelId = int.MaxValue
            });

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();

            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "ModelNotFound");
        }

        [Fact]
        public async Task AModelOfAnotherTenantIsRefusedAsync()
        {
            var foreign = await NewModelAsync(fx.Seed.UserTenantB);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                EntityAnalysisModelId = foreign
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task ValidateReportsAnUnknownModelWithoutSavingAnythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ValidateAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                EntityAnalysisModelId = int.MaxValue
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorCode == "ModelNotFound");
        }

        [Fact]
        public async Task GetCurrentForAModelIsTheNewerOfItsOwnRowAndTheWholeTenantRowAsync()
        {
            var model = await NewModelAsync(fx.Seed.UserWithPermission);
            var own = await InsertAsync(model, DateTimeOffset.UtcNow.AddMinutes(5));

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetCurrentAsync(model))!.Id.Should().Be(own);

            var wholeTenant = await InsertAsync(null, DateTimeOffset.UtcNow.AddMinutes(6));

            (await service.GetCurrentAsync(model))!.Id.Should().Be(wholeTenant,
                "a later whole tenant row also applies to this model");
        }

        [Fact]
        public async Task GetCurrentWithoutAModelIgnoresRowsForOneModelAsync()
        {
            var model = await NewModelAsync(fx.Seed.UserWithPermission);
            var wholeTenant = await InsertAsync(null, DateTimeOffset.UtcNow.AddMinutes(5));
            await InsertAsync(model, DateTimeOffset.UtcNow.AddMinutes(6));

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetCurrentAsync())!.Id.Should().Be(wholeTenant);
        }

        [Fact]
        public async Task GetCurrentForAnotherModelIgnoresARowForThisOneAsync()
        {
            var model = await NewModelAsync(fx.Seed.UserWithPermission);
            var other = await NewModelAsync(fx.Seed.UserWithPermission);
            await InsertAsync(model, DateTimeOffset.UtcNow.AddMinutes(5));

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var current = await service.GetCurrentAsync(other);

            (current?.EntityAnalysisModelId).Should().NotBe(model);
        }

        [Fact]
        public async Task AScheduleForAModelWithoutPermissionIsForbiddenAsync()
        {
            var model = await NewModelAsync(fx.Seed.UserWithPermission);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                EntityAnalysisModelId = model
            });

            await act.Should().ThrowAsync<ForbiddenException>();
        }
    }
}
