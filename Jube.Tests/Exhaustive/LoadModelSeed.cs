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
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Test.Exhaustive
{
    internal sealed class LoadModelSeed(DatabaseFixture fx)
    {
        private static readonly string[] childTables =
        [
            "EntityAnalysisModelRequestXpath", "EntityAnalysisModelTtlCounter",
            "EntityAnalysisModelSanction", "EntityAnalysisModelAbstractionRule",
            "EntityAnalysisModelAbstractionCalculation", "EntityAnalysisModelHttpAdaptation",
            "ExhaustiveSearchInstance",
            "EntityAnalysisModelActivationRule", "EntityAnalysisModelTag"
        ];

        private readonly List<int> modelIds = [];

        public async Task<(int ModelId, int TenantId)> CreateModelAsync()
        {
            await using var db = fx.GetDbContext();

            var tenantId = (await db.UserInTenant.FirstAsync(u => u.User == fx.Seed.UserWithPermission))
                .TenantRegistryId;

            var modelId = await db.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Load{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            modelIds.Add(modelId);

            return (modelId, tenantId);
        }

        public async Task AddChildAsync(string table, int modelId, string name, int active = 1, int deleted = 0)
        {
            await using var db = fx.GetDbContext();

            await db.ExecuteAsync(
                $"INSERT INTO \"{table}\" (\"EntityAnalysisModelId\", \"Name\", \"Active\", \"Deleted\") " +
                "VALUES (@m, @n, @a, @d)",
                new DataParameter("m", modelId), new DataParameter("n", name), new DataParameter("a", active),
                new DataParameter("d", deleted));
        }

        public async Task AddRowsAsync(string table, int modelId, int count, string jsonExpression)
        {
            await using var db = fx.GetDbContext();

            await db.ExecuteAsync(
                $"INSERT INTO \"{table}\" (\"Json\", \"EntityAnalysisModelInstanceEntryGuid\", " +
                "\"EntityAnalysisModelId\", \"CreatedDate\", \"ReferenceDate\", \"ResponseElevation\", " +
                "\"ActivationRuleCount\", \"EntryKeyValue\") " +
                $"SELECT ({jsonExpression})::jsonb, " +
                "(lpad(to_hex(@m), 8, '0') || '-0000-0000-0000-' || lpad(to_hex(g), 12, '0'))::uuid, " +
                "@m, now(), now(), 0, 0, 'k' || g FROM generate_series(1, @n) g",
                new DataParameter("m", modelId), new DataParameter("n", count));
        }

        public async Task DisposeAsync()
        {
            await using var db = fx.GetDbContext();

            foreach (var modelId in modelIds)
            {
                var parameter = new DataParameter("m", modelId);
                await db.ExecuteAsync("DELETE FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = @m", parameter);
                await db.ExecuteAsync("DELETE FROM \"MockArchive\" WHERE \"EntityAnalysisModelId\" = @m", parameter);

                foreach (var table in childTables)
                {
                    await db.ExecuteAsync($"DELETE FROM \"{table}\" WHERE \"EntityAnalysisModelId\" = @m", parameter);
                    await db.ExecuteAsync($"DELETE FROM \"{table}Version\" WHERE \"EntityAnalysisModelId\" = @m",
                        parameter);
                }

                await db.ExecuteAsync("DELETE FROM \"EntityAnalysisModelVersion\" WHERE \"EntityAnalysisModelId\" = @m",
                    parameter);
                await db.ExecuteAsync("DELETE FROM \"EntityAnalysisModel\" WHERE \"Id\" = @m", parameter);
            }
        }
    }
}