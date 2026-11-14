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

namespace Jube.Migrations.Branches.ForceOverrideApproval
{
    using System;
    using FluentMigrator;

    [Migration(20263108610001)]
    public class BackfillEntityApprovalForForceOverrides : Migration
    {
        public const int MinimumSystemApprovers = 5;
        private const string ApprovedBy = "Administrator";

        public override void Up()
        {
            var configured = int.TryParse(Environment.GetEnvironmentVariable("ApprovalsRequired"), out var parsed)
                ? parsed
                : 1;

            Execute.Sql(BuildBackfillSql(Math.Max(MinimumSystemApprovers, configured)));
        }

        public override void Down()
        {
        }

        public static string BuildBackfillSql(int systemApprovers)
        {
            return BuildKindSql(22, "EntityAnalysisModelOverride", systemApprovers) +
                   BuildKindSql(23, "EntityAnalysisModelActivationRuleOverride", systemApprovers);
        }

        private static string BuildKindSql(int kind, string table, int systemApprovers)
        {
            return $"""
                    insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId",
                        "EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                    select m."TenantRegistryId", {kind}, r."Id", coalesce(r."Version", 1), 1,
                        now() at time zone 'utc',
                        case when s.n = 1 then '{ApprovedBy}' else '{ApprovedBy}' || s.n::text end,
                        gen_random_uuid()
                    from "{table}" r
                    join "EntityAnalysisModel" m on m."Guid" = r."EntityAnalysisModelGuid"
                    cross join generate_series(1, {systemApprovers}) as s(n)
                    where r."OverrideKind" = 1
                      and coalesce(r."Deleted", 0) = 0
                      and m."TenantRegistryId" is not null
                      and not exists (
                          select 1 from "EntityApproval" a
                          where a."EntityApprovalKindId" = {kind}
                            and a."EntityId" = r."Id"
                            and a."EntityVersion" = coalesce(r."Version", 1)
                            and a."CreatedUser" = case when s.n = 1 then '{ApprovedBy}'
                                                       else '{ApprovedBy}' || s.n::text end);

                    """;
        }
    }
}
