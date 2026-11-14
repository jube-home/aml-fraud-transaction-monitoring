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
using System.Collections.Generic;
using System.Linq;
using Svirfd;
using Svirfd.Narrative;

namespace Jube.Svirfd
{
    public static class SvirfdNarrative
    {
        public static readonly IReadOnlyList<string> GroupIds =
        [
            "identity", "network", "routing", "registry", "location", "privacy", "cloud", "observed", "density",
            "caveats"
        ];

        public static SvirfdNarrativeSummary? Render(IpResponse? record)
        {
            return record == null ? null : From(record.Narrate());
        }

        public static SvirfdNarrativeSummary From(NarrativeDto narrative)
        {
            var groups = narrative.Groups
                .Select(g => new SvirfdNarrativeGroup(g.Id.ToLowerInvariant(),
                    string.IsNullOrWhiteSpace(g.Title) ? Title(g.Id) : g.Title, g.Paragraphs))
                .ToList();

            return new SvirfdNarrativeSummary(narrative.Headline, groups, narrative.Version);
        }

        public static string Title(string id)
        {
            return string.IsNullOrEmpty(id) ? id : char.ToUpperInvariant(id[0]) + id[1..];
        }
    }
}
