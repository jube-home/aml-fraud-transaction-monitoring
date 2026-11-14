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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jube.Data.Query.GetCaseNarrativeCandidatesQuery;
using Jube.Data.Query.GetCaseNarrativeFactsQuery;

namespace Jube.Service.Query.CaseNarrative
{
    public static class CaseNarrativeFacts
    {
        public static string Hash(GetCaseNarrativeCandidatesQueryDto candidate, GetCaseNarrativeFactsQueryDto facts)
        {
            var text = string.Join('|',
                candidate.CaseWorkflowStatusGuid.ToString("N"),
                facts.ArchiveCount.ToString(CultureInfo.InvariantCulture),
                facts.ArchiveMaxId.ToString(CultureInfo.InvariantCulture),
                facts.ActivationCount.ToString(CultureInfo.InvariantCulture),
                facts.NoteCount.ToString(CultureInfo.InvariantCulture),
                facts.NoteMaxId.ToString(CultureInfo.InvariantCulture),
                facts.FindingCount.ToString(CultureInfo.InvariantCulture));

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        }
    }
}
