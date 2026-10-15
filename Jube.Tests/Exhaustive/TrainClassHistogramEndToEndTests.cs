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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Collection("Database")]
    public class TrainClassHistogramEndToEndTests(DatabaseFixture fx)
    {
        [Fact]
        public async Task ClassHistogramTableRepositoryRoundTripsSoftDeletesAndIsTenantScopedAsync()
        {
            await using var db = fx.GetDbContext();

            var model = await new EntityAnalysisModelRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Ch{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                    Locked = 0, Deleted = 0
                });

            var instance = await new ExhaustiveSearchInstanceRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Ch{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id, Active = 1, Locked = 0, Deleted = 0, StatusId = 0
                });

            var variableId = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = instance.Id, Name = "x"
            });

            var repository = new ExhaustiveSearchInstanceVariableHistogramClassRepository(db);

            try
            {
                for (var bin = 0; bin < 3; bin++)
                {
                    await repository.InsertAsync(new ExhaustiveSearchInstanceVariableHistogramClass
                    {
                        ExhaustiveSearchInstanceVariableId = variableId, ClassLabel = bin % 2, BinSequence = bin,
                        BinRangeStart = bin, BinRangeEnd = bin + 1, Frequency = bin + 10
                    });
                }

                var read = (await repository.GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(variableId))
                    .ToList();

                read.Select(r => r.Frequency).Should().Equal(10, 11, 12);
                read.Select(r => r.ClassLabel).Should().Equal(0, 1, 0);

                await repository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(model.TenantRegistryId!.Value + 999999,
                    0);

                (await repository.GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(variableId)).Should()
                    .HaveCount(3, "another tenant's soft delete must not reach these rows");
            }
            finally
            {
                await db.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>()
                    .Where(w => w.ExhaustiveSearchInstanceVariableId == variableId).DeleteAsync();
                await db.GetTable<ExhaustiveSearchInstanceVariable>().Where(w => w.Id == variableId).DeleteAsync();

                await db.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).DeleteAsync();
                await db.GetTable<ExhaustiveSearchInstance>().Where(w => w.Id == instance.Id).DeleteAsync();
                await db.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == model.Id)
                    .DeleteAsync();

                await db.EntityAnalysisModel.Where(w => w.Id == model.Id).DeleteAsync();
            }
        }

        [Fact]
        public async Task MigrationCreatesTheClassHistogramTableWithEveryColumnAsync()
        {
            await using var db = fx.GetDbContext();

            var columns = await db.QueryToListAsync<string>(
                "select column_name from information_schema.columns where table_name = 'ExhaustiveSearchInstanceVariableHistogramClass' order by ordinal_position");

            columns.Should().Equal("Id", "ExhaustiveSearchInstanceVariableId", "ClassLabel", "BinSequence",
                "BinRangeStart", "BinRangeEnd", "Frequency", "Deleted", "DeletedDate", "ImportId");
        }
    }
}