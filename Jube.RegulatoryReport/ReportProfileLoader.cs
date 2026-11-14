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

using System.Text.Json;

namespace Jube.RegulatoryReport
{
    public static class ReportProfileLoader
    {
        public const string ProfileFile = "profile.json";

        public static ReportProfile Load(string directory)
        {
            var root = Path.GetFullPath(directory);
            var file = Path.Combine(root, ProfileFile);

            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"{ProfileFile} was not found in the profile directory.", file);
            }

            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var json = document.RootElement;

            var name = Required(json, "name");
            var template = ReadInside(root, Required(json, "template"));
            var schemas = new Dictionary<string, string>(StringComparer.Ordinal);

            if (json.TryGetProperty("schemas", out var schemaList) && schemaList.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in schemaList.EnumerateArray())
                {
                    var relative = entry.GetString() ?? throw new InvalidDataException("A schema entry is empty.");
                    schemas[relative] = ReadInside(root, relative);
                }
            }

            var codeLists = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

            if (json.TryGetProperty("codeLists", out var lists) && lists.ValueKind == JsonValueKind.Object)
            {
                foreach (var list in lists.EnumerateObject())
                {
                    codeLists[list.Name] = list.Value.EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
                }
            }

            var description = json.TryGetProperty("description", out var d) ? d.GetString() : null;

            return new ReportProfile(name, template, schemas, codeLists, description);
        }

        private static string Required(JsonElement json, string property)
        {
            if (json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                                                              && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString()!;
            }

            throw new InvalidDataException($"The profile needs a '{property}'.");
        }

        private static string ReadInside(string root, string relative)
        {
            var path = Path.GetFullPath(Path.Combine(root, relative));

            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"'{relative}' is outside the profile directory.");
            }

            var info = new FileInfo(path);

            if (!info.Exists)
            {
                throw new FileNotFoundException($"'{relative}' was not found in the profile directory.", path);
            }

            if (info.Length > ReportProfile.MaximumTemplateCharacters * 4L)
            {
                throw new InvalidDataException($"'{relative}' is too large.");
            }

            return File.ReadAllText(path);
        }
    }
}
