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
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using log4net;
using Svirfd;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys
{
    public sealed class AbstractionMatchResolver(
        List<DictionaryNoBoxing<string>> documents,
        DictionaryNoBoxing<string> current,
        Dictionary<string, List<string>> lists,
        PooledDictionary<string, double> kvp,
        PooledDictionary<string, IpResponse> svirfd,
        ILog log)
    {
        private readonly Dictionary<string, List<DictionaryNoBoxing<string>>> logicHashMatches = [];

        private Dictionary<(string LogicHash, long From, long To, bool Descending),
            List<DictionaryNoBoxing<string>>> cursorMatches;

        public List<DictionaryNoBoxing<string>> Resolve(EntityAnalysisModelAbstractionRule abstractionRule,
            string dateName, DateTime from, DateTime to, out int matchedBeforeWindow, out bool reused)
        {
            var descending = AbstractionCursorRunner.IsDescending(abstractionRule);

            if (abstractionRule.UsesCursor)
            {
                var key = (abstractionRule.LogicHash ?? string.Empty, from.Ticks, to.Ticks, descending);

                cursorMatches ??= [];

                reused = cursorMatches.TryGetValue(key, out var windowedMatches);

                if (!reused)
                {
                    windowedMatches = AbstractionCursorRunner.Scan(abstractionRule.AbstractionRuleCompileDelegate,
                        InWindow(documents, dateName, from, to), descending, current, lists, kvp, svirfd, log);

                    cursorMatches[key] = windowedMatches;
                }

                matchedBeforeWindow = windowedMatches.Count;

                return windowedMatches;
            }

            var logicHash = abstractionRule.LogicHash ?? string.Empty;

            reused = logicHashMatches.TryGetValue(logicHash, out var matches);

            if (!reused)
            {
                matches = AbstractionCursorRunner.Scan(abstractionRule.AbstractionRuleCompileDelegate, documents,
                    false, current, lists, kvp, svirfd, log);

                logicHashMatches[logicHash] = matches;
            }

            matchedBeforeWindow = matches.Count;

            var finalMatches = InWindow(matches, dateName, from, to);

            if (descending)
            {
                finalMatches.Reverse();
            }

            return finalMatches;
        }

        private static List<DictionaryNoBoxing<string>> InWindow(List<DictionaryNoBoxing<string>> source,
            string dateName, DateTime from, DateTime to)
        {
            return source.FindAll(x =>
            {
                var date = x[dateName].AsDateTime();

                return date >= from && date <= to;
            });
        }
    }
}
