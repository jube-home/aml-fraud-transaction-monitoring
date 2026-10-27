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

using System.Linq;
using System.Threading.Tasks;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using Npgsql;
using Xunit;

namespace Jube.Test.Service.ModelScaffolding
{
    [Collection("Database")]
    public sealed class ModelSyncResponseElevationRangeTests(ModelScaffoldFixture fixture)
        : IClassFixture<ModelScaffoldFixture>
    {
        private static async Task ExecuteAsync(string sql, int modelId)
        {
            await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("model", modelId);
            await command.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task TheEngineSnapshotCarriesTheActiveRangesWithTheirNameAndOutcomeAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();

            await ExecuteAsync("""
                               DELETE FROM "EntityAnalysisModelResponseElevationRange" WHERE "EntityAnalysisModelId" = @model;
                               INSERT INTO "EntityAnalysisModelResponseElevationRange"
                                   ("Guid","EntityAnalysisModelId","Name","Minimum","Maximum","Decline","Hold","Locked","Active","Deleted","Version","CreatedDate","CreatedUser")
                               VALUES
                                   (gen_random_uuid(), @model, 'Review hold', 10, 19, 0, 1, 0, 1, 0, 1, now(), 'Administrator'),
                                   (gen_random_uuid(), @model, 'Hard decline', 20, 20, 1, 1, 0, 1, 0, 1, now(), 'Administrator'),
                                   (gen_random_uuid(), @model, 'Dormant', 30, 39, 0, 0, 0, 0, 0, 1, now(), 'Administrator');
                               INSERT INTO "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                               SELECT m."TenantRegistryId", 19, r."Id", 1, 1, now(), 'Administrator', gen_random_uuid()
                               FROM "EntityAnalysisModelResponseElevationRange" r
                               JOIN "EntityAnalysisModel" m ON m."Id" = r."EntityAnalysisModelId"
                               WHERE r."EntityAnalysisModelId" = @model;
                               """, scaffold.ModelId);

            await ModelSync.SyncAsync(fixture.Engine, scaffold);

            var ranges = fixture.Engine.FindActiveModel(scaffold.ModelGuid).Required().Snapshot
                .EntityAnalysisModelResponseElevationRanges;

            Assert.Equal(["Hard decline", "Review hold"], ranges.Select(s => s.Name).Order().ToArray());

            var hold = ranges.Single(s => s.Name == "Review hold");
            Assert.True(hold.Hold);
            Assert.False(hold.Decline);
            Assert.True(hold.Contains(10) && hold.Contains(19));
            Assert.False(hold.Contains(20));

            var decline = ranges.Single(s => s.Name == "Hard decline");
            Assert.True(decline.Decline);
            Assert.False(decline.Hold, "a decline never holds, even if the row says both");
        }

        [Fact]
        public async Task ARuleWhoseLevelIsInNoActiveRangeIsNotSentOutByTheEngineAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();

            await ExecuteAsync("""
                               DELETE FROM "EntityAnalysisModelResponseElevationRange" WHERE "EntityAnalysisModelId" = @model;
                               UPDATE "EntityAnalysisModelActivationRule" SET "EnableResponseElevation" = 1, "ResponseElevation" = 5
                               WHERE "EntityAnalysisModelId" = @model AND coalesce("Deleted",0) = 0;
                               """, scaffold.ModelId);

            await ModelSync.SyncAsync(fixture.Engine, scaffold);

            var rules = fixture.Engine.FindActiveModel(scaffold.ModelGuid).Required().Snapshot.ModelActivationRules;

            Assert.NotEmpty(rules);
            Assert.All(rules, rule => Assert.False(rule.EnableResponseElevation));

            await ExecuteAsync("""
                               INSERT INTO "EntityAnalysisModelResponseElevationRange"
                                   ("Guid","EntityAnalysisModelId","Name","Minimum","Maximum","Decline","Hold","Locked","Active","Deleted","Version","CreatedDate","CreatedUser")
                               VALUES (gen_random_uuid(), @model, 'Five', 1, 9, 0, 0, 0, 1, 0, 1, now(), 'Administrator');
                               INSERT INTO "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                               SELECT m."TenantRegistryId", 19, r."Id", 1, 1, now(), 'Administrator', gen_random_uuid()
                               FROM "EntityAnalysisModelResponseElevationRange" r
                               JOIN "EntityAnalysisModel" m ON m."Id" = r."EntityAnalysisModelId"
                               WHERE r."EntityAnalysisModelId" = @model;
                               """, scaffold.ModelId);

            await ModelSync.SyncAsync(fixture.Engine, scaffold);

            var after = fixture.Engine.FindActiveModel(scaffold.ModelGuid).Required().Snapshot.ModelActivationRules;

            Assert.All(after, rule => Assert.True(rule.EnableResponseElevation));
        }
    }
}
