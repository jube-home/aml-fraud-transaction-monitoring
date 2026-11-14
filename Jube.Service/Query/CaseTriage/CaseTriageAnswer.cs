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

using System.Text.Json;
using Jube.Dto.Options;

namespace Jube.Service.Query.CaseTriage
{
    public sealed record CaseTriageAnswer(int OutcomeId, string Rationale, IReadOnlyList<string> Evidence)
    {
        private const int MaximumRationale = 4000;
        private const int MaximumEvidence = 10;
        private const int MaximumCitation = 300;

        public static CaseTriageAnswer? TryParse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start < 0 || end <= start)
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(text[start..(end + 1)]);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("outcome", out var outcome)
                                                           || outcome.ValueKind != JsonValueKind.String
                                                           || !root.TryGetProperty("reason", out var reason)
                                                           || reason.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                int? outcomeId = outcome.GetString()?.Trim().ToLowerInvariant() switch
                {
                    "confirmed" => (int)CaseStatusOutcome.Confirmed,
                    "notconfirmed" => (int)CaseStatusOutcome.NotConfirmed,
                    "inconclusive" => (int)CaseStatusOutcome.NotAnOutcome,
                    _ => null
                };

                var rationale = reason.GetString()?.Trim();

                if (outcomeId is null || string.IsNullOrEmpty(rationale))
                {
                    return null;
                }

                var evidence = new List<string>();

                if (root.TryGetProperty("evidence", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray().Take(MaximumEvidence))
                    {
                        var citation = item.ValueKind == JsonValueKind.String ? item.GetString()?.Trim() : null;

                        if (!string.IsNullOrEmpty(citation))
                        {
                            evidence.Add(citation.Length > MaximumCitation ? citation[..MaximumCitation] : citation);
                        }
                    }
                }

                return new CaseTriageAnswer(outcomeId.Value,
                    rationale.Length > MaximumRationale ? rationale[..MaximumRationale] : rationale, evidence);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
