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

using System.Globalization;
using System.Resources;
using Jube.Data.Reporting.Check;
using Jube.Resources;

namespace Jube.Service.DynamicSql
{
    public static class SqlFindingText
    {
        private static readonly ResourceManager manager = new(typeof(SqlCheckResources));

        public static string Render(string code, IReadOnlyList<object> arguments, CultureInfo? culture = null)
        {
            var template = manager.GetString(code, culture ?? CultureInfo.CurrentUICulture);

            if (string.IsNullOrWhiteSpace(template))
            {
                return SqlCheckMessages.Format(code, arguments);
            }

            return string.Format(CultureInfo.CurrentUICulture, template,
                [.. arguments.Select(a => a?.ToString() ?? string.Empty)]);
        }

        public static string Render(SqlFinding finding, CultureInfo? culture = null)
        {
            return Render(finding.Code, finding.Arguments, culture);
        }

        public static string RenderEnglish(SqlFinding finding)
        {
            var text = finding.English;

            return finding.Line.HasValue ? $"{text} (line {finding.Line}, column {finding.Column})" : text;
        }
    }
}
