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

namespace Jube.Engine.CaseRisk
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query.GetCaseNetworkBucketOutcomesQuery;
    using Data.Repository;
    using Jube.CaseRisk;

    public static class CaseNetworkEvidence
    {
        public const string Family = "Network";
        public const int MaximumDistance = 3;

        public static async Task<List<EvidenceItem>> BuildAsync(DbContext dbContext, int tenantRegistryId,
            Guid caseWorkflowGuid, int caseId, string caseKey, string caseKeyValue, DateTime now, double halfLifeDays,
            CancellationToken token = default)
        {
            var items = new List<EvidenceItem>();
            var node = await new GraphNodeRepository(dbContext).GetAsync(tenantRegistryId, caseKey, caseKeyValue, token);

            if (node == null)
            {
                return items;
            }

            var age = node.ComputedDate.HasValue ? Math.Max(0, (now - node.ComputedDate.Value).TotalDays) : 0;

            if (node.DistanceToConfirmed is >= 1 and <= MaximumDistance)
            {
                var bucket = await new GetCaseNetworkBucketOutcomesQuery(dbContext).ExecuteAsync(tenantRegistryId,
                    caseWorkflowGuid, caseId, node.DistanceToConfirmed.Value, token);

                if (bucket.Closed > 0)
                {
                    items.Add(new EvidenceItem(Family,
                        string.Format(CultureInfo.InvariantCulture, "{0} hop(s) from a confirmed true positive",
                            node.DistanceToConfirmed.Value),
                        "Closed cases in the workflow whose key sat at the same graph distance from a confirmed case",
                        Family, bucket.Confirmed, bucket.Closed, age, halfLifeDays));
                }
            }

            if (node.CommunityId.HasValue)
            {
                var community = await new GraphCommunityRepository(dbContext).GetByIdAsync(tenantRegistryId,
                    node.CommunityId.Value, token);

                if (community is { ClosedCount: > 0 })
                {
                    items.Add(new EvidenceItem(Family,
                        string.Format(CultureInfo.InvariantCulture, "Community {0}: {1} confirmed of {2} closed cases",
                            community.Id, community.ConfirmedCount, community.ClosedCount),
                        "Closed cases among the members of the node's graph community", Family, community.ConfirmedCount,
                        community.ClosedCount, age, halfLifeDays));
                }
            }

            return items;
        }
    }
}
