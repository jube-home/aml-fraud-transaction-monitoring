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
using System.Text.Json;
using Jube.Data.Repository;

namespace Jube.Service.Query.CaseTriage
{
    public static class CaseTriageReplayBundle
    {
        public const int MaximumArchiveCharacters = 3000;
        public const int MaximumNoteCharacters = 1000;

        public static string Build(ReplayCaseCandidate candidate, DateTime asOf,
            IReadOnlyList<ReplayArchiveRow> archive, IReadOnlyList<ReplayNote> notes,
            IReadOnlyList<ReplayPriorOutcome> prior)
        {
            return JsonSerializer.Serialize(new
            {
                asOf = asOf.ToString("O", CultureInfo.InvariantCulture),
                caseKey = candidate.CaseKey,
                caseKeyValue = candidate.CaseKeyValue,
                caseCreated = candidate.CreatedDate.ToString("O", CultureInfo.InvariantCulture),
                transactions = archive.Select(a => new
                {
                    referenceDate = a.ReferenceDate.ToString("O", CultureInfo.InvariantCulture),
                    payload = Clip(a.Json, MaximumArchiveCharacters)
                }),
                notes = notes.Select(n => new
                {
                    createdDate = n.CreatedDate.ToString("O", CultureInfo.InvariantCulture),
                    text = Clip(n.Note, MaximumNoteCharacters)
                }),
                earlierClosedCases = new
                {
                    confirmed = prior.Where(p => p.OutcomeId == 1).Sum(p => p.Cases),
                    notConfirmed = prior.Where(p => p.OutcomeId == 2).Sum(p => p.Cases)
                }
            });
        }

        private static string Clip(string? text, int maximum)
        {
            var value = text ?? string.Empty;

            return value.Length > maximum ? value[..maximum] : value;
        }
    }
}
