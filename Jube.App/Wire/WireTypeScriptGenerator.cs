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
using System.Linq;
using System.Text;
using System.Text.Json;
using Jube.Dto.Query.WireSchema;

namespace Jube.App.Wire
{
    public static class WireTypeScriptGenerator
    {
        public const string DecodersFile = "wire.generated.ts";
        public const string TypesFile = "wireTypes.generated.json";

        private static readonly HashSet<string> Primitives =
            ["string", "char", "int", "double", "bool", "guid", "date", "any", "bytes"];

        private static readonly string LicenceHeader = "/* Copyright (C) 2022-present Jube Holdings Limited.\n *\n * This file is part of Jube™ software.\n *\n * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License\n * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.\n * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty\n * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.\n\n * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,\n * see <https://www.gnu.org/licenses/>.\n */";

        public static IReadOnlyDictionary<string, string> Files(WireSchemaDto schema,
            IReadOnlySet<string> include = null)
        {
            var types = new SortedDictionary<string, WireTypeDto>(
                schema.Types.Where(type => include?.Contains(type.Key) ?? WireTypeRegistry.IsDtoType(type.Key))
                    .ToDictionary(type => type.Key, type => type.Value), StringComparer.Ordinal);

            return new Dictionary<string, string>
            {
                [DecodersFile] = Decoders(schema.TypesHash, types),
                [TypesFile] = TypesJson(schema.TypesHash, types)
            };
        }

        public static IReadOnlyList<string> Stale(string directory, WireSchemaDto schema,
            IReadOnlySet<string> include = null)
        {
            return Files(schema, include)
                .Where(file =>
                {
                    var path = Path.Combine(directory, file.Key);

                    return !File.Exists(path)
                           || File.ReadAllText(path).Replace("\r\n", "\n") != file.Value;
                })
                .Select(file => file.Key)
                .ToList();
        }

        public static IReadOnlyList<string> Write(string directory, WireSchemaDto schema,
            IReadOnlySet<string> include = null)
        {
            Directory.CreateDirectory(directory);

            var stale = Stale(directory, schema, include);

            foreach (var file in Files(schema, include).Where(file => stale.Contains(file.Key)))
            {
                File.WriteAllText(Path.Combine(directory, file.Key), file.Value, new UTF8Encoding(false));
            }

            return stale;
        }

        private static string TypesJson(string typesHash, SortedDictionary<string, WireTypeDto> types)
        {
            return JsonSerializer.Serialize(new { typesHash, types },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) + "\n";
        }

        private static string Decoders(string typesHash, SortedDictionary<string, WireTypeDto> types)
        {
            var names = Names(types.Keys);
            var text = new StringBuilder();

            text.Append(LicenceHeader).Append("\n\n");
            text.Append("import { dict, many, rows } from '../api/wireRuntime'\n\n");
            text.Append("type Decoder = (value: unknown) => unknown\n\n");
            text.Append($"export const typesHash = '{typesHash}'\n");

            foreach (var (name, type) in types)
            {
                text.Append('\n');
                text.Append($"function {names[name]}(value: unknown): unknown {{\n");
                text.Append("  if (value == null) return value\n");

                if (type.Fields.Count == 0)
                {
                    text.Append("  return {}\n}\n");

                    continue;
                }

                text.Append("  const a = value as unknown[]\n");
                text.Append("  return {\n");

                foreach (var field in type.Fields)
                {
                    text.Append($"    {Key(field.Name)}: {Read(field, names)},\n");
                }

                text.Append("  }\n}\n");
            }

            text.Append("\nexport const decoders: Record<string, Decoder> = {\n");

            foreach (var name in types.Keys)
            {
                text.Append($"  '{name}': {names[name]},\n");
            }

            text.Append("}\n");

            return text.ToString();
        }

        private static string Read(WireFieldDto field, Dictionary<string, string> names)
        {
            var cell = $"a[{field.Index}] ?? null";
            var convert = ConvertFor(field.Type, names);

            if (convert == null)
            {
                return cell;
            }

            return convert.IsIdentifier ? $"{convert.Expression}({cell})" : $"({convert.Expression})({cell})";
        }

        private static Conversion ConvertFor(string shape, Dictionary<string, string> names)
        {
            if (shape.EndsWith("{}#", StringComparison.Ordinal))
            {
                var inner = ConvertFor(shape[..^3], names);

                return new Conversion($"(v) => rows(v, {inner?.Expression ?? "null"})", false);
            }

            if (shape.EndsWith("[]", StringComparison.Ordinal))
            {
                var inner = ConvertFor(shape[..^2], names);

                return inner == null ? null : new Conversion($"(v) => many(v, {inner.Expression})", false);
            }

            if (shape.EndsWith("{}", StringComparison.Ordinal))
            {
                var inner = ConvertFor(shape[..^2], names);

                return inner == null ? null : new Conversion($"(v) => dict(v, {inner.Expression})", false);
            }

            var baseName = shape.EndsWith('?') ? shape[..^1] : shape;

            if (Primitives.Contains(baseName))
            {
                return null;
            }

            return names.TryGetValue(baseName, out var function)
                ? new Conversion(function, true)
                : throw new InvalidOperationException(
                    $"The wire shape {shape} names a type that is not in the Jube.Dto assembly.");
        }

        private static Dictionary<string, string> Names(IEnumerable<string> typeNames)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var name in typeNames)
            {
                var candidate = "d_" + new string(name.Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray());
                var unique = candidate;

                for (var suffix = 2; !used.Add(unique); suffix++)
                {
                    unique = candidate + "_" + suffix;
                }

                names[name] = unique;
            }

            return names;
        }

        private static string Key(string name)
        {
            if (name == "__proto__")
            {
                return "['__proto__']";
            }

            var simple = name.Length > 0
                         && (char.IsAsciiLetter(name[0]) || name[0] is '_' or '$')
                         && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '$');

            return simple ? name : "'" + name.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
        }

        private sealed record Conversion(string Expression, bool IsIdentifier);
    }
}
