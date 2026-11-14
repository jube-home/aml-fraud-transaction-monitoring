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
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using log4net;
using Svirfd;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys
{
    public static class AbstractionCursorRunner
    {
        private const int DescendingIterationOrderType = 2;

        public static bool IsDescending(EntityAnalysisModelAbstractionRule abstractionRule)
        {
            return abstractionRule.IterationOrderType == DescendingIterationOrderType;
        }

        public static List<DictionaryNoBoxing<string>> Scan(EntityAnalysisModelAbstractionRule.Match match,
            List<DictionaryNoBoxing<string>> documents, bool descending, DictionaryNoBoxing<string> current,
            Dictionary<string, List<string>> lists, PooledDictionary<string, double> kvp,
            PooledDictionary<string, IpResponse> svirfd, ILog log)
        {
            var matches = new AbstractionMatchList();
            var cursor = new AbstractionCursor();
            var count = documents.Count;

            cursor.Begin(count);

            for (var index = 0; index < count; index++)
            {
                var document = documents[descending ? count - 1 - index : index];

                cursor.MoveTo(index, ReferenceEquals(document, current));

                if (match(document, lists, kvp, svirfd, cursor, log))
                {
                    matches.Add(document);
                    cursor.Accept();
                }

                if (cursor.Stopped)
                {
                    break;
                }
            }

            matches.CursorResult = cursor.Result;

            return matches;
        }

        public static bool Single(EntityAnalysisModelAbstractionRule.Match match, AbstractionCursor cursor,
            DictionaryNoBoxing<string> document, Dictionary<string, List<string>> lists,
            PooledDictionary<string, double> kvp, PooledDictionary<string, IpResponse> svirfd, ILog log)
        {
            cursor.Begin(1);
            cursor.MoveTo(0, true);

            return match(document, lists, kvp, svirfd, cursor, log);
        }
    }
}
