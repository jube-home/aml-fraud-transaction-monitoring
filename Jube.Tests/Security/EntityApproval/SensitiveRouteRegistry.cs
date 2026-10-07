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

namespace Jube.Test.Security.EntityApproval;

public static class SensitiveRouteRegistry
{
    public static readonly string[] Prefixes =
    [
        "api/EntityApproval/",
        "api/TenantRegistryVersionHistory/",
        "api/EntityAnalysisModelRequestXpathVersionHistory/"
    ];

    private static readonly string[] versionHistoryTemplates =
    [
        "ById/{id:int}",
        "ByParent/{parentId:int}",
        "ByDateRange/{parentId:int}",
        "ById/{id:int}/ByDateRange",
        "Latest/{parentId:int}",
        "Compare"
    ];

    public static readonly string[] Covered =
    [
        "api/EntityApproval/Approve",
        "api/EntityApproval/Reject",
        "api/EntityApproval/ApproveValues",
        "api/EntityApproval/History",
        "api/EntityApproval/Pending",
        "api/EntityApproval/Changes",
        "api/EntityApproval/PendingByKind",
        "api/EntityApproval/Status",
        "api/EntityApproval/ValueStatus",
        .. versionHistoryTemplates.Select(t => "api/TenantRegistryVersionHistory/" + t),
        .. versionHistoryTemplates.Select(t => "api/EntityAnalysisModelRequestXpathVersionHistory/" + t)
    ];
}