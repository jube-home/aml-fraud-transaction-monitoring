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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.EntityAnalysisModel;
using Jube.Service.Exceptions.EntityAnalysisModel;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Filter
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class FilterServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModels = [];
        private readonly string marker = $"{DatabaseFixture.Prefix}Flt{Guid.NewGuid():N}"[..30];

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission);
            foreach (var (suffix, active) in new[] { ("A", 1), ("B", 1), ("C", 0) })
            {
                var model = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
                {
                    Name = marker + suffix, Guid = Guid.NewGuid(), Active = (byte)active, Locked = 0, Deleted = 0
                });
                createdModels.Add(model.Id);
            }
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => createdModels.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModels.Contains(w.Id)).DeleteAsync();
        }

        private Task<EntityAnalysisModelService> ServiceAsync(DbContext dbContext, string userName)
        {
            return EntityAnalysisModelService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus());
        }

        private string ByMarker(string extra = "")
        {
            return "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Name\",\"operator\":\"begins_with\",\"value\":\"" +
                   marker + "\"}" + extra + "]}";
        }

        [Fact]
        public async Task FilterReturnsOnlyTheMatchingModelsOfTheCallersTenantPagedByIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var first = await service.FilterAsync(ByMarker(), 2);
            var rest = await service.FilterAsync(ByMarker(), 2, first.Items[^1].Id);
            var active = await service.FilterAsync(
                ByMarker(",{\"id\":\"Active\",\"operator\":\"equal\",\"value\":\"True\"}"));

            first.Valid.Should().BeTrue();
            first.Items.Select(i => i.Name).Should().Equal(marker + "A", marker + "B");
            first.More.Should().BeTrue();
            rest.Items.Select(i => i.Name).Should().Equal(marker + "C");
            rest.More.Should().BeFalse();
            active.Items.Select(i => i.Name).Should().Equal(marker + "A", marker + "B");
        }

        [Fact]
        public async Task CountGroupsTheMatchingModelsByAFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.CountAsync(ByMarker(), "Active");

            result.Valid.Should().BeTrue();
            result.Count.Should().Be(3);
            result.Groups.Select(g => (g.Value, g.Count)).Should().Equal(("True", 2), ("False", 1));
        }

        [Fact]
        public async Task AnotherTenantSeesNoneOfTheModelsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await service.CountAsync(ByMarker())).Count.Should().Be(0);
            (await service.FilterAsync(ByMarker())).Items.Should().BeEmpty();
        }

        [Fact]
        public async Task FilterFieldsDescribeTheListDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var fields = (await service.FilterFieldsAsync()).ToDictionary(f => f.Name);

            fields["Name"].DataType.Should().Be("String");
            fields["Active"].DataType.Should().Be("Boolean");
            fields["CacheTtlInterval"].DataType.Should().Be("String");
            fields["Name"].Description.Should().NotBeEmpty();
        }

        [Fact]
        public async Task InvalidJsonComesBackAsErrorsNotAnExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result =
                await service.FilterAsync("{\"rules\":[{\"id\":\"Nope\",\"operator\":\"equal\",\"value\":1}]}");

            result.Valid.Should().BeFalse();
            result.Errors.Should()
                .ContainSingle(e => e.ErrorCode == "FieldUnknown" && e.PropertyName == "$.rules[0].id");
        }

        [Fact]
        public async Task EveryFilterOperationNeedsTheListPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.FilterFieldsAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => service.FilterAsync(ByMarker()));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.CountAsync(ByMarker()));
        }
    }
}