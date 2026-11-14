/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Jube.Service.DynamicSql
{
    public static class SqlValidationLedger
    {
        private static readonly TimeSpan lifetime = TimeSpan.FromHours(6);
        private static readonly Regex whitespace = new(@"\s+", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<string, Entry> scopes = new(StringComparer.Ordinal);

        public static void Record(string scope, string sql)
        {
            Prune();

            var entry = scopes.GetOrAdd(scope, _ => new Entry());

            lock (entry.Fingerprints)
            {
                entry.Fingerprints.Add(Fingerprint(sql));
                entry.Touched = DateTime.UtcNow;
            }
        }

        public static bool Has(string scope, string sql)
        {
            if (!scopes.TryGetValue(scope, out var entry))
            {
                return false;
            }

            lock (entry.Fingerprints)
            {
                entry.Touched = DateTime.UtcNow;

                return entry.Fingerprints.Contains(Fingerprint(sql));
            }
        }

        public static string Fingerprint(string sql)
        {
            var normalised = whitespace.Replace(sql.Trim().TrimEnd(';'), " ");

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalised)));
        }

        private static void Prune()
        {
            var cutoff = DateTime.UtcNow - lifetime;

            foreach (var stale in scopes.Where(s => s.Value.Touched < cutoff).Select(s => s.Key).ToList())
            {
                scopes.TryRemove(stale, out _);
            }
        }

        private sealed class Entry
        {
            public HashSet<string> Fingerprints { get; } = [];
            public DateTime Touched { get; set; } = DateTime.UtcNow;
        }
    }
}
