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

namespace Jube.Data.Query.ArchiveKeyLookup
{
    using System;
    using System.Collections.Generic;

    public static class ArchiveKeyLookupSql
    {
        public const string Select =
            "select a.\"Id\", a.\"EntityAnalysisModelInstanceEntryGuid\", a.\"ReferenceDate\", " +
            "a.\"CreatedDate\", a.\"ResponseElevation\", a.\"EntryKeyValue\", a.\"ActivationRuleCount\", " +
            "a.\"Json\" ";

        public const string TenantExists =
            "exists (select 1 from \"UserInTenant\" u " +
            "        where u.\"TenantRegistryId\" = m.\"TenantRegistryId\" and u.\"User\" = ";

        public static Func<object, string> Ordered(List<object> tokens)
        {
            return value =>
            {
                tokens.Add(value);

                return $"(@{tokens.Count})";
            };
        }

        public static string From(ArchiveKeyPredicate predicate, Func<object, string> bind)
        {
            return predicate.EntryKey
                ? "from \"Archive\" a " +
                  "inner join \"EntityAnalysisModel\" m on m.\"Id\" = a.\"EntityAnalysisModelId\" " +
                  $"where {predicate.Condition("a", bind)} "
                : "from \"ArchiveKey\" k " +
                  "inner join \"Archive\" a on a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\" " +
                  "inner join \"EntityAnalysisModel\" m on m.\"Id\" = a.\"EntityAnalysisModelId\" " +
                  $"where {predicate.Condition("k", bind)} ";
        }

        public static string Scope(ArchiveKeyPredicate predicate, List<object> tokens)
        {
            return From(predicate, Ordered(tokens)) +
                   "and a.\"EntityAnalysisModelId\" = (@1) " +
                   $"and {TenantExists}(@2)) ";
        }

        public static string Exists(ArchiveKeyPredicate predicate, string archiveAlias, Func<object, string> bind)
        {
            return predicate.EntryKey
                ? predicate.Condition(archiveAlias, bind)
                : "exists (select 1 from \"ArchiveKey\" k " +
                  $"where k.\"EntityAnalysisModelInstanceEntryGuid\" = {archiveAlias}.\"EntityAnalysisModelInstanceEntryGuid\" " +
                  $"and {predicate.Condition("k", bind)})";
        }

        public static string ValueText(string alias, ArchiveKeyValueColumn column)
        {
            var name = ArchiveKeyPredicate.ColumnNameOf(column);

            return column switch
            {
                ArchiveKeyValueColumn.String => $"{alias}.\"{name}\"",
                ArchiveKeyValueColumn.Boolean => $"case {alias}.\"{name}\" when 1 then 'true' when 0 then 'false' end",
                ArchiveKeyValueColumn.Date =>
                    $"to_char({alias}.\"{name}\", 'YYYY-MM-DD\"T\"HH24:MI:SS.US\"Z\"')",
                _ => $"{alias}.\"{name}\"::text"
            };
        }
    }
}
