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


namespace Jube.Data.Query.GetCaseKeyJournalRowsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using log4net;
    using Newtonsoft.Json.Linq;
    using Reporting;

    public class GetCaseKeyJournalRowsQuery(string connectionString, ILog log)
    {
        private static readonly Regex indexableKey = new(@"^[A-Za-z0-9_][A-Za-z0-9_ .:-]{0,127}\z",
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

        public const string Scope =
            "from \"Archive\" a " +
            "inner join \"EntityAnalysisModel\" m on m.\"Id\" = a.\"EntityAnalysisModelId\" " +
            "where a.\"EntityAnalysisModelId\" = (@1) " +
            "and exists (select 1 from \"UserInTenant\" u " +
            "           where u.\"TenantRegistryId\" = m.\"TenantRegistryId\" and u.\"User\" = (@2)) ";

        public const string SelectFrom =
            "select a.\"Id\", a.\"EntityAnalysisModelInstanceEntryGuid\", a.\"ReferenceDate\", " +
            "a.\"CreatedDate\", a.\"ResponseElevation\", a.\"EntryKeyValue\", a.\"ActivationRuleCount\", " +
            "a.\"Json\" " + Scope;

        public static string KeyPredicate(string key, string value, bool keyIndexed, List<object> tokens)
        {
            if (keyIndexed && IsIndexableKey(key) && UsesTextEquality(value))
            {
                tokens.Add(value);

                return $"and {IndexExpression(key)} = (@{tokens.Count}) ";
            }

            tokens.Add(Containment(key, value));
            tokens.Add(TypedContainment(key, value));

            return $"and (a.\"Json\" @> (@{tokens.Count - 1})::jsonb or a.\"Json\" @> (@{tokens.Count})::jsonb) ";
        }

        public static string FilterPredicate(Func<List<object>, string> filter, List<object> tokens)
        {
            return filter is null ? string.Empty : $"and {filter(tokens)} ";
        }

        public static bool IsIndexableKey(string key)
        {
            return indexableKey.IsMatch(key);
        }

        public static string IndexExpression(string key)
        {
            return $"(\"Json\" -> 'payload' ->> '{key.Replace("'", "''")}')";
        }

        public static bool UsesTextEquality(string value)
        {
            if (bool.TryParse(value, out _))
            {
                return false;
            }

            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            {
                return integer.ToString(CultureInfo.InvariantCulture) == value;
            }

            return !(double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                     && double.IsFinite(number));
        }

        public static string Containment(string key, string value)
        {
            return new JObject { ["payload"] = new JObject { [key] = value } }.ToString(
                Newtonsoft.Json.Formatting.None);
        }

        public static string TypedContainment(string key, string value)
        {
            JToken typed;

            if (bool.TryParse(value, out var boolean))
            {
                typed = boolean;
            }
            else if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            {
                typed = integer;
            }
            else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                     && double.IsFinite(number))
            {
                typed = number;
            }
            else
            {
                typed = value;
            }

            return new JObject { ["payload"] = new JObject { [key] = typed } }.ToString(
                Newtonsoft.Json.Formatting.None);
        }

        public static GetCaseKeyJournalRowsQueryDto Map(IDictionary<string, object> record)
        {
            return new GetCaseKeyJournalRowsQueryDto
            {
                Id = Convert.ToInt64(record["Id"], CultureInfo.InvariantCulture),
                EntityAnalysisModelInstanceEntryGuid = (Guid)record["EntityAnalysisModelInstanceEntryGuid"],
                ReferenceDate = record["ReferenceDate"] as DateTime?,
                CreatedDate = record["CreatedDate"] as DateTime?,
                ResponseElevation = record["ResponseElevation"] is DBNull
                    ? 0
                    : Convert.ToDouble(record["ResponseElevation"], CultureInfo.InvariantCulture),
                EntryKeyValue = record["EntryKeyValue"] as string,
                ActivationRuleCount = record["ActivationRuleCount"] is DBNull
                    ? 0
                    : Convert.ToInt32(record["ActivationRuleCount"], CultureInfo.InvariantCulture),
                Json = record["Json"]?.ToString()
            };
        }

        public async Task<List<GetCaseKeyJournalRowsQueryDto>> ExecuteAsync(int entityAnalysisModelId, string user,
            string key, string value, DateTime from, DateTime to, double responseElevation, int activationRuleCount,
            int limit, CancellationToken token = default, bool keyIndexed = false,
            Func<List<object>, string> filter = null)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var predicate = KeyPredicate(key, value, keyIndexed, tokens) + FilterPredicate(filter, tokens);

            var next = tokens.Count + 1;

            tokens.AddRange([from, to, responseElevation, activationRuleCount, limit]);

            var sql = SelectFrom + predicate +
                      $"and a.\"CreatedDate\" >= (@{next}) and a.\"CreatedDate\" <= (@{next + 1}) " +
                      $"and a.\"ResponseElevation\" >= (@{next + 2}) " +
                      $"and a.\"ActivationRuleCount\" >= (@{next + 3}) " +
                      "order by a.\"Id\" desc " +
                      $"limit (@{next + 4})";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(Map).ToList();
        }
    }
}
