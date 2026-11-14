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

using Jube.Dto.Query.Svirfd;
using Jube.Svirfd;
using Jube.Svirfd.Interfaces;
using Svirfd;

namespace Jube.Service.Query.Svirfd
{
    public static class SvirfdLookupMapper
    {
        public static SvirfdLookupDto ToDto(IpResponse record, ISvirfdLookup lookup)
        {
            var narrative = SvirfdNarrative.Render(record);

            return new SvirfdLookupDto
            {
                Ip = record.Ip,
                RecordJson = SvirfdJson.Serialize(record),
                Headline = narrative?.Headline,
                Narrative =
                [
                    .. (narrative?.Groups ?? []).Select(g => new SvirfdNarrativeGroupDto
                    {
                        Id = g.Id, Title = g.Title, Paragraphs = [.. g.Paragraphs]
                    })
                ],
                NarrativeVersion = narrative?.Version ?? 0,
                Attribution = lookup.Attribution,
                LicenceUrl = lookup.LicenceUrl,
                Mode = lookup.Mode.ToString(),
                Seq = lookup.Seq,
                RttChartSvg = SvirfdRttChart.Render(record)
            };
        }

        public static SvirfdStatusDto ToStatus(ISvirfdLookup lookup, bool tenantRemoteLookupsEnabled = true)
        {
            return new SvirfdStatusDto
            {
                Enabled = lookup.Enabled,
                Ready = lookup.Ready,
                Mode = lookup.Mode.ToString(),
                Seq = lookup.Seq,
                RemoteEngineLookup = lookup.RemoteEngineLookup,
                TenantRemoteLookupsEnabled = tenantRemoteLookupsEnabled,
                Attribution = lookup.Attribution,
                LicenceUrl = lookup.LicenceUrl,
                State = lookup.State.ToString(),
                LastError = lookup.LastError,
                NextRetryAt = lookup.NextRetryAt,
                StalenessSeconds = lookup.Staleness.HasValue ? (long)lookup.Staleness.Value.TotalSeconds : null
            };
        }
    }
}
