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
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using Npgsql;
using Xunit;
using Fixture = Jube.Test.Infrastructure.DatabaseFixture.DatabaseFixture;

namespace Jube.Test.Service.ModelScaffolding
{
    [Collection("Database")]
    public sealed class ModelScaffoldTests
    {
        private const string Root = "EntityAnalysisModel";

        private static async Task<NpgsqlConnection> OpenAsync()
        {
            var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static async Task<long> CountAsync(NpgsqlConnection connection, string sql,
            params (string Name, object Value)[] parameters)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        private static async Task<List<(string Child, string ChildColumn, string Parent)>> SingleColumnForeignKeysAsync(
            NpgsqlConnection connection)
        {
            var edges = new List<(string, string, string)>();
            await using var command = new NpgsqlCommand(
                "SELECT cl.relname, a.attname, pl.relname FROM pg_constraint c " +
                "JOIN pg_class cl ON cl.oid = c.conrelid JOIN pg_namespace n ON n.oid = cl.relnamespace AND n.nspname = 'public' " +
                "JOIN pg_class pl ON pl.oid = c.confrelid " +
                "JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = c.conkey[1] " +
                "WHERE c.contype = 'f' AND array_length(c.conkey, 1) = 1", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                edges.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }

            return edges;
        }

        private static async Task<Guid> SourceModelGuidAsync(NpgsqlConnection connection)
        {
            await using var command = new NpgsqlCommand(
                $"SELECT \"Guid\" FROM \"{Root}\" WHERE \"Id\" = @id", connection);
            command.Parameters.AddWithValue("id",
                await ModelGraphCopier.ResolveLiveSourceAsync(connection, null, ModelScaffold.ExampleModelId));
            return (Guid)(await command.ExecuteScalarAsync()).Required();
        }

        [Fact]
        public async Task Copy_HasTheSameRowCountsAsTheSourceModelAsync()
        {
            await using var scaffold = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            await using var connection = await OpenAsync();
            var sourceGuid = await SourceModelGuidAsync(connection);
            var sourceModelId = await ModelGraphCopier.ResolveLiveSourceAsync(connection, null,
                ModelScaffold.ExampleModelId);
            var edges = await SingleColumnForeignKeysAsync(connection);
            var excluded = new System.Text.RegularExpressions.Regex(ModelScaffold.DefaultExcludedTablePattern);

            var checkedTables = 0;

            foreach (var (child, column, _) in edges.Where(e => e.Parent == Root && e.Child != Root &&
                                                                !excluded.IsMatch(e.Child)))
            {
                var source = await CountAsync(connection, $"SELECT count(*) FROM \"{child}\" WHERE \"{column}\" = @id",
                    ("id", sourceModelId));
                var copy = await CountAsync(connection, $"SELECT count(*) FROM \"{child}\" WHERE \"{column}\" = @id",
                    ("id", scaffold.ModelId));
                Assert.True(source == copy, $"\"{child}\": the source model has {source} rows, the copy {copy}.");
                Assert.Equal(source, scaffold.CopiedRows.GetValueOrDefault(child));
                checkedTables++;
            }

            await using var guidTables = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.columns WHERE table_schema = 'public' " +
                "AND column_name = 'EntityAnalysisModelGuid' AND data_type = 'uuid' " +
                "AND table_name NOT IN ('EntityAnalysisModel', 'EntityAnalysisModelRole')", connection);
            var guidLinked = new List<string>();
            await using (var reader = await guidTables.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    guidLinked.Add(reader.GetString(0));
                }
            }

            foreach (var table in guidLinked.Where(t => !excluded.IsMatch(t)))
            {
                var source = await CountAsync(connection,
                    $"SELECT count(*) FROM \"{table}\" WHERE \"EntityAnalysisModelGuid\" = @g", ("g", sourceGuid));
                var copy = await CountAsync(connection,
                    $"SELECT count(*) FROM \"{table}\" WHERE \"EntityAnalysisModelGuid\" = @g",
                    ("g", scaffold.ModelGuid));
                Assert.True(source == copy, $"\"{table}\": the source model has {source} rows, the copy {copy}.");
                checkedTables++;
            }

            Assert.True(checkedTables > 5, "Expected the example model to have configuration below it.");
            Assert.True(scaffold.CopiedRows.ContainsKey("EntityAnalysisModelRequestXpath"));
            Assert.True(scaffold.CopiedRows.ContainsKey("EntityAnalysisModelActivationRule"));
            Assert.True(scaffold.CopiedRows.ContainsKey("EntityAnalysisModelList"), "guid linked tables are copied");
            Assert.True(scaffold.CopiedRows.ContainsKey("EntityAnalysisModelDictionary"),
                "guid linked tables are copied");
        }

        [Fact]
        public async Task Copy_RemapsEveryForeignKeyOntoTheCopyAsync()
        {
            await using var scaffold = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            await using var connection = await OpenAsync();
            var edges = await SingleColumnForeignKeysAsync(connection);

            var mapped = 0;
            foreach (var (child, column, parent) in edges.Where(e => scaffold.IdMap.ContainsKey(e.Child) &&
                                                                     scaffold.IdMap.ContainsKey(e.Parent)))
            {
                var childIds = scaffold.IdMap[child].Values.ToArray();
                var parentIds = scaffold.IdMap[parent].Values.ToArray();

                var stray = await CountAsync(connection,
                    $"SELECT count(*) FROM \"{child}\" WHERE \"Id\" = ANY(@ids) AND \"{column}\" IS NOT NULL " +
                    $"AND NOT (\"{column}\" = ANY(@parents))", ("ids", childIds), ("parents", parentIds));
                Assert.True(stray == 0,
                    $"\"{child}\".\"{column}\" has {stray} copied row(s) pointing outside the copy of \"{parent}\".");
                mapped++;
            }

            Assert.True(mapped > 5, "Expected foreign keys between copied tables.");

            foreach (var (table, map) in scaffold.IdMap)
            {
                Assert.Equal(map.Count, map.Values.Distinct().Count());
                Assert.DoesNotContain(map, kvp => kvp.Key == kvp.Value);
                var present = await CountAsync(connection, $"SELECT count(*) FROM \"{table}\" WHERE \"Id\" = ANY(@ids)",
                    ("ids", map.Values.ToArray()));
                Assert.Equal(map.Count, present);
            }

            var withTenant = await CountAsync(connection,
                $"SELECT count(*) FROM \"{Root}\" WHERE \"Id\" = @id AND \"TenantRegistryId\" = @tenant",
                ("id", scaffold.ModelId), ("tenant", scaffold.TenantRegistryId));
            Assert.Equal(1, withTenant);
        }

        [Fact]
        public async Task DefaultOptions_UseTheDocumentationGuidAndTheNamePrefixAsync()
        {
            await using var scaffold = await ModelScaffold.CreateAsync();
            Assert.Equal(ModelScaffold.ExampleModelGuid, scaffold.ModelGuid);
            Assert.Equal(new Guid("81abd51c-0013-41c1-a4c7-4a6270eb5aa4"), scaffold.ModelGuid);
            Assert.StartsWith(Fixture.Prefix, scaffold.ModelName);
            Assert.StartsWith(Fixture.Prefix, scaffold.TenantName);
            Assert.True(scaffold.OwnsTenant);

            await using var connection = await OpenAsync();
            Assert.Equal(1, await CountAsync(connection, $"SELECT count(*) FROM \"{Root}\" WHERE \"Guid\" = @g",
                ("g", ModelScaffold.ExampleModelGuid)));
            Assert.Equal(scaffold.ModelId, (int)await CountAsync(connection,
                $"SELECT \"Id\" FROM \"{Root}\" WHERE \"Guid\" = @g", ("g", ModelScaffold.ExampleModelGuid)));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task Options_SetTheActiveAndLockedFlagsAsync(bool active, bool locked)
        {
            await using var scaffold = await ModelScaffold.CreateAsync(new ModelScaffoldOptions
                { ModelGuid = null, Active = active, Locked = locked, CreateCallers = false });
            await using var connection = await OpenAsync();
            Assert.Equal(active ? 1 : 0, await CountAsync(connection,
                $"SELECT \"Active\" FROM \"{Root}\" WHERE \"Id\" = @id", ("id", scaffold.ModelId)));
            Assert.Equal(locked ? 1 : 0, await CountAsync(connection,
                $"SELECT \"Locked\" FROM \"{Root}\" WHERE \"Id\" = @id", ("id", scaffold.ModelId)));
            Assert.Equal(string.Empty, scaffold.UserName);
        }

        [Fact]
        public async Task DefaultExclusions_LeaveRuntimeAndCaseDataAndArchiveOutAsync()
        {
            await using var scaffold = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            var excluded = new System.Text.RegularExpressions.Regex(ModelScaffold.DefaultExcludedTablePattern);
            Assert.DoesNotContain(scaffold.IdMap.Keys, excluded.IsMatch);
            Assert.DoesNotContain("Archive", scaffold.IdMap.Keys);
            Assert.DoesNotContain("Case", scaffold.IdMap.Keys);
        }

        [Fact]
        public async Task ExcludeTables_LeavesATableOutAndEverythingBelowItAsync()
        {
            await using var scaffold = await ModelScaffold.CreateAsync(new ModelScaffoldOptions
            {
                ModelGuid = null, CreateCallers = false,
                ExcludeTables = ["EntityAnalysisModelAbstractionRule"]
            });
            Assert.DoesNotContain("EntityAnalysisModelAbstractionRule", scaffold.IdMap.Keys);
            Assert.True(scaffold.IdMap.ContainsKey("EntityAnalysisModelActivationRule"));
        }

        [Fact]
        public async Task Dispose_LeavesNoRowsBehindAsync()
        {
            var scaffold = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            var idMap = scaffold.IdMap;
            var modelGuid = scaffold.ModelGuid;
            var tenant = scaffold.TenantRegistryId;
            var userNames = new[] { scaffold.UserName, scaffold.OtherUserName };
            Assert.True(scaffold.CopiedRows.Values.Sum() > 50);

            await scaffold.DisposeAsync();
            await scaffold.DisposeAsync();

            await using var connection = await OpenAsync();
            foreach (var (table, map) in idMap)
            {
                var remaining = await CountAsync(connection,
                    $"SELECT count(*) FROM \"{table}\" WHERE \"Id\" = ANY(@ids)",
                    ("ids", map.Values.ToArray()));
                Assert.True(remaining == 0, $"\"{table}\" still has {remaining} copied row(s).");
            }

            Assert.Equal(0,
                await CountAsync(connection, $"SELECT count(*) FROM \"{Root}\" WHERE \"Guid\" = @g", ("g", modelGuid)));
            Assert.Equal(0, await CountAsync(connection,
                "SELECT count(*) FROM \"EntityAnalysisModelRole\" WHERE \"EntityAnalysisModelGuid\" = @g",
                ("g", modelGuid)));
            Assert.Equal(0,
                await CountAsync(connection, "SELECT count(*) FROM \"TenantRegistry\" WHERE \"Id\" = @t",
                    ("t", tenant)));
            Assert.Equal(0,
                await CountAsync(connection, "SELECT count(*) FROM \"UserRegistry\" WHERE \"Name\" = ANY(@n)",
                    ("n", userNames)));
            Assert.Equal(0,
                await CountAsync(connection, "SELECT count(*) FROM \"UserInTenant\" WHERE \"User\" = ANY(@n)",
                    ("n", userNames)));
            Assert.Equal(0,
                await CountAsync(connection, "SELECT count(*) FROM \"RoleRegistry\" WHERE \"TenantRegistryId\" = @t",
                    ("t", tenant)));

            foreach (var table in await GuidLinkedAsync(connection))
            {
                Assert.Equal(0, await CountAsync(connection,
                    $"SELECT count(*) FROM \"{table}\" WHERE \"EntityAnalysisModelGuid\" = @g", ("g", modelGuid)));
            }
        }

        private static async Task<List<string>> GuidLinkedAsync(NpgsqlConnection connection)
        {
            var tables = new List<string>();
            await using var command = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.columns WHERE table_schema = 'public' " +
                "AND column_name = 'EntityAnalysisModelGuid' AND data_type = 'uuid'", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }

        [Fact]
        public async Task TwoScaffolds_LiveSideBySideAsync()
        {
            await using var first = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            var second = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);

            Assert.NotEqual(first.ModelGuid, second.ModelGuid);
            Assert.NotEqual(first.ModelId, second.ModelId);
            Assert.NotEqual(first.TenantRegistryId, second.TenantRegistryId);
            Assert.Equal(first.CopiedRows.OrderBy(k => k.Key), second.CopiedRows.OrderBy(k => k.Key));

            await second.DisposeAsync();

            await using var connection = await OpenAsync();
            foreach (var (table, map) in first.IdMap)
            {
                var present = await CountAsync(connection, $"SELECT count(*) FROM \"{table}\" WHERE \"Id\" = ANY(@ids)",
                    ("ids", map.Values.ToArray()));
                Assert.Equal(map.Count, present);
            }
        }

        [Fact]
        public async Task SuppliedTenant_IsKeptWhenTheScaffoldIsRemovedAsync()
        {
            await using var owner = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            var guest = await ModelScaffold.CreateAsync(new ModelScaffoldOptions
                { TenantRegistryId = owner.TenantRegistryId, ModelGuid = null });

            Assert.False(guest.OwnsTenant);
            Assert.Equal(owner.TenantRegistryId, guest.TenantRegistryId);
            Assert.Equal(owner.TenantName, guest.TenantName);

            await guest.DisposeAsync();

            await using var connection = await OpenAsync();
            Assert.Equal(1, await CountAsync(connection, "SELECT count(*) FROM \"TenantRegistry\" WHERE \"Id\" = @t",
                ("t", owner.TenantRegistryId)));
            Assert.Equal(1, await CountAsync(connection, $"SELECT count(*) FROM \"{Root}\" WHERE \"Id\" = @id",
                ("id", owner.ModelId)));
            Assert.Equal(0, await CountAsync(connection, $"SELECT count(*) FROM \"{Root}\" WHERE \"Id\" = @id",
                ("id", guest.ModelId)));
        }

        [Fact]
        public async Task NamePrefix_MustStartWithTheFixturePrefixAsync()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                ModelScaffold.CreateAsync(new ModelScaffoldOptions { NamePrefix = "NotAFixturePrefix" }));
            Assert.Contains(Fixture.Prefix, ex.Message);
        }

        [Fact]
        public async Task MissingSourceModel_FailsNamingTheModel_AndLeavesNothingBehindAsync()
        {
            await using var connection = await OpenAsync();
            var tenantsBefore = await CountAsync(connection,
                "SELECT count(*) FROM \"TenantRegistry\" WHERE \"Name\" LIKE 'ZzTest%'");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ModelScaffold.CreateAsync(new ModelScaffoldOptions { SourceModelId = 987_654_321, ModelGuid = null }));

            Assert.Contains("Source model 987654321", ex.Message);
            Assert.Contains("does not exist", ex.Message);
            Assert.Equal(tenantsBefore, await CountAsync(connection,
                "SELECT count(*) FROM \"TenantRegistry\" WHERE \"Name\" LIKE 'ZzTest%'"));
        }

        [Fact]
        public async Task IncludeTables_NamingAnUnreachableTable_FailsLoudlyAsync()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ModelScaffold.CreateAsync(new ModelScaffoldOptions
                    { ModelGuid = null, IncludeTables = ["TenantRegistry"] }));
            Assert.Contains("IncludeTables names \"TenantRegistry\"", ex.Message);
        }

        [Fact]
        public async Task ATableWithoutAnIdentityId_FailsNamingTheTable_AndLeavesNothingBehindAsync()
        {
            const string table = "ZzTestScaffoldNoIdChild";
            await using var connection = await OpenAsync();
            await using (var create = new NpgsqlCommand(
                             $"CREATE TABLE \"{table}\" (\"EntityAnalysisModelId\" integer NOT NULL " +
                             $"REFERENCES \"{Root}\"(\"Id\"), \"Value\" integer)", connection))
            {
                await create.ExecuteNonQueryAsync();
            }

            try
            {
                var tenantsBefore = await CountAsync(connection,
                    "SELECT count(*) FROM \"TenantRegistry\" WHERE \"Name\" LIKE 'ZzTest%'");

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated));

                Assert.Contains($"Table \"{table}\" has no identity \"Id\" column", ex.Message);
                Assert.Contains("ExcludeTables", ex.Message);
                Assert.Equal(tenantsBefore, await CountAsync(connection,
                    "SELECT count(*) FROM \"TenantRegistry\" WHERE \"Name\" LIKE 'ZzTest%'"));

                await using var scaffold = await ModelScaffold.CreateAsync(new ModelScaffoldOptions
                    { ModelGuid = null, ExcludeTables = [table] });
                Assert.True(scaffold.ModelId > 0);
            }
            finally
            {
                await using var drop = new NpgsqlCommand($"DROP TABLE IF EXISTS \"{table}\"", connection);
                await drop.ExecuteNonQueryAsync();
            }
        }
    }
}