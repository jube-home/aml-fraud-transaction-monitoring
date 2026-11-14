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
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace Jube.Test.Volume
{
    internal sealed class CasePagesQueryPlanWorld(DatabaseFixture fx)
    {
        public const int Archives = 100000;
        public const int Cases = 30000;

        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly string tag = Guid.NewGuid().ToString("N");

        public string User => fx.Seed.UserWithPermission;
        public CaseRiskSeeder.Seeded Workflow { get; private set; } = null!;
        public int ModelId { get; private set; }
        public int TenantRegistryId { get; private set; }
        public int OpenCases { get; private set; }
        public int HeldCases { get; private set; }
        public int QaSamples { get; private set; }
        public string HeavyAccount { get; private set; } = null!;
        public int HeavyAccountRows { get; private set; }
        public string LightAccount { get; private set; } = null!;
        public int LightAccountRows { get; private set; }
        public string CaseAccount { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await using var db = fx.GetDbContext();
            db.CommandTimeout = 600;

            Workflow = await seeder.SeedWorkflowAsync(db);
            await seeder.GrantAsync(db, Workflow, User);
            ModelId = await db.CaseWorkflow.Where(w => w.Guid == Workflow.WorkflowGuid)
                .Select(w => w.EntityAnalysisModelId!.Value).FirstAsync();
            TenantRegistryId = await db.UserInTenant.Where(u => u.User == User).Select(u => u.TenantRegistryId)
                .FirstAsync();

            await ArchiveKeySeed.FieldAsync(db, ModelId, "AccountId");
            await ArchiveKeySeed.FieldAsync(db, ModelId, "Device");
            await ArchiveKeySeed.FieldAsync(db, ModelId, "Amount", 3);

            var parameters = new[]
            {
                new DataParameter("model", ModelId, DataType.Int32),
                new DataParameter("tag", tag, DataType.Text),
                new DataParameter("archives", Archives, DataType.Int32),
                new DataParameter("cases", Cases, DataType.Int32),
                new DataParameter("workflow", Workflow.WorkflowGuid, DataType.Guid),
                new DataParameter("st1", Workflow.Confirmed, DataType.Guid),
                new DataParameter("st2", Workflow.NotConfirmed, DataType.Guid),
                new DataParameter("st3", Workflow.Neutral, DataType.Guid),
                new DataParameter("user", User, DataType.Text)
            };

            foreach (var statement in SeedStatements)
            {
                await db.ExecuteAsync(statement, parameters);
            }

            foreach (var statement in SimilaritySeedStatements)
            {
                await db.ExecuteAsync(statement, new DataParameter("workflow", Workflow.WorkflowGuid, DataType.Guid));
            }

            foreach (var table in new[]
                     {
                         "Archive", "ArchiveKey", "Case", "CaseEvent", "CaseRiskAssessment", "CaseSimilarityComponent",
                         "CaseSimilarityComponentFrequency"
                     })
            {
                await db.ExecuteAsync("analyze \"" + table + "\"");
            }

            OpenCases = await db.Case.CountAsync(c => c.CaseWorkflowGuid == Workflow.WorkflowGuid
                                                      && c.ClosedStatusId == 0);
            HeldCases = await db.Case.CountAsync(c => c.CaseWorkflowGuid == Workflow.WorkflowGuid
                                                      && c.ClosedStatusId == 0 && c.Locked == 1
                                                      && c.LockedUser == User);

            QaSamples = await db.Case.CountAsync(c => c.CaseWorkflowGuid == Workflow.WorkflowGuid
                                                      && c.ClosedStatusId == 0 && c.QaSampleStatusId == 2);

            var ranked = await db.QueryToListAsync<AccountCount>(
                "select k.\"KeyValueString\" as \"Account\", count(*)::int as \"Rows\" from \"ArchiveKey\" k " +
                "join \"Archive\" a on a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\" " +
                "where a.\"EntityAnalysisModelId\" = @model and k.\"Key\" = 'AccountId' " +
                "group by 1 order by 2 desc, 1", new DataParameter("model", ModelId, DataType.Int32));

            HeavyAccount = ranked[0].Account;
            HeavyAccountRows = ranked[0].Rows;
            LightAccount = ranked[^1].Account;
            LightAccountRows = ranked[^1].Rows;
            CaseAccount = await db.Case.Where(c => c.CaseWorkflowGuid == Workflow.WorkflowGuid)
                .OrderBy(c => c.Id).Select(c => c.CaseKeyValue).FirstAsync();
        }

        public async Task DisposeAsync()
        {
            await using var db = fx.GetDbContext();
            db.CommandTimeout = 600;
            var parameters = new[]
            {
                new DataParameter("workflow", Workflow.WorkflowGuid, DataType.Guid),
                new DataParameter("model", ModelId, DataType.Int32)
            };

            foreach (var statement in new[]
                     {
                         "delete from \"CaseSimilarityComponent\" where \"CaseWorkflowGuid\" = @workflow",
                         "delete from \"CaseSimilarityComponentFrequency\" where \"CaseWorkflowGuid\" = @workflow",
                         "delete from \"CaseRiskAssessment\" where \"CaseId\" in (select \"Id\" from \"Case\" where \"CaseWorkflowGuid\" = @workflow)",
                         "delete from \"CaseEvent\" where \"CaseId\" in (select \"Id\" from \"Case\" where \"CaseWorkflowGuid\" = @workflow)",
                         "delete from \"Case\" where \"CaseWorkflowGuid\" = @workflow",
                         "delete from \"ArchiveKey\" k using \"Archive\" a where a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\" and a.\"EntityAnalysisModelId\" = @model",
                         "delete from \"Archive\" where \"EntityAnalysisModelId\" = @model"
                     })
            {
                await db.ExecuteAsync(statement, parameters);
            }

            await ArchiveKeySeed.CleanupAsync(db, [ModelId]);
            await seeder.CleanAsync(db);
        }


        private const string AccountExpression =
            "'ACC-' || (1 + floor(5000 * power(((n * 7919) % 100000) / 100000.0, 2)))::int";

        private static readonly string[] SimilaritySeedStatements =
        [
            "insert into \"CaseSimilarityComponent\" (\"CaseId\", \"CaseWorkflowGuid\", \"KindId\", \"Component\") " +
            "select c.\"Id\", @workflow, v.kind, v.component from \"Case\" c cross join lateral (values " +
            "(1::smallint, 'Common'), (1::smallint, 'Rule' || (c.\"Id\" % 50)), " +
            "(4::smallint, 'Device=' || (c.\"Id\" % 3000)), (6::smallint, 'Amount:sum=' || (c.\"Id\" % 7))) " +
            "as v(kind, component) where c.\"CaseWorkflowGuid\" = @workflow",

            "insert into \"CaseSimilarityComponentFrequency\" (\"CaseWorkflowGuid\", \"KindId\", \"Component\", \"CaseCount\") " +
            "select @workflow, s.\"KindId\", s.\"Component\", count(*)::int from \"CaseSimilarityComponent\" s " +
            "where s.\"CaseWorkflowGuid\" = @workflow group by 1, 2, 3",

            "insert into \"CaseSimilarityComponentFrequency\" (\"CaseWorkflowGuid\", \"KindId\", \"Component\", \"CaseCount\") " +
            "select @workflow, 0, '', count(distinct s.\"CaseId\")::int from \"CaseSimilarityComponent\" s " +
            "where s.\"CaseWorkflowGuid\" = @workflow"
        ];

        private static readonly string[] SeedStatements =
        [
            "insert into \"Archive\" (\"Json\", \"EntityAnalysisModelInstanceEntryGuid\", \"EntryKeyValue\", \"ResponseElevation\", " +
            "\"EntityAnalysisModelId\", \"ActivationRuleCount\", \"CreatedDate\", \"ReferenceDate\", \"Version\") " +
            "select jsonb_build_object('payload', jsonb_build_object('AccountId', s.acc, 'Device', 'DEV-' || (s.n % 2000), " +
            "'Amount', (s.n % 5000)::float8, 'Narrative', repeat('lorem ipsum ', 20)), 'tag', '[]'::jsonb), " +
            "md5(@tag || s.n::text)::uuid, s.acc, (s.n % 10)::float8, @model, case when s.n % 10 = 0 then 1 else 0 end, s.ts, s.ts, 1 " +
            "from (select n, " + AccountExpression + " as acc, (now() at time zone 'utc') - ((n % 365) * interval '1 day') - " +
            "((n % 1440) * interval '1 minute') as ts from generate_series(1, @archives) n) s",

            "insert into \"ArchiveKey\" (\"ProcessingTypeId\", \"Key\", \"KeyValueString\", \"EntityAnalysisModelInstanceEntryGuid\", \"Version\") " +
            "select 1, 'AccountId', a.\"Json\" -> 'payload' ->> 'AccountId', a.\"EntityAnalysisModelInstanceEntryGuid\", 1 " +
            "from \"Archive\" a where a.\"EntityAnalysisModelId\" = @model",

            "insert into \"ArchiveKey\" (\"ProcessingTypeId\", \"Key\", \"KeyValueString\", \"EntityAnalysisModelInstanceEntryGuid\", \"Version\") " +
            "select 1, 'Device', a.\"Json\" -> 'payload' ->> 'Device', a.\"EntityAnalysisModelInstanceEntryGuid\", 1 " +
            "from \"Archive\" a where a.\"EntityAnalysisModelId\" = @model",

            "insert into \"ArchiveKey\" (\"ProcessingTypeId\", \"Key\", \"KeyValueFloat\", \"EntityAnalysisModelInstanceEntryGuid\", \"Version\") " +
            "select 1, 'Amount', (a.\"Json\" -> 'payload' ->> 'Amount')::float8, a.\"EntityAnalysisModelInstanceEntryGuid\", 1 " +
            "from \"Archive\" a where a.\"EntityAnalysisModelId\" = @model",

            "insert into \"Case\" (\"EntityAnalysisModelInstanceEntryGuid\", \"CreatedDate\", \"Locked\", \"LockedUser\", \"LockedDate\", " +
            "\"ClosedStatusId\", \"ClosedDate\", \"ClosedUser\", \"CaseKey\", \"Diary\", \"Json\", \"CaseKeyValue\", \"CaseWorkflowGuid\", " +
            "\"CaseWorkflowStatusGuid\", \"DiaryTypeId\", \"QaSampleStatusId\", \"RiskLaneId\", \"RiskPriority\", " +
            "\"RiskLikelihoodProbability\", \"RiskNextDeadlineDate\", \"RiskValue\", \"RiskSequence\") " +
            "select c.\"EntityAnalysisModelInstanceEntryGuid\", c.\"CreatedDate\", " +
            "case when c.n % 100 = 0 then 1 else 0 end, case when c.n % 100 = 0 then @user end, " +
            "case when c.n % 100 = 0 then c.\"CreatedDate\" + interval '1 hour' end, " +
            "case when c.n % 10 = 0 then 0 else 3 end, case when c.n % 10 = 0 then null else c.\"CreatedDate\" + interval '3 days' end, " +
            "case when c.n % 10 = 0 then null else @user end, 'AccountId', 0, " +
            "jsonb_build_object('payload', jsonb_build_object('AccountId', c.acc)), c.acc, @workflow, " +
            "(array[@st1, @st2, @st3])[1 + c.n % 3], 0, case when c.n % 40 = 0 then 2 else 0 end, " +
            "1 + c.n % 4, (c.n * 37) % 1000, ((c.n * 13) % 100) / 100.0, c.\"CreatedDate\" + ((1 + c.n % 4) * interval '1 day'), " +
            "((c.n * 17) % 20000)::float8, 1 " +
            "from (select (a.rn / 3)::int as n, a.\"EntityAnalysisModelInstanceEntryGuid\", a.\"CreatedDate\", " +
            "a.\"Json\" -> 'payload' ->> 'AccountId' as acc from (select x.*, row_number() over (order by x.\"Id\") as rn " +
            "from \"Archive\" x where x.\"EntityAnalysisModelId\" = @model) a where a.rn % 3 = 0 and a.rn / 3 <= @cases) c",

            "insert into \"CaseEvent\" (\"CaseEventTypeId\", \"Before\", \"After\", \"CreatedUser\", \"CreatedDate\", \"CaseKey\", \"CaseKeyValue\", \"CaseId\") " +
            "select 1 + (e % 6), 'before', 'after', @user, c.\"CreatedDate\" + (e * interval '20 minutes'), c.\"CaseKey\", c.\"CaseKeyValue\", c.\"Id\" " +
            "from \"Case\" c cross join generate_series(0, 2) e where c.\"CaseWorkflowGuid\" = @workflow",

            "insert into \"CaseRiskAssessment\" (\"CaseId\", \"Sequence\", \"CreatedDate\", \"TriggerId\", \"MethodId\", \"MethodVersion\", \"BaseRate\", " +
            "\"LikelihoodProbability\", \"LikelihoodBandId\", \"EvidenceCount\", \"ContributionsJson\", \"ImpactId\", \"ImpactComponentsJson\", " +
            "\"LaneId\", \"LaneReason\", \"NextDeadlineDate\", \"DeadlineBreached\", \"ExpectedHarm\", \"Priority\") " +
            "select c.\"Id\", 1, c.\"CreatedDate\", 1, 1, 1, 0.1, c.\"RiskLikelihoodProbability\", 1 + (c.\"Id\" % 4), 3, '[]'::jsonb, " +
            "1 + (c.\"Id\" % 3), jsonb_build_object('value', c.\"RiskValue\"), c.\"RiskLaneId\", 'seed', c.\"RiskNextDeadlineDate\", 0, " +
            "c.\"RiskValue\" * c.\"RiskLikelihoodProbability\", c.\"RiskPriority\" " +
            "from \"Case\" c where c.\"CaseWorkflowGuid\" = @workflow and c.\"ClosedStatusId\" = 0"
        ];

        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private sealed class AccountCount
        {
            public string Account { get; set; } = null!;
            public int Rows { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}
