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

using System.Text.RegularExpressions;

namespace Jube.App.Code.Waf.Models
{
    public sealed class WafCompiledException
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public Regex RouteRegex { get; init; }
        public Regex FieldRegex { get; init; }
        public int? WafSignatureId { get; init; }

        public bool Suppresses(string route, string field, int signatureId)
        {
            if (RouteRegex == null)
            {
                return false;
            }

            if (WafSignatureId.HasValue && WafSignatureId.Value != signatureId)
            {
                return false;
            }

            if (!SafeMatch(RouteRegex, route ?? string.Empty))
            {
                return false;
            }

            if (FieldRegex != null && !SafeMatch(FieldRegex, field ?? string.Empty))
            {
                return false;
            }

            return true;
        }

        private static bool SafeMatch(Regex regex, string input)
        {
            try
            {
                return regex.IsMatch(input);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}