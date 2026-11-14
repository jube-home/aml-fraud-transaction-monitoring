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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Reporting;
using Jube.Service.DynamicSql;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowChecklistItem
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SeededCaseChecklistTests(DatabaseFixture fx)
    {
        private const string WorkflowName = "Detailed Account Financial Transactions Cases";

        private static string Connection()
        {
            return Environment.GetEnvironmentVariable("JubeTestConnectionString")
                   ?? Environment.GetEnvironmentVariable("ConnectionString")
                   ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;";
        }

        private async Task<List<(string Status, string Checklist, int ChecklistPriority, Data.Poco.CaseWorkflowChecklistItem Item)>>
            SeededAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var rows = await (from w in dbContext.CaseWorkflow
                join s in dbContext.CaseWorkflowStatus on w.Id equals s.CaseWorkflowId
                join c in dbContext.GetTable<Data.Poco.CaseWorkflowChecklist>() on s.Guid equals c.CaseWorkflowStatusGuid
                join i in dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>() on c.Guid equals i.CaseWorkflowChecklistGuid
                where w.Name == WorkflowName && c.CreatedUser == "Administrator" && i.CreatedUser == "Administrator"
                select new { Status = s.Name, Checklist = c.Name, c.Priority, Item = i }).ToListAsync();

            return rows.Select(r => (r.Status, r.Checklist, r.Priority.GetValueOrDefault(), r.Item)).ToList();
        }

        [Fact]
        public async Task TheDemonstrationWorkflowCarriesThreeChecklistsAndSixItemsInPriorityOrder()
        {
            var seeded = await SeededAsync();

            seeded.Select(s => s.Checklist).Distinct().Should().BeEquivalentTo(
                "Initial Triage", "Supervisor Sign-off", "Fraud Confirmation");
            seeded.Should().HaveCount(6);

            seeded.Where(s => s.Status == "First Line Review").OrderBy(s => s.Item.Priority)
                .Select(s => s.Item.Name).Should().Equal(
                    "Review the triggering transactions", "Record an investigation note", "Contact the customer",
                    "Check related cases on the case key");
        }

        [Fact]
        public async Task ShowValidationAndSlaAreEachOptionalAndStoredAsNullWhenAbsent()
        {
            var seeded = (await SeededAsync()).Select(s => s.Item).ToList();

            var plain = seeded.Single(s => s.Name == "Review the triggering transactions");
            plain.ShowSql.Should().BeNull();
            plain.ValidateSql.Should().BeNull();
            plain.SlaMinutes.Should().BeNull();

            var validateOnly = seeded.Single(s => s.Name == "Record an investigation note");
            validateOnly.ShowSql.Should().BeNull();
            validateOnly.ValidateSql.Should().NotBeNullOrWhiteSpace();
            validateOnly.SlaMinutes.Should().Be(60);

            var both = seeded.Single(s => s.Name == "Contact the customer");
            both.ShowSql.Should().NotBeNullOrWhiteSpace();
            both.ValidateSql.Should().NotBeNullOrWhiteSpace();

            var showOnly = seeded.Single(s => s.Name == "Check related cases on the case key");
            showOnly.ShowSql.Should().NotBeNullOrWhiteSpace();
            showOnly.ValidateSql.Should().BeNull();
            showOnly.SlaMinutes.Should().Be(120);

            seeded.Single(s => s.Name == "Record the finding").SlaMinutes.Should().BeNull();
        }

        [Fact]
        public async Task TheBackfillSwitchesAConditionOnExactlyWhereTheSeededItemHasItsSql()
        {
            var seeded = (await SeededAsync()).Select(s => s.Item).ToList();

            seeded.Should().NotBeEmpty();

            foreach (var item in seeded)
            {
                item.EnableShow.Should().Be((byte)(string.IsNullOrWhiteSpace(item.ShowSql) ? 0 : 1), item.Name);
                item.EnableValidation.Should()
                    .Be((byte)(string.IsNullOrWhiteSpace(item.ValidateSql) ? 0 : 1), item.Name);
            }

            seeded.Count(c => c.EnableShow == 1).Should().Be(2);
            seeded.Count(c => c.EnableValidation == 1).Should().Be(4);
        }

        [Fact]
        public async Task NoItemOrItsVersionRowIsLeftWithoutAnExplicitSwitchAfterTheBackfill()
        {
            await using var dbContext = fx.GetDbContext();

            (await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>()
                .CountAsync(w => w.EnableShow == null || w.EnableValidation == null)).Should().Be(0);
            (await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItemVersion>()
                .CountAsync(w => w.EnableShow == null || w.EnableValidation == null)).Should().Be(0);
        }

        [Fact]
        public async Task EverySeededSqlRunsAsASingleBooleanAgainstARealCase()
        {
            await using var dbContext = fx.GetDbContext();
            var caseRow = await dbContext.GetTable<Data.Poco.Case>().OrderBy(o => o.Id).FirstOrDefaultAsync();

            var caseId = caseRow?.Id ?? 0;
            var caseKeyValue = caseRow?.CaseKeyValue ?? string.Empty;

            var allowed = DynamicSqlTableAccess.BuildVisualisationAllowedTables("Administrator", _ => true);
            var statements = (await SeededAsync()).SelectMany(s => new[] { s.Item.ShowSql, s.Item.ValidateSql })
                .Where(sql => !string.IsNullOrWhiteSpace(sql)).ToList();

            statements.Should().HaveCount(6);

            foreach (var sql in statements)
            {
                using var postgres = new Postgres(Connection(), TestLog.NoOp);

                var result = await postgres.ExecuteLimitedByNamedParametersAsync(sql!,
                    new Dictionary<string, object> { ["Id"] = caseId, ["CaseKeyValue"] = caseKeyValue }, allowed, 2,
                    CancellationToken.None);

                result.Columns.Count.Should().Be(1, sql);
                result.Rows.Count.Should().Be(1, sql);
                result.Rows[0][0].Should().BeOfType<bool>(sql);
            }
        }
    }
}
