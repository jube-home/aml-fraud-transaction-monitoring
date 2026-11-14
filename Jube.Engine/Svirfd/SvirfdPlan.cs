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
using Jube.Data.Poco;

namespace Jube.Engine.Svirfd
{
    public sealed class SvirfdPlan
    {
        public static readonly SvirfdPlan Empty = new([], true);

        private SvirfdPlan(IReadOnlyList<SvirfdEntity> entities, bool remoteLookupsEnabled)
        {
            Entities = entities;
            RemoteLookupsEnabled = remoteLookupsEnabled;
            Names = [.. entities.Select(e => e.Name)];
        }

        public IReadOnlyList<SvirfdEntity> Entities { get; }
        public IReadOnlyList<string> Names { get; }
        public bool Any => Entities.Count > 0;
        public bool RemoteLookupsEnabled { get; }

        public static SvirfdPlan Build(IEnumerable<EntityAnalysisModelSvirfd> rows, bool remoteLookupsEnabled = true)
        {
            var entities = rows
                .Where(r => r is { Active: 1 } && (r.Deleted ?? 0) == 0 && !string.IsNullOrWhiteSpace(r.Name) &&
                            !string.IsNullOrWhiteSpace(r.RequestXPathName))
                .OrderBy(r => r.Id)
                .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Select(r => new SvirfdEntity(r.Id, r.Name.Trim(), r.RequestXPathName.Trim().Replace(" ", "_"),
                    r.ResponsePayload == 1, (r.ReportTable ?? 1) == 1, (r.Narrative ?? 1) == 1))
                .ToList();

            return new SvirfdPlan(entities, remoteLookupsEnabled);
        }
    }
}
