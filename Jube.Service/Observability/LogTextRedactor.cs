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

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Jube.Service.Observability
{
    public static class LogTextRedactor
    {
        private static readonly Regex basic = new(@"(?i)\bBasic\s+[A-Za-z0-9+/]{8,}=*",
            RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

        [return: NotNullIfNotNull(nameof(text))]
        public static string? Redact(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            try
            {
                return basic.Replace(Data.Helpers.SensitiveTextRedactor.Redact(text),
                    "Basic " + Data.Helpers.SensitiveTextRedactor.Mask);
            }
            catch (RegexMatchTimeoutException)
            {
                return Data.Helpers.SensitiveTextRedactor.Mask;
            }
        }
    }
}