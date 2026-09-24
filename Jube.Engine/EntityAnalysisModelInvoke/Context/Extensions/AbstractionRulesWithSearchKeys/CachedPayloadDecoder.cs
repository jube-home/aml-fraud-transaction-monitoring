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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys
{
    using System.Collections.Generic;
    using Dictionary;

    public static class CachedPayloadDecoder
    {
        private const int ReferenceDateIndex = -1;

        public static Dictionary<string, DictionaryNoBoxing<string>> Decode(
            Dictionary<string, DictionaryNoBoxing<int>> payloadMap, IReadOnlyDictionary<int, string> parseIndex,
            string referenceDateName)
        {
            var parsedPayloadMap = new Dictionary<string, DictionaryNoBoxing<string>>(payloadMap.Count);

            foreach (var (key, raw) in payloadMap)
            {
                var document = new DictionaryNoBoxing<string>(raw.Count);
                foreach (var (i, value) in raw)
                {
                    switch (i)
                    {
                        case ReferenceDateIndex:
                            document.AddUnchecked(referenceDateName, value);
                            continue;
                        case < 0:
                            continue;
                    }

                    if (parseIndex is not null && parseIndex.TryGetValue(i, out var name))
                    {
                        document.AddUnchecked(name, value);
                    }
                }

                parsedPayloadMap[key] = document;
            }

            return parsedPayloadMap;
        }
    }
}