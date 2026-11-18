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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB.Data;
using Npgsql;
using Xunit;

namespace Jube.Test.Migrations
{
    [Collection("Database")]
    [Trait("Category", "Service")]
    public sealed class ScaleSeedScriptTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string SeedPath = "Jube.Migrations/Scripts/ScaleSeed.sql";
        private const string BenchmarkPath = "Jube.Migrations/Scripts/ScaleBenchmark.sql";
        private const int Archive = 2000;
        private const int Cases = 300;
        private const string Workflow = "(select \"Guid\" from \"CaseWorkflow\" where \"Name\" = 'Scale: Case Volume')";

        private static readonly string[] cleanup =
        [
            $"delete from \"CaseNote\" where \"CaseId\" in (select \"Id\" from \"Case\" where \"CaseWorkflowGuid\" = {Workflow})",
            $"delete from \"CaseRiskAssessment\" where \"CaseId\" in (select \"Id\" from \"Case\" where \"CaseWorkflowGuid\" = {Workflow})",
            $"delete from \"CaseStatusTransition\" where \"CaseId\" in (select \"Id\" from \"Case\" where \"CaseWorkflowGuid\" = {Workflow})",
            $"delete from \"CaseEvent\" where \"CaseId\" in (select \"Id\" from \"Case\" where \"CaseWorkflowGuid\" = {Workflow})",
            $"delete from \"Case\" where \"CaseWorkflowGuid\" = {Workflow}",
            "delete from \"ArchiveKey\" where \"EntityAnalysisModelInstanceEntryGuid\" in (select \"EntityAnalysisModelInstanceEntryGuid\" from \"Archive\" where \"EntryKeyValue\" like 'SCL-TXN-%')",
            "delete from \"Archive\" where \"EntryKeyValue\" like 'SCL-TXN-%'",
            "delete from \"CaseWorkflowStatusRole\" where \"CaseWorkflowStatusGuid\" in (select s.\"Guid\" from \"CaseWorkflowStatus\" s join \"CaseWorkflow\" w on w.\"Id\" = s.\"CaseWorkflowId\" where w.\"Name\" = 'Scale: Case Volume')",
            "delete from \"CaseWorkflowStatus\" where \"CaseWorkflowId\" in (select \"Id\" from \"CaseWorkflow\" where \"Name\" = 'Scale: Case Volume')",
            $"delete from \"CaseWorkflowRole\" where \"CaseWorkflowGuid\" = {Workflow}",
            "delete from \"CaseWorkflow\" where \"Name\" = 'Scale: Case Volume'"
        ];

        private static readonly Regex variable = new(@":'(?<name>[a-z]+)'", RegexOptions.CultureInvariant);

        private DbContext? context;
        private bool protectedData;

        private DbContext Db => context!;

        public async Task InitializeAsync()
        {
            context = fx.GetDbContext();

            var existing = await Db.ExecuteAsync<int>("select count(*)::int from \"Archive\" where \"EntryKeyValue\" like 'SCL-TXN-%'");

            protectedData = existing > Archive;

            if (!protectedData)
            {
                await CleanAsync();
            }
        }

        public async Task DisposeAsync()
        {
            if (context != null)
            {
                if (!protectedData)
                {
                    await CleanAsync();
                }

                await context.DisposeAsync();
            }
        }

        private async Task CleanAsync()
        {
            foreach (var sql in cleanup)
            {
                await Db.ExecuteAsync(sql);
            }
        }

        private async Task<bool> ExampleModelAsync()
        {
            if (protectedData)
            {
                return false;
            }

            return await Db.ExecuteAsync<int>(
                       "select count(*)::int from \"EntityAnalysisModel\" m where m.\"Guid\" = '90c425fd-101a-420b-91d1-cb7a24a969cc' and coalesce(m.\"Deleted\", 0) = 0") > 0
                   && await Db.ExecuteAsync<int>(
                       "select count(*)::int from \"EntityAnalysisModelActivationRule\" r join \"EntityAnalysisModel\" m on m.\"Id\" = r.\"EntityAnalysisModelId\" " +
                       "where m.\"Guid\" = '90c425fd-101a-420b-91d1-cb7a24a969cc' and r.\"EnableCaseWorkflow\" = 1 and coalesce(r.\"Deleted\", 0) = 0") > 0
                   && await Db.ExecuteAsync<int>("select count(*)::int from \"RoleRegistry\" where \"Id\" = 1") > 0;
        }

        private async Task RunSeedAsync()
        {
            var script = await File.ReadAllTextAsync(Path.Combine(SqlScriptSteps.SolutionDirectory(), SeedPath));

            await using var connection = new NpgsqlConnection(fx.ConnectionString);
            await connection.OpenAsync();

            await using (var settings = new NpgsqlCommand(
                             "select set_config('jube.scale_archive', @a, false), set_config('jube.scale_cases', @c, false)", connection))
            {
                settings.Parameters.AddWithValue("a", Archive.ToString());
                settings.Parameters.AddWithValue("c", Cases.ToString());
                await settings.ExecuteNonQueryAsync();
            }

            foreach (var (_, sql) in SqlScriptSteps.Split(script))
            {
                await using var command = new NpgsqlCommand(sql, connection);
                command.CommandTimeout = 0;
                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task<string?> PlanAsync(NpgsqlConnection connection, string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 0;

            try
            {
                await command.ExecuteNonQueryAsync();

                return null;
            }
            catch (PostgresException exception)
            {
                return exception.MessageText;
            }
        }

        private Task<int> CountAsync(string sql)
        {
            return Db.ExecuteAsync<int>(sql);
        }

        [Fact]
        public async Task TheScriptBuildsTheRequestedVolumesLinksEveryCaseToATransactionAndStopsOnASecondRunAsync()
        {
            if (!await ExampleModelAsync())
            {
                return;
            }

            await RunSeedAsync();

            const int expectedCases = Archive / (Archive / Cases);

            (await CountAsync("select count(*)::int from \"Archive\" where \"EntryKeyValue\" like 'SCL-TXN-%'")).Should().Be(Archive);
            (await CountAsync(
                "select count(*)::int from \"ArchiveKey\" k join \"Archive\" a on a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\" where a.\"EntryKeyValue\" like 'SCL-TXN-%'"))
                .Should().Be(Archive * 3, "every transaction has its account, amount and country key");
            (await CountAsync($"select count(*)::int from \"Case\" where \"CaseWorkflowGuid\" = {Workflow}")).Should().Be(expectedCases);

            (await CountAsync(
                $"select count(*)::int from \"Case\" c where c.\"CaseWorkflowGuid\" = {Workflow} and not exists (select 1 from \"Archive\" a where a.\"EntityAnalysisModelInstanceEntryGuid\" = c.\"EntityAnalysisModelInstanceEntryGuid\" and a.\"ActivationRuleCount\" > 0)"))
                .Should().Be(0, "every case is attached to an alerted transaction");
            (await CountAsync(
                $"select count(*)::int from \"Case\" c where c.\"CaseWorkflowGuid\" = {Workflow} and (select count(*) from \"CaseStatusTransition\" t where t.\"CaseId\" = c.\"Id\") <> 1"))
                .Should().Be(0);
            (await CountAsync(
                $"select count(*)::int from \"Case\" c where c.\"CaseWorkflowGuid\" = {Workflow} and (select count(*) from \"CaseRiskAssessment\" t where t.\"CaseId\" = c.\"Id\") <> 1"))
                .Should().Be(0);
            (await CountAsync(
                $"select count(*)::int from \"CaseStatusTransition\" t join \"Case\" c on c.\"Id\" = t.\"CaseId\" where c.\"CaseWorkflowGuid\" = {Workflow} and t.\"ExitedDate\" is null and c.\"ClosedStatusId\" <> 0"))
                .Should().Be(0, "only open cases have an open transition");

            var open = await CountAsync($"select count(*)::int from \"Case\" where \"CaseWorkflowGuid\" = {Workflow} and \"ClosedStatusId\" = 0");
            open.Should().BeInRange(expectedCases / 20, expectedCases * 3 / 10, "about a seventh of the cases are open");

            (await CountAsync(
                "select count(*)::int from \"CaseWorkflowStatusRole\" g join \"CaseWorkflowStatus\" s on s.\"Guid\" = g.\"CaseWorkflowStatusGuid\" join \"CaseWorkflow\" w on w.\"Id\" = s.\"CaseWorkflowId\" where w.\"Name\" = 'Scale: Case Volume'"))
                .Should().BeGreaterThanOrEqualTo(2, "the workflow is granted to the administrator role so its cases are visible");

            await RunSeedAsync();

            (await CountAsync("select count(*)::int from \"Archive\" where \"EntryKeyValue\" like 'SCL-TXN-%'")).Should().Be(Archive);
            (await CountAsync($"select count(*)::int from \"Case\" where \"CaseWorkflowGuid\" = {Workflow}")).Should().Be(expectedCases);
        }

        [Fact]
        public async Task EveryStatementOfTheBenchmarkFileIsValidAgainstTheSchemaAsync()
        {
            if (!await ExampleModelAsync())
            {
                return;
            }

            await RunSeedAsync();

            var lines = (await File.ReadAllTextAsync(Path.Combine(SqlScriptSteps.SolutionDirectory(), BenchmarkPath))).Split('\n');

            await using var connection = new NpgsqlConnection(fx.ConnectionString);
            await connection.OpenAsync();

            var variables = new Dictionary<string, string>(StringComparer.Ordinal);
            var statements = new List<string>();
            var pending = new List<string>();

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("--", StringComparison.Ordinal)
                                                    || line.StartsWith("\\echo", StringComparison.Ordinal)
                                                    || line.StartsWith("\\set", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.EndsWith("\\gset", StringComparison.Ordinal))
                {
                    await using var command = new NpgsqlCommand(line[..^"\\gset".Length], connection);
                    await using var reader = await command.ExecuteReaderAsync();

                    (await reader.ReadAsync()).Should().BeTrue("the benchmark's sample picks must find a row: " + line);

                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        variables[reader.GetName(i)] = Convert.ToString(reader.GetValue(i))!;
                    }

                    continue;
                }

                pending.Add(line);

                if (!line.EndsWith(';'))
                {
                    continue;
                }

                statements.Add(string.Join("\n", pending));
                pending.Clear();
            }

            pending.Should().BeEmpty("every statement in the benchmark file ends with a semicolon");
            statements.Count(s => s.StartsWith("EXPLAIN", StringComparison.Ordinal)).Should().BeGreaterThanOrEqualTo(20);

            var prepared = statements.Select(statement => variable.Replace(
                statement.Replace("EXPLAIN (ANALYZE, BUFFERS)", "EXPLAIN", StringComparison.Ordinal), m =>
                    "'" + variables.GetValueOrDefault(m.Groups["name"].Value, string.Empty)
                        .Replace("'", "''", StringComparison.Ordinal) + "'")).ToList();

            foreach (var sql in prepared)
            {
                (await PlanAsync(connection, sql)).Should()
                    .BeNull("the benchmark statement must plan against the current schema: " + sql);
            }
        }
    }
}
