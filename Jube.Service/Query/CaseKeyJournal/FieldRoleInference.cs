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


using Jube.Dto.Options;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class FieldRoleInference
    {
        public const int MaxCategoryValues = 12;
        public const int FewRows = 20;

        public static (int RoleId, bool Inferred) Infer(int configured, string type, bool drill,
            IReadOnlyCollection<object?> values)
        {
            if (configured != (int)JournalFieldRole.Inferred)
            {
                return (configured, false);
            }

            if (drill)
            {
                return ((int)JournalFieldRole.Identifier, true);
            }

            switch (type)
            {
                case "date":
                    return ((int)JournalFieldRole.Time, true);
                case "number":
                    return ((int)JournalFieldRole.Measure, true);
                case "boolean":
                case "array":
                    return ((int)JournalFieldRole.Category, true);
            }

            var present = values.Where(value => value is not null && value as string != string.Empty).ToList();
            var distinct = present.Select(value => value!.ToString()).Distinct(StringComparer.Ordinal).Count();

            if (present.Count == 0)
            {
                return ((int)JournalFieldRole.Category, true);
            }

            var few = distinct <= MaxCategoryValues
                      && (present.Count < FewRows || distinct * 2 <= present.Count);

            return (few ? (int)JournalFieldRole.Category : (int)JournalFieldRole.Identifier, true);
        }
    }
}
