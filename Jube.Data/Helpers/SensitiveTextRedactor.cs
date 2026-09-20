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
using System.Text.RegularExpressions;

namespace Jube.Data.Helpers
{
    public static class SensitiveTextRedactor
    {
        public const string Mask = "[REDACTED]";
        private static readonly TimeSpan timeout = TimeSpan.FromMilliseconds(250);

        private static readonly Regex keyValue = new(
            @"(?<key>\b(?:password|passwd|pwd|passfile|sslpassword|secret|client_secret|api[_-]?key|access[_-]?key|auth[_-]?token|token|masterauth|requirepass)\b[""']?\s*[=:]\s*)(?<value>'(?:[^']|'')*'|""[^""]*""|[^\s;,&'"")]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, timeout);

        private static readonly Regex sqlPassword = new(
            @"(?<key>\bpassword\s+)(?<value>'(?:[^']|'')*'|E'(?:[^']|'')*'|\$\$.*?\$\$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline, timeout);

        private static readonly Regex urlUserInfo = new(
            @"(?<scheme>[a-z][a-z0-9+.-]*://[^\s:/@'""]*:)(?<value>[^\s@/'""]+)(?=@)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, timeout);

        private static readonly Regex bearerToken = new(@"(?<key>\bbearer\s+)(?<value>[A-Za-z0-9._~+/=-]{8,})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, timeout);

        private static readonly Regex jwt = new(@"\beyJ[A-Za-z0-9_-]{5,}\.[A-Za-z0-9_-]{5,}\.[A-Za-z0-9_-]*",
            RegexOptions.CultureInvariant, timeout);

        private static readonly Regex pem = new(
            @"-----BEGIN [A-Z ]*PRIVATE KEY-----.*?(?:-----END [A-Z ]*PRIVATE KEY-----|$)",
            RegexOptions.CultureInvariant | RegexOptions.Singleline, timeout);

        private static readonly HashSet<string> credentialConfigKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "requirepass", "masterauth", "masteruser", "primaryauth", "primaryuser", "tls-key-file-pass",
            "tls-client-key-file-pass"
        };

        public static string Redact(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 4)
            {
                return text;
            }

            try
            {
                var result = pem.Replace(text, Mask);
                result = sqlPassword.Replace(result, m => m.Groups["key"].Value + "'" + Mask + "'");
                result = keyValue.Replace(result, Mask);
                result = urlUserInfo.Replace(result, m => m.Groups["scheme"].Value + Mask);
                result = bearerToken.Replace(result, m => m.Groups["key"].Value + Mask);
                return jwt.Replace(result, Mask);
            }
            catch (RegexMatchTimeoutException)
            {
                return Mask;
            }
        }

        public static string RedactRedisCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return command;
            }

            var tokens = command.Split(' ');
            var name = tokens[0].ToUpperInvariant();
            switch (name)
            {
                case "AUTH":
                    for (var i = 1; i < tokens.Length; i++)
                    {
                        tokens[i] = Mask;
                    }

                    break;
                case "HELLO":
                    MaskAfterKeyword(tokens, "AUTH", 2);
                    break;
                case "MIGRATE":
                    MaskAfterKeyword(tokens, "AUTH", 1);
                    MaskAfterKeyword(tokens, "AUTH2", 2);
                    break;
                case "ACL" when tokens.Length > 1 && tokens[1].Equals("SETUSER", StringComparison.OrdinalIgnoreCase):
                    for (var i = 3; i < tokens.Length; i++)
                    {
                        if (tokens[i].Length > 0 && tokens[i][0] is '>' or '<' or '#' or '!')
                        {
                            tokens[i] = tokens[i][0] + Mask;
                        }
                    }

                    break;
                case "CONFIG" when tokens.Length > 2 && tokens[1].Equals("SET", StringComparison.OrdinalIgnoreCase):
                    for (var i = 2; i + 1 < tokens.Length; i += 2)
                    {
                        if (credentialConfigKeys.Contains(tokens[i]))
                        {
                            tokens[i + 1] = Mask;
                        }
                    }

                    break;
            }

            return Redact(string.Join(' ', tokens));
        }

        public static string RedactRedisKeyName(string commandName, string keyName)
        {
            if (string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(commandName))
            {
                return Redact(keyName);
            }

            return commandName.Equals("AUTH", StringComparison.OrdinalIgnoreCase)
                ? Mask
                : Redact(keyName);
        }

        private static void MaskAfterKeyword(string[] tokens, string keyword, int count)
        {
            for (var i = 1; i < tokens.Length; i++)
            {
                if (!tokens[i].Equals(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                for (var j = i + 1; j <= i + count && j < tokens.Length; j++)
                {
                    tokens[j] = Mask;
                }

                return;
            }
        }
    }
}