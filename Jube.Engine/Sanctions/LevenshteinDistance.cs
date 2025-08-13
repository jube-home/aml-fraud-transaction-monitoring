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
using Fastenshtein;

namespace Jube.Engine.Sanctions;

public static class LevenshteinDistance
{
    public static List<SanctionEntryReturn> CheckMultipartString(string multiPartString, int distance,
        Dictionary<int, SanctionEntryDto> sanctionsEntries)
    {
        var sanctionsEntriesReturn = new Dictionary<int, SanctionEntryReturn>();
        var multiPartStrings = multiPartString.Split([" "], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < multiPartStrings.Length; i++) multiPartStrings[i] = Clean(multiPartStrings[i]);

        foreach (var (_, value) in sanctionsEntries.ToList())
            for (var i = 0; i <= distance; i++)
            {
                var sanctionEntryMatches = (from multiPartStringElement in multiPartStrings.Distinct().ToArray()
                    where value.SanctionElementValue.Distinct()
                        .ToArray()
                        .Select(sanctionPayloadString =>
                            Levenshtein.Distance(multiPartStringElement, sanctionPayloadString))
                        .Any(output => output <= i)
                    select new SanctionEntryMatch { SanctionEntryDto = value }).ToList();

                if (!multiPartStrings.Any()) continue;

                if (sanctionEntryMatches.Count != multiPartStrings.Distinct().ToArray().Length) continue;

                var match = new SanctionEntryReturn
                {
                    SanctionEntryDto = value,
                    LevenshteinDistance = i
                };
                sanctionsEntriesReturn.TryAdd(match.SanctionEntryDto.SanctionEntryId, match);
            }

        return sanctionsEntriesReturn.Values.ToList();
    }

    public static string Clean(string raw)
    {
        var value = raw;
        value = value.Replace(",", "");
        value = value.Replace(" ", "");
        value = value.ToLower();
        return value;
    }
}