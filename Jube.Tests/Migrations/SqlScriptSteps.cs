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
using System.IO;
using System.Text.RegularExpressions;

namespace Jube.Test.Migrations
{
    internal static class SqlScriptSteps
    {
        private static readonly Regex Marker = new(@"^--\s*@jube:step\s+(?<name>\S+)\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        public static string SolutionDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Jube.sln was not found above the test directory.");
        }

        public static List<(string Name, string Sql)> Split(string script)
        {
            var steps = new List<(string Name, string Sql)>();
            var matches = Marker.Matches(script);

            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index + matches[i].Length;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : script.Length;
                var sql = script[start..end].Trim();

                if (sql.Length > 0)
                {
                    steps.Add((matches[i].Groups["name"].Value, sql));
                }
            }

            return steps;
        }
    }
}
