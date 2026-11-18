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
    public sealed class SmokeTestSeedTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string CataloguePath = "docs/Testing/SmokeTestSeedData.md";

        private static readonly Regex catalogueRow =
            new(@"^\|\s*`(?<code>[A-Z0-9-]+)`\s*\|\s*(?<count>\d+)\s*\|", RegexOptions.CultureInvariant);

        private static readonly Regex keyCode = new(@"^SMK-(?<code>.+?)(-\d\d)?$", RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, string> exactKeys = new(StringComparer.Ordinal)
        {
            ["ED-DUPLICATE-OPEN"] = "SMK-ED-DUPLICATE-OPEN",
            ["ER-OPEN-CASE"] = "SMK-ER-OPEN",
            ["ER-LEGAL-HOLD"] = "SMK-ER-HELD",
            ["ER-RETENTION"] = "SMK-ER-RETAINED",
            ["ER-ERASABLE"] = "SMK-ER-OLD",
            ["ED-KEY-LONG"] = "SMK-LONG-" + new string('X', 600)
        };

        private static readonly HashSet<string> unsearchable = new(StringComparer.Ordinal)
        {
            "ED-KEY-RTL", "ED-KEY-EMOJI", "ED-KEY-EMPTY", "ED-KEY-NULL"
        };

        private DbContext? context;
        private bool seeded;

        private DbContext Db => context!;

        public async Task InitializeAsync()
        {
            context = fx.GetDbContext();
            seeded = await Db.ExecuteAsync<int>(
                "select count(*)::int from \"Case\" where \"CaseKeyValue\" like 'SMK-%'") > 0;
        }

        public async Task DisposeAsync()
        {
            if (context != null)
            {
                await context.DisposeAsync();
            }
        }

        private static string Code(string code)
        {
            return $"\"CaseKeyValue\" like 'SMK-{code}-__'";
        }

        private Task<int> CountAsync(string where)
        {
            return Db.ExecuteAsync<int>($"select count(*)::int from \"Case\" where {where}");
        }

        private Task<int> ViolationsAsync(string code, string violation)
        {
            return Db.ExecuteAsync<int>(
                $"select count(*)::int from \"Case\" k where {Code(code)} and ({violation})");
        }

        private static string Granted(string status, string roleGuid)
        {
            return "select count(*)::int from \"CaseWorkflowStatusRole\" g join \"CaseWorkflowStatus\" s on s.\"Guid\" = g.\"CaseWorkflowStatusGuid\" " +
                   $"where s.\"Name\" = '{status}' and g.\"RoleRegistryGuid\" = {roleGuid} and coalesce(g.\"Deleted\", 0) = 0";
        }

        private static string WorkflowGrant(string roleGuid)
        {
            return "select count(*)::int from \"CaseWorkflowRole\" g join \"CaseWorkflow\" w on w.\"Guid\" = g.\"CaseWorkflowGuid\" " +
                   $"where w.\"Name\" = 'Smoke: Role Gated' and g.\"RoleRegistryGuid\" = {roleGuid} and coalesce(g.\"Deleted\", 0) = 0";
        }

        private static string Override(string condition)
        {
            return $"not exists (select 1 from \"CaseChecklistOverride\" o where o.\"CaseId\" = k.\"Id\" and {condition})";
        }

        private async Task ExpectAsync(string sql, string sqlState)
        {
            await Db.BeginTransactionAsync();

            try
            {
                var act = async () => await Db.ExecuteAsync(sql);

                (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(sqlState);
            }
            finally
            {
                await Db.RollbackTransactionAsync();
            }
        }

        private Task<int> CaseIdAsync(string code)
        {
            return Db.ExecuteAsync<int>($"select \"Id\" from \"Case\" where {Code(code)} order by \"Id\" limit 1");
        }

        private static string Catalogue()
        {
            var path = Path.Combine(SqlScriptSteps.SolutionDirectory(), CataloguePath);

            return File.ReadAllText(path);
        }

        private static Dictionary<string, int> CatalogueCounts()
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var inScenarios = false;

            foreach (var line in Catalogue().Split('\n'))
            {
                if (line.StartsWith("## Scenarios", StringComparison.Ordinal))
                {
                    inScenarios = true;

                    continue;
                }

                if (line.StartsWith("## ", StringComparison.Ordinal) && inScenarios)
                {
                    break;
                }

                var match = catalogueRow.Match(line);

                if (inScenarios && match.Success)
                {
                    counts[match.Groups["code"].Value] = int.Parse(match.Groups["count"].Value);
                }
            }

            return counts;
        }

        [Fact]
        public async Task EveryScenarioInTheCatalogueHasExactlyTheCasesItPromisesAsync()
        {
            if (!seeded)
            {
                return;
            }

            var catalogue = CatalogueCounts();
            catalogue.Count.Should().BeGreaterThan(150, "the catalogue page lists every scenario of the seed");

            var keys = await Db.QueryToListAsync<string>(
                "select \"CaseKeyValue\" from \"Case\" " +
                "where \"CaseKeyValue\" ~ '^SMK-[A-Z0-9]' and \"CaseKeyValue\" not like 'SMK-VOL-%'");

            var byCode = keys
                .GroupBy(k => exactKeys.ContainsValue(k) ? k : keyCode.Match(k).Groups["code"].Value)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

            var problems = new List<string>();

            foreach (var (code, expected) in catalogue.Where(c => !unsearchable.Contains(c.Key)))
            {
                var key = exactKeys.GetValueOrDefault(code, code);
                var actual = byCode.GetValueOrDefault(key);

                if (actual != expected)
                {
                    problems.Add($"{code}: the catalogue promises {expected} case(s) and the database has {actual}");
                }
            }

            var documented = catalogue.Keys.Select(c => exactKeys.GetValueOrDefault(c, c)).ToHashSet(StringComparer.Ordinal);
            var unknown = byCode.Keys.Where(k => !documented.Contains(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();

            problems.AddRange(unknown.Select(k => $"{k}: seeded cases that the catalogue does not list"));

            (await CountAsync("\"CaseKeyValue\" like 'SMK-العميل-__'")).Should().Be(catalogue["ED-KEY-RTL"]);
            (await CountAsync("\"CaseKeyValue\" like 'SMK-💳🚨-__'")).Should().Be(catalogue["ED-KEY-EMOJI"]);
            (await CountAsync("\"CaseKeyValue\" = ''")).Should().Be(catalogue["ED-KEY-EMPTY"]);
            (await Db.ExecuteAsync<int>(
                "select count(*)::int from \"Case\" where \"CaseKeyValue\" is null and \"CaseKey\" = 'AccountId'"))
                .Should().BeGreaterThanOrEqualTo(catalogue["ED-KEY-NULL"]);

            problems.Should().BeEmpty("the seed and docs/Testing/SmokeTestSeedData.md must describe the same cases");
        }

        [Fact]
        public async Task EverySmokeCaseHasItsTransactionAndKeysExceptTheOneThatMustNotAsync()
        {
            if (!seeded)
            {
                return;
            }

            var withoutArchive = await Db.QueryToListAsync<string>(
                "select k.\"CaseKeyValue\" from \"Case\" k where k.\"CaseKeyValue\" like 'SMK-%' " +
                "and not exists (select 1 from \"Archive\" a where a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\")");

            withoutArchive.Should().OnlyContain(r => r.StartsWith("SMK-ED-NO-ARCHIVE-", StringComparison.Ordinal));
            withoutArchive.Should().HaveCount(1);

            var withoutKeys = await Db.ExecuteAsync<int>(
                "select count(*)::int from \"Case\" k where k.\"CaseKeyValue\" like 'SMK-%' " +
                "and exists (select 1 from \"Archive\" a where a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\") " +
                "and not exists (select 1 from \"ArchiveKey\" a where a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\" and a.\"Key\" = 'AccountId')");

            withoutKeys.Should().Be(0, "the case key journal reads the archive key table, so every case needs its keys");
        }

        [Fact]
        public async Task TheVisibilityScenariosHideAndShowTheCasesTheyAreMeantToAsync()
        {
            if (!seeded)
            {
                return;
            }

            const string role = "(select \"Guid\" from \"RoleRegistry\" where \"Id\" = 1)";
            const string analyst = "(select \"Guid\" from \"RoleRegistry\" where \"Name\" = 'Smoke: Case Analyst')";

            (await Db.ExecuteAsync<int>(
                "select count(*)::int from \"RoleRegistry\" where \"Name\" = 'Smoke: Case Analyst'")).Should().Be(1);
            (await Db.ExecuteAsync<int>(Granted("Smoke: Ungranted", role))).Should().Be(0);
            (await Db.ExecuteAsync<int>(
                "select count(*)::int from \"CaseWorkflowStatusRole\" g join \"CaseWorkflowStatus\" s on s.\"Guid\" = g.\"CaseWorkflowStatusGuid\" where s.\"Name\" = 'Smoke: Ungranted' and coalesce(g.\"Deleted\", 0) = 0"))
                .Should().Be(0, "an ungranted status is granted to no role at all");
            (await Db.ExecuteAsync<int>(Granted("Smoke: Admin Only", role))).Should().Be(1);
            (await Db.ExecuteAsync<int>(Granted("Smoke: Admin Only", analyst))).Should().Be(0);
            (await Db.ExecuteAsync<int>(Granted("Smoke: Inactive", role))).Should().Be(1);
            (await Db.ExecuteAsync<int>(
                "select count(*)::int from \"CaseWorkflowStatus\" where \"Name\" = 'Smoke: Inactive' and \"Active\" = 0"))
                .Should().Be(1);
            (await Db.ExecuteAsync<int>(
                "select count(*)::int from \"CaseWorkflowStatus\" where \"Name\" = 'Smoke: Deleted' and \"Deleted\" = 1"))
                .Should().Be(1);

            (await Db.ExecuteAsync<int>(WorkflowGrant(role))).Should().Be(0);
            (await Db.ExecuteAsync<int>(WorkflowGrant(analyst))).Should().Be(1);
        }

        [Fact]
        public async Task TheChecklistScenariosCarryTheItemsAndOverridesThatDriveTheGateAsync()
        {
            if (!seeded)
            {
                return;
            }

            const string definitions =
                "(select count(*) from \"CaseWorkflowChecklistItem\" i join \"CaseWorkflowChecklist\" c on c.\"Guid\" = i.\"CaseWorkflowChecklistGuid\" " +
                "where c.\"CaseWorkflowStatusGuid\" = k.\"CaseWorkflowStatusGuid\" and c.\"Active\" = 1 and i.\"Active\" = 1 and coalesce(c.\"Deleted\", 0) = 0 and coalesce(i.\"Deleted\", 0) = 0)";
            const string items = "(select count(*) from \"CaseChecklistItem\" x where x.\"CaseId\" = k.\"Id\" and x.\"CaseWorkflowStatusGuid\" = k.\"CaseWorkflowStatusGuid\"";

            (await ViolationsAsync("CL-ALL-OPEN", $"{items} and x.\"StateId\" in (1, 2)) <> {definitions} or {items} and x.\"StateId\" = 1) = 0 or {items} and x.\"StateId\" = 3) > 0")).Should().Be(0,
                "the reconciler may hide an item whose show condition is false, but never completes one");
            (await ViolationsAsync("CL-ALL-DONE", $"{items} and x.\"StateId\" = 3) <> {definitions}")).Should().Be(0);
            (await ViolationsAsync("CL-PARTIAL", $"{items} and x.\"StateId\" = 1) = 0 or {items} and x.\"StateId\" = 3) = 0")).Should().Be(0);
            (await ViolationsAsync("CL-NONE-DEFINED", $"{items}) <> 0")).Should().Be(0);
            (await ViolationsAsync("CL-SLA-BREACHED",
                "not exists (select 1 from \"CaseChecklistItem\" x where x.\"CaseId\" = k.\"Id\" and x.\"StateId\" = 1 and x.\"SlaBreachedDate\" is not null)")).Should().Be(0);
            (await ViolationsAsync("CL-NULL-DUE",
                "not exists (select 1 from \"CaseChecklistItem\" x where x.\"CaseId\" = k.\"Id\" and x.\"StateId\" = 1 and x.\"DueDate\" is null)")).Should().Be(0);
            (await ViolationsAsync("CL-ORPHAN-ITEM",
                "not exists (select 1 from \"CaseChecklistItem\" x where x.\"CaseId\" = k.\"Id\" and x.\"StateId\" = 1 and not exists (select 1 from \"CaseWorkflowChecklistItem\" d where d.\"Guid\" = x.\"CaseWorkflowChecklistItemGuid\"))")).Should().Be(0);

            const string current = "o.\"CaseWorkflowStatusGuid\" = k.\"CaseWorkflowStatusGuid\"";

            (await ViolationsAsync("CL-OVERRIDE-LIVE", Override($"{current} and o.\"ConsumedDate\" is null and o.\"GrantedUser\" <> 'Administrator'"))).Should().Be(0);
            (await ViolationsAsync("CL-OVERRIDE-OWN", Override($"{current} and o.\"ConsumedDate\" is null and o.\"GrantedUser\" = 'Administrator'"))).Should().Be(0);
            (await ViolationsAsync("CL-OVERRIDE-CONSUMED", Override($"{current} and o.\"ConsumedDate\" is not null"))).Should().Be(0);
            (await ViolationsAsync("CL-OVERRIDE-WRONG-STATUS", Override("o.\"CaseWorkflowStatusGuid\" <> k.\"CaseWorkflowStatusGuid\""))).Should().Be(0);
            (await ViolationsAsync("CL-OVERRIDE-STALE", Override(
                $"{current} and o.\"GrantedDate\" < (select max(t.\"EnteredDate\") from \"CaseStatusTransition\" t where t.\"CaseId\" = k.\"Id\")"))).Should().Be(0);
        }

        [Fact]
        public async Task TheLockCloseHoldAndDiaryScenariosHaveTheShapeTheCatalogueDescribesAsync()
        {
            if (!seeded)
            {
                return;
            }

            (await ViolationsAsync("LOCK-ORPHAN", "k.\"Locked\" is distinct from 1 or coalesce(k.\"LockedUser\", '') <> ''")).Should().Be(0);
            (await ViolationsAsync("LOCK-NULL", "k.\"Locked\" is not null")).Should().Be(0);
            (await ViolationsAsync("LOCK-NOBODY", "k.\"Locked\" is distinct from 0 or coalesce(k.\"LockedUser\", '') = ''")).Should().Be(0);
            (await ViolationsAsync("LOCK-MINE", "k.\"Locked\" is distinct from 1 or k.\"LockedUser\" is distinct from 'Administrator'")).Should().Be(0);
            (await ViolationsAsync("LOCK-ON-CLOSED", "k.\"ClosedStatusId\" is distinct from 3 or k.\"Locked\" is distinct from 1")).Should().Be(0);
            (await ViolationsAsync("LOCK-STUCK-3H", "k.\"LockedDate\" > (now() at time zone 'utc') - interval '2 hours'")).Should().Be(0);

            (await ViolationsAsync("CLOSED-STATUS-NULL", "k.\"ClosedStatusId\" is not null")).Should().Be(0);
            (await ViolationsAsync("SUSPEND-OPEN", "k.\"ClosedStatusId\" is distinct from 1")).Should().Be(0);
            (await ViolationsAsync("SUSPEND-CLOSED", "k.\"ClosedStatusId\" is distinct from 2")).Should().Be(0);
            (await ViolationsAsync("SUSPEND-BYPASS", "k.\"ClosedStatusId\" is distinct from 4")).Should().Be(0);
            (await ViolationsAsync("CLOSED-NO-REASON", "k.\"ClosedStatusId\" is distinct from 3 or k.\"ClosedReasonId\" is not null")).Should().Be(0);

            for (var reason = 1; reason <= 8; reason++)
            {
                (await ViolationsAsync($"CLOSED-R{reason}",
                    $"k.\"ClosedStatusId\" is distinct from 3 or k.\"ClosedReasonId\" is distinct from {reason} or k.\"ClosedDate\" is null")).Should().Be(0);
            }

            (await ViolationsAsync("CLOSED-NOTE-1000", "length(k.\"ClosedReasonNote\") <> 1000")).Should().Be(0);
            (await ViolationsAsync("CLOSED-NULL-DATES", "k.\"ClosedDate\" is not null or k.\"ClosedUser\" is not null")).Should().Be(0);

            (await ViolationsAsync("LH-NORMAL", "k.\"LegalHold\" is distinct from 1 or coalesce(k.\"LegalHoldReason\", '') = ''")).Should().Be(0);
            (await ViolationsAsync("LH-NO-DETAIL", "k.\"LegalHold\" is distinct from 1 or k.\"LegalHoldReason\" is not null or k.\"LegalHoldUser\" is not null")).Should().Be(0);
            (await ViolationsAsync("LH-FILING", "k.\"LegalHoldReason\" is distinct from 'Regulatory report filed'")).Should().Be(0);

            for (var type = 1; type <= 4; type++)
            {
                foreach (var when in new[] { "PAST", "SOON", "FAR" })
                {
                    (await ViolationsAsync($"DI-T{type}-{when}", $"k.\"Diary\" is distinct from 1 or k.\"DiaryTypeId\" is distinct from {type} or k.\"DiaryDate\" is null")).Should().Be(0);
                }
            }

            (await ViolationsAsync("DI-NULL-DATE", "k.\"Diary\" is distinct from 1 or k.\"DiaryDate\" is not null")).Should().Be(0);
            (await ViolationsAsync("DI-DATE-NO-FLAG", "k.\"Diary\" is distinct from 0 or k.\"DiaryDate\" is null")).Should().Be(0);
            (await ViolationsAsync("DI-TYPE-INVALID", "k.\"DiaryTypeId\" is distinct from 9")).Should().Be(0);

            foreach (var (code, state) in new[] { ("PH-HELD", 1), ("PH-DECLINED", 2), ("PH-RELEASED", 3), ("PH-REFUSED", 4) })
            {
                (await ViolationsAsync(code,
                    $"not exists (select 1 from \"Archive\" a where a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\" and (a.\"Json\"->'hold'->>'stateId')::int = {state})")).Should().Be(0);
            }
        }

        [Fact]
        public async Task TheDatabaseRefusesTheStatesTheCatalogueSaysItRefusesAsync()
        {
            if (!seeded)
            {
                return;
            }

            var fresh = await CaseIdAsync("OPEN-FRESH");
            var viewed = await CaseIdAsync("RT-NEW-EVENT");
            var checklist = await CaseIdAsync("CL-ALL-OPEN");

            await ExpectAsync(
                "insert into \"CaseStatusTransition\" (\"CaseId\", \"CaseWorkflowGuid\", \"CaseWorkflowStatusGuid\", \"EnterReasonId\", \"EnteredDate\") " +
                $"select \"CaseId\", \"CaseWorkflowGuid\", \"CaseWorkflowStatusGuid\", \"EnterReasonId\", \"EnteredDate\" from \"CaseStatusTransition\" where \"CaseId\" = {fresh} and \"ExitedDate\" is null",
                PostgresErrorCodes.UniqueViolation);
            await ExpectAsync(
                "insert into \"CaseView\" (\"CaseId\", \"User\", \"LastViewedDate\") " +
                $"select \"CaseId\", \"User\", \"LastViewedDate\" from \"CaseView\" where \"CaseId\" = {viewed}",
                PostgresErrorCodes.UniqueViolation);
            await ExpectAsync(
                "insert into \"CaseChecklistItem\" (\"CaseId\", \"CaseWorkflowChecklistItemGuid\", \"CaseWorkflowChecklistGuid\", \"CaseWorkflowStatusGuid\", \"StateId\", \"FirstShownDate\", \"ShownDate\", \"CreatedDate\") " +
                $"select \"CaseId\", \"CaseWorkflowChecklistItemGuid\", \"CaseWorkflowChecklistGuid\", \"CaseWorkflowStatusGuid\", \"StateId\", \"FirstShownDate\", \"ShownDate\", \"CreatedDate\" from \"CaseChecklistItem\" where \"CaseId\" = {checklist} limit 1",
                PostgresErrorCodes.UniqueViolation);
            await ExpectAsync(
                "insert into \"CaseEvent\" (\"CaseEventTypeId\", \"Before\", \"After\", \"CreatedUser\", \"CreatedDate\", \"CaseKey\", \"CaseKeyValue\", \"CaseId\") " +
                $"select \"CaseEventTypeId\", \"Before\", \"After\", \"CreatedUser\", \"CreatedDate\", \"CaseKey\", \"CaseKeyValue\", 2147483000 from \"CaseEvent\" where \"CaseId\" = {checklist} limit 1",
                PostgresErrorCodes.ForeignKeyViolation);
        }

    }
}
