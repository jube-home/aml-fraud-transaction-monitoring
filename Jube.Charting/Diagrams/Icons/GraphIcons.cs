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

namespace Jube.Charting.Diagrams.Icons
{
    public static class GraphIcons
    {
        public const string Fallback = "circle-dot";

        private static readonly (string[] Words, string Icon)[] keyWords =
        [
            (["case"], "briefcase"),
            (["account", "iban", "ledger"], "landmark"),
            (["payee", "beneficiary", "counterparty", "payer", "recipient"], "hand-coins"),
            (["device", "handset", "imei", "fingerprint"], "smartphone"),
            (["ip", "network", "subnet", "asn", "wifi", "hotspot"], "globe"),
            (["email", "mail"], "mail"),
            (["phone", "msisdn", "mobile", "telephone"], "phone"),
            (["card", "pan", "bin"], "credit-card"),
            (["merchant", "shop", "store", "terminal"], "store"),
            (["customer", "person", "user", "holder", "individual"], "user"),
            (["company", "business", "organisation", "organization", "entity", "corporate"], "building-2"),
            (["address", "location", "country", "city", "geo"], "map-pin"),
            (["wallet", "crypto", "bitcoin"], "wallet"),
            (["document", "passport", "licence", "license", "identity", "kyc"], "id-card"),
            (["session", "browser", "cookie"], "monitor"),
            (["tag"], "tag")
        ];

        public static IReadOnlyList<string> Names { get; } =
            LucideIcons.Elements.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        public static bool Exists(string? name)
        {
            return !string.IsNullOrWhiteSpace(name) && LucideIcons.Elements.ContainsKey(name);
        }

        public static IReadOnlyList<IconElement>? Resolve(string? name)
        {
            return !string.IsNullOrWhiteSpace(name) && LucideIcons.Elements.TryGetValue(name, out var elements)
                ? elements
                : null;
        }

        public static string ForNodeKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Fallback;
            }

            var tokens = Tokenise(key);

            foreach (var (words, icon) in keyWords)
            {
                if (words.Any(word => tokens.Contains(word)))
                {
                    return icon;
                }
            }

            return Fallback;
        }

        private static HashSet<string> Tokenise(string key)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            var current = new System.Text.StringBuilder();

            foreach (var c in key)
            {
                if (char.IsLetter(c) && char.IsUpper(c) && current.Length > 0 && !char.IsUpper(current[^1]))
                {
                    tokens.Add(current.ToString().ToLowerInvariant());
                    current.Clear();
                }

                if (char.IsLetterOrDigit(c))
                {
                    current.Append(c);
                }
                else if (current.Length > 0)
                {
                    tokens.Add(current.ToString().ToLowerInvariant());
                    current.Clear();
                }
            }

            if (current.Length > 0)
            {
                tokens.Add(current.ToString().ToLowerInvariant());
            }

            return tokens;
        }
    }
}
