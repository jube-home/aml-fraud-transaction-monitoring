/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Data.Reporting;
using Jube.Service.DynamicSql;
using Jube.Test.Infrastructure.DatabaseFixture;
using log4net;
using Npgsql;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class DynamicSqlTemplateSchemaSweepTests
    {
        private static readonly string connectionString =
            (Environment.GetEnvironmentVariable("JubeTestConnectionString")
             ?? Environment.GetEnvironmentVariable("ConnectionString"))!;

        [Fact]
        public async Task EveryRegisteredTemplateExplainsAgainstTheLiveSchemaAsync()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("jube_sweep_probe", _ => true, 1);

            allowed.Keys.Should().BeEquivalentTo(DynamicSqlTableAccess.RegisteredTables,
                "a registered table whose template never materialises under full permissions and a conversation "
                + "is a registration that can never be used");

            using var postgres = new Postgres(connectionString, LogManager.GetLogger(GetType()));
            var failures = new List<string>();

            foreach (var table in allowed.Keys.OrderBy(name => name, StringComparer.Ordinal))
            {
                try
                {
                    await postgres.ExplainWithTableTokenSwitchAsync($"SELECT * FROM \"{table}\"", allowed);
                }
                catch (Exception ex)
                {
                    failures.Add($"{table}: {ex.Message}");
                }
            }

            failures.Should().BeEmpty(
                "every template must reference only live tables and columns: " + string.Join("; ", failures));
        }

        [Fact]
        public async Task EveryLiveTableIsEitherRegisteredOrBlockedInProtectedModeAsync()
        {
            var registered = new HashSet<string>(DynamicSqlTableAccess.RegisteredTables, StringComparer.OrdinalIgnoreCase);
            var unprotected = new List<string>();

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "select table_name from information_schema.tables where table_schema = 'public' "
                + "and table_type = 'BASE TABLE'", connection);

            var tables = new List<string>();

            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            foreach (var table in tables.Where(t => !registered.Contains(t)
                                                    && t != "VersionInfo"
                                                    && !t.StartsWith("pg_", StringComparison.OrdinalIgnoreCase)
                                                    && !t.StartsWith(DatabaseFixture.Prefix,
                                                        StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    PostgresSqlValidator.AssertSelectOnly($"SELECT * FROM \"{table}\"", true);
                    unprotected.Add(table);
                }
                catch (InvalidOperationException)
                {
                }
            }

            unprotected.Should().BeEmpty(
                "a live table outside the dynamic SQL registry must sit on the protected-relations deny list, "
                + "so the deny-mode reporting paths cannot read it either: " + string.Join(", ", unprotected));
        }
    }
}
