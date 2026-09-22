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

#nullable enable

using System.Text.RegularExpressions;

namespace Jube.Migrations.Branches.AbstractionCalculationCoderOnly
{
    public static class AbstractionCalculationScriptBuilder
    {
        private static readonly Regex safeName = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        public static bool TryBuild(int? abstractionCalculationTypeId, string? left, string? right, out string script)
        {
            script = string.Empty;

            var method = abstractionCalculationTypeId switch
            {
                1 => "Plus",
                2 => "Minus",
                3 => "RatioOf",
                4 => "Times",
                _ => null
            };

            if (method is null || !TryToken(left, out var leftToken) || !TryToken(right, out var rightToken))
            {
                return false;
            }

            script = $"Matched = Abstraction.{leftToken}.{method}(Abstraction.{rightToken}).ZeroIfUndefined()";

            return true;
        }

        public static string BuildPlaceholder(int? abstractionCalculationTypeId, string? left, string? right)
        {
            var (label, symbol) = abstractionCalculationTypeId switch
            {
                1 => ("Add", "+"),
                2 => ("Subtract", "-"),
                3 => ("Divide", "/"),
                4 => ("Multiply", "*"),
                _ => ("arithmetic", "?")
            };

            return $"' Converted from {label}: {Describe(left)} {symbol} {Describe(right)}. " +
                   "Rewrite this as a function, then activate it.";
        }

        private static string Describe(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "(none)";
            }

            var single = Regex.Replace(name.Trim(), "[\\r\\n]+", " ");

            return single.Length > 100 ? single[..100] : single;
        }

        private static bool TryToken(string? name, out string token)
        {
            token = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var candidate = name.Trim().Replace(" ", "_");
            if (!safeName.IsMatch(candidate))
            {
                return false;
            }

            token = candidate;

            return true;
        }
    }
}
