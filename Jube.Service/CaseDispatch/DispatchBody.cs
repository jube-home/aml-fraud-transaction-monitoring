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


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.CaseDispatch
{
    public static class DispatchBody
    {
        public static string Build(string source, string name, int caseId, string? caseKey, string? caseKeyValue,
            string user, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
            IReadOnlyDictionary<string, string?>? values)
        {
            var body = new JObject
            {
                ["source"] = source,
                ["name"] = name,
                ["caseId"] = caseId,
                ["caseKey"] = caseKey,
                ["caseKeyValue"] = caseKeyValue,
                ["user"] = user,
                ["rows"] = JArray.FromObject(rows)
            };

            if (values is { Count: > 0 })
            {
                body["values"] = JObject.FromObject(values);
            }

            return body.ToString(Formatting.None);
        }
    }
}
