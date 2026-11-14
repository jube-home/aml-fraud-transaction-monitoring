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
using Jube.Data.Query.GetPostgresSchemaSnapshotQuery;
using Jube.Data.Reporting.Check;
using Npgsql;
using Xunit;

namespace Jube.Test.Reporting.Check
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SqlSchemaCheckerDifferentialTests : IAsyncLifetime
    {
        private static readonly string connectionString =
            (Environment.GetEnvironmentVariable("JubeTestConnectionString")
             ?? Environment.GetEnvironmentVariable("ConnectionString"))!;

        private SqlSchemaSnapshot snapshot = null!;

        public async Task InitializeAsync()
        {
            await ExecuteScriptAsync(SqlCheckCorpus.DropSchema);
            await ExecuteScriptAsync(SqlCheckCorpus.Schema);

            snapshot = new SqlSchemaSnapshot(await new GetPostgresSchemaSnapshotQuery(connectionString).ExecuteAsync());
        }

        public async Task DisposeAsync()
        {
            await ExecuteScriptAsync(SqlCheckCorpus.DropSchema);
        }

        private static async Task ExecuteScriptAsync(string script)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(script, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task<string?> DatabaseRejectionAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 10 };

            try
            {
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                }

                return null;
            }
            catch (PostgresException ex)
            {
                return ex.SqlState;
            }
        }

        private static IEnumerable<IReadOnlyDictionary<string, string>?> Modes()
        {
            yield return null;
            yield return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["zztest_orders"] = null!,
                ["zztest_items"] = null!,
                ["ZzTestMixed"] = null!,
                ["zztest_customers"] = "SELECT t0.* FROM zztest_customers t0 WHERE 1 = 1"
            };
        }

        private static bool Matches(string actual, string expected)
        {
            return actual == expected || (!expected.EndsWith("Suggest", StringComparison.Ordinal) &&
                                          actual == expected + "Suggest");
        }

        [Fact]
        public void TheCorpusHasAtLeastOneHundredAndFiftyStatements()
        {
            (SqlCheckCorpus.Good.Length + SqlCheckCorpus.Bad.Length).Should().BeGreaterThanOrEqualTo(150);
        }

        [Fact]
        public async Task EveryGoodStatementPassesTheCheckerAndTheDatabaseAcceptsItAsync()
        {
            var problems = new List<string>();

            foreach (var allowed in Modes())
            {
                foreach (var sql in SqlCheckCorpus.Good)
                {
                    var findings = SqlPreflight.Check(sql, snapshot, allowed);

                    if (findings.Count > 0)
                    {
                        problems.Add($"[over-strict, allowed={allowed != null}] {sql} => {string.Join("; ", findings.Select(f => f.Code + ":" + f.English))}");
                    }
                }
            }

            foreach (var sql in SqlCheckCorpus.Good)
            {
                var state = await DatabaseRejectionAsync(sql);

                if (state != null)
                {
                    problems.Add($"[corpus error, the database rejects a statement listed as good, {state}] {sql}");
                }
            }

            problems.Should().BeEmpty();
        }

        [Fact]
        public async Task EveryBadStatementIsRejectedByTheCheckerBeforeTheDatabaseAsync()
        {
            var gaps = new List<string>();

            foreach (var allowed in Modes())
            {
                foreach (var (sql, code) in SqlCheckCorpus.Bad)
                {
                    var findings = SqlPreflight.Check(sql, snapshot, allowed);

                    if (findings.Count == 0)
                    {
                        gaps.Add($"[validator gap, allowed={allowed != null}] {sql}");
                    }
                    else if (findings.All(f => !Matches(f.Code, code)))
                    {
                        gaps.Add($"[wrong code, allowed={allowed != null}, expected {code}] {sql} => {string.Join("; ", findings.Select(f => f.Code))}");
                    }
                }
            }

            foreach (var (sql, _) in SqlCheckCorpus.Bad)
            {
                var state = await DatabaseRejectionAsync(sql);

                if (state == null)
                {
                    gaps.Add($"[corpus error, the database accepts a statement listed as bad] {sql}");
                }
            }

            gaps.Take(60).Should().BeEmpty();
        }

        [Fact]
        public void ATableThatExistsButIsNotAllowedIsRefusedAsUnknownWithoutNamingItAsExisting()
        {
            var allowed = new Dictionary<string, string> { ["zztest_orders"] = null! };

            var findings = SqlPreflight.Check("select * from zztest_customers", snapshot, allowed);

            findings.Should().ContainSingle().Which.Code.Should().Be(SqlCheckMessages.UnknownTable);
            findings[0].English.Should().Contain("that you may query");
        }

        [Fact]
        public void ASuggestionOnlyNamesATablePermittedToTheCaller()
        {
            var allowed = new Dictionary<string, string> { ["zztest_orders"] = null! };

            var findings = SqlPreflight.Check("select * from zztest_ordres", snapshot, allowed);

            findings.Should().ContainSingle().Which.Arguments.Should().Contain("zztest_orders");

            SqlPreflight.Check("select * from zztest_custmers", snapshot, allowed).Single().Arguments
                .Should().NotContain("zztest_customers");
        }
    }
}
