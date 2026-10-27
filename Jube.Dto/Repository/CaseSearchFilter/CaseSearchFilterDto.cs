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

using System.ComponentModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseSearchFilter
{
    [Description("A search saved by one user for the Cases page: the query-builder selection and filter composed " +
                 "against one Case Workflow. Private to its owner unless shared with a role.")]
    public class CaseSearchFilterDto
    {
        [Description("Guid of the saved search. Leave empty on create; the server assigns it.")]
        public Guid Guid { get; set; }

        [Description("Guid of the Case Workflow the search is composed against. Required on create and not " +
                     "changeable afterwards.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("The owner's name for the search, unique for that owner within the Case Workflow ignoring " +
                     "case. Required.")]
        public string? Name { get; set; }

        [Description("The query-builder condition tree as JSON, exactly as the Case Filter Builder emits it. " +
                     "Required. The SQL is compiled from it on every run and is never accepted from the caller.")]
        public string? FilterJson { get; set; }

        [Description("The query-builder column selection and ordering as JSON, exactly as the Case Filter " +
                     "Builder emits it. Required.")]
        public string? SelectJson { get; set; }

        [Description("Guid of a Role Registry entry. When set, every user holding that role can also run the " +
                     "search; only the owner can change or delete it. Null keeps the search private.")]
        public Guid? SharedRoleRegistryGuid { get; set; }

        [Description("True when the caller owns the search. Server generated.")]
        public bool Mine { get; set; }

        [Description("The user that owns the search. Server generated.")]
        public string? CreatedUser { get; set; }

        [Description("When the search was created (UTC). Server generated.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("The user that last changed the search. Server generated.")]
        public string? UpdatedUser { get; set; }

        [Description("When the search was last changed (UTC). Server generated.")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Optimistic-concurrency version. Send back the value read; a changed version is refused. " +
                     "Zero or absent skips the check.")]
        public int Version { get; set; }
    }
}
