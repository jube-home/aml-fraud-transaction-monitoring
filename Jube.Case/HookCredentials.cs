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

namespace Jube.Case
{
    using System.Text.RegularExpressions;

    public static partial class HookCredentials
    {
        public const string SecretPrefix = "JUBE_HOOK_";
        public const string ReservedPrefix = "JUBE_HOOK_ALLOWED_";

        private static readonly HashSet<string> ReservedHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Host", "Content-Length", "Content-Type", "Transfer-Encoding", "Connection", "X-Correlation-Id"
        };

        public static bool IsAllowedSecretName(string? name)
        {
            return !string.IsNullOrEmpty(name) && SecretNamePattern().IsMatch(name)
                   && !name.StartsWith(ReservedPrefix, StringComparison.Ordinal);
        }

        public static bool IsAllowedHeaderName(string? name)
        {
            return !string.IsNullOrEmpty(name) && HeaderNamePattern().IsMatch(name) && !ReservedHeaders.Contains(name);
        }

        public static string? ReadSecret(string? name)
        {
            return IsAllowedSecretName(name) ? Environment.GetEnvironmentVariable(name!) : null;
        }

        [GeneratedRegex("^JUBE_HOOK_[A-Z0-9_]{1,52}$")]
        private static partial Regex SecretNamePattern();

        [GeneratedRegex("^[A-Za-z][A-Za-z0-9-]{0,63}$")]
        private static partial Regex HeaderNamePattern();
    }
}
