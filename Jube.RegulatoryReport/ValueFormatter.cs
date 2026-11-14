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

using System.Globalization;

namespace Jube.RegulatoryReport
{
    internal static class ValueFormatter
    {
        public static (string? Value, ReportIssue? Issue) Apply(string? value, IEnumerable<string> steps,
            ReportProfile profile, string path)
        {
            var current = value;

            foreach (var step in steps)
            {
                if (string.IsNullOrEmpty(current))
                {
                    return (current, null);
                }

                var colon = step.IndexOf(':');
                var name = (colon < 0 ? step : step[..colon]).Trim().ToLowerInvariant();
                var argument = colon < 0 ? string.Empty : step[(colon + 1)..];

                switch (name)
                {
                    case "upper":
                        current = current.ToUpperInvariant();

                        break;
                    case "lower":
                        current = current.ToLowerInvariant();

                        break;
                    case "trim":
                        current = current.Trim();

                        break;
                    case "truncate" when int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture,
                        out var length):
                        current = current.Length > length ? current[..length] : current;

                        break;
                    case "date":
                        if (!DateTimeOffset.TryParse(current, CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal, out var date))
                        {
                            return (null, Issue("NotADate", $"'{current}' is not a date.", path));
                        }

                        current = date.ToString(argument, CultureInfo.InvariantCulture);

                        break;
                    case "number":
                        if (!decimal.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture,
                                out var number))
                        {
                            return (null, Issue("NotANumber", $"'{current}' is not a number.", path));
                        }

                        current = number.ToString(argument, CultureInfo.InvariantCulture);

                        break;
                    case "map":
                        if (!profile.CodeLists.TryGetValue(argument, out var list))
                        {
                            return (null, Issue("UnknownCodeList", $"The code list '{argument}' is not in the profile.",
                                path));
                        }

                        if (!list.TryGetValue(current, out var mapped))
                        {
                            return (null, Issue("UnknownCode",
                                $"'{current}' is not in the code list '{argument}'.", path));
                        }

                        current = mapped;

                        break;
                    default:
                        return (null, Issue("UnknownFormat", $"The format '{step}' is not known.", path));
                }
            }

            return (current, null);
        }

        private static ReportIssue Issue(string code, string message, string path)
        {
            return new ReportIssue(ReportIssueStage.Mapping, code, message, path);
        }
    }
}
