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


using System.Reflection;
using Jube.Preservation.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Preservation.Diff
{
    public sealed class PreservationDiffer
    {
        public const int MaximumEntries = 5000;
        public const int MaximumValueLength = 200;
        private const int ValuePreviewLength = 120;

        private static readonly string[] keyCandidates =
            ["Name", "KvpKey", "ListValue", "Key", "Title", "Value", "Description"];

        private static readonly HashSet<string> ignored = new(StringComparer.Ordinal)
        {
            "Id", "Guid", "TenantRegistryId", "InheritedId", "ImportId", "Version", "Deleted", "DeletedDate",
            "DeletedUser", "CreatedDate", "CreatedUser", "UpdatedDate", "UpdatedUser"
        };

        private static readonly Lazy<HashSet<string>> entityNames = new(Exported);

        private readonly List<DiffEntry> entries = [];
        private readonly Dictionary<string, DiffSection> sections = new(StringComparer.Ordinal);
        private int referencesIgnored;
        private bool truncated;

        private PreservationDiffer()
        {
        }

        public static DiffResult Compare(JObject left, JObject right)
        {
            var differ = new PreservationDiffer();

            foreach (var name in left.Properties().Select(p => p.Name).Union(right.Properties().Select(p => p.Name)))
            {
                differ.Property(name, [], left[name], right[name], name);
            }

            var all = differ.sections.Values.OrderBy(s => s.Name, StringComparer.Ordinal).ToList();

            return new DiffResult
            {
                Sections = all,
                Entries = differ.entries,
                Added = all.Sum(s => s.Added),
                Removed = all.Sum(s => s.Removed),
                Changed = all.Sum(s => s.Changed),
                Unchanged = all.Sum(s => s.Unchanged),
                ReferencesIgnored = differ.referencesIgnored,
                Truncated = differ.truncated
            };
        }

        private static HashSet<string> Exported()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<Type>();

            void Walk(Type type)
            {
                var element = type.IsGenericType && type.GetGenericArguments().Length == 1
                    ? type.GetGenericArguments()[0]
                    : type;

                if (!visited.Add(element) || element.IsPrimitive || element == typeof(string) ||
                    element.Namespace is null || !element.Namespace.StartsWith("Jube.", StringComparison.Ordinal))
                {
                    return;
                }

                if (element.Namespace == "Jube.Data.Poco")
                {
                    names.Add(element.Name);
                }

                var pocoType = element.Namespace == "Jube.Data.Poco";

                foreach (var property in element.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (pocoType && !(property.PropertyType.IsGenericType &&
                                      property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                    {
                        continue;
                    }

                    Walk(property.PropertyType);
                }
            }

            Walk(typeof(Payload));

            return names;
        }

        public static JObject PayloadOf(string wrapperJson)
        {
            var wrapper = JObject.Parse(wrapperJson);

            return wrapper["Payload"] as JObject ?? [];
        }

        private DiffSection Section(string name)
        {
            if (!sections.TryGetValue(name, out var found))
            {
                found = new DiffSection { Name = name };
                sections[name] = found;
            }

            return found;
        }

        private void Add(DiffEntry entry)
        {
            if (entries.Count >= MaximumEntries)
            {
                truncated = true;

                return;
            }

            entries.Add(entry);
        }

        private void Property(string section, List<string> path, JToken? left, JToken? right, string name)
        {
            left = Present(left);
            right = Present(right);

            if (left is null && right is null)
            {
                return;
            }

            if (Collection(left) || Collection(right))
            {
                Array(section, path, name, left as JArray, right as JArray);

                return;
            }

            if (left is JObject || right is JObject)
            {
                Single(section, path, name, left as JObject, right as JObject);
            }
        }

        private void Single(string section, List<string> path, string name, JObject? left, JObject? right)
        {
            var next = new List<string>(path) { name };

            if (left is null || right is null)
            {
                Whole(section, next, left ?? right!, left is null ? DiffEntryKind.Added : DiffEntryKind.Removed);

                return;
            }

            Objects(section, next, left, right);
        }

        private void Array(string section, List<string> path, string name, JArray? left, JArray? right)
        {
            var leftItems = Items(left);
            var rightItems = Items(right);

            if (leftItems.Count == 0 && rightItems.Count == 0)
            {
                return;
            }

            var key = KeyOf(leftItems, rightItems);

            if (key is null)
            {
                SetSemantics(section, path, name, leftItems, rightItems);

                return;
            }

            var rightByKey = rightItems.ToDictionary(o => (string)o[key]!, StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in leftItems)
            {
                var label = (string)item[key]!;
                var next = new List<string>(path) { $"{name}[{label}]" };

                if (rightByKey.TryGetValue(label, out var match))
                {
                    seen.Add(label);
                    Objects(section, next, item, match);
                }
                else
                {
                    Whole(section, next, item, DiffEntryKind.Removed);
                }
            }

            foreach (var item in rightItems.Where(o => !seen.Contains((string)o[key]!)))
            {
                Whole(section, new List<string>(path) { $"{name}[{(string)item[key]!}]" }, item,
                    DiffEntryKind.Added);
            }
        }

        private void SetSemantics(string section, List<string> path, string name, List<JObject> leftItems,
            List<JObject> rightItems)
        {
            var rightPool = rightItems.Select(o => (Item: o, Canon: Canonical(o))).ToList();

            foreach (var item in leftItems)
            {
                var canon = Canonical(item);
                var index = rightPool.FindIndex(r => r.Canon == canon);

                if (index >= 0)
                {
                    rightPool.RemoveAt(index);
                    Section(section).Unchanged++;
                }
                else
                {
                    Whole(section, new List<string>(path) { $"{name}[{Label(item)}]" }, item,
                        DiffEntryKind.Removed);
                }
            }

            foreach (var (item, _) in rightPool)
            {
                Whole(section, new List<string>(path) { $"{name}[{Label(item)}]" }, item, DiffEntryKind.Added);
            }
        }

        private void Objects(string section, List<string> path, JObject left, JObject right)
        {
            var changes = new List<DiffChange>();

            foreach (var name in left.Properties().Select(p => p.Name).Union(right.Properties().Select(p => p.Name)))
            {
                var l = Present(left[name]);
                var r = Present(right[name]);

                if (Collection(l) || Collection(r) || l is JObject || r is JObject)
                {
                    Property(section, path, l, r, name);

                    continue;
                }

                if (Ignored(name, l ?? r))
                {
                    if (!JToken.DeepEquals(l, r))
                    {
                        referencesIgnored++;
                    }

                    continue;
                }

                if (!JToken.DeepEquals(l, r))
                {
                    changes.Add(new DiffChange(name, Display(l), Display(r)));
                }
            }

            if (changes.Count == 0)
            {
                Section(section).Unchanged++;

                return;
            }

            Section(section).Changed++;
            Add(new DiffEntry
            {
                Section = section, Path = path, Kind = DiffEntryKind.Changed, Changes = changes
            });
        }

        private void Whole(string section, List<string> path, JObject item, DiffEntryKind kind)
        {
            var count = Count(item);
            var target = Section(section);

            if (kind == DiffEntryKind.Added)
            {
                target.Added += count;
            }
            else
            {
                target.Removed += count;
            }

            Add(new DiffEntry { Section = section, Path = path, Kind = kind, NestedCount = count - 1 });
        }

        private static int Count(JToken token)
        {
            var total = token is JObject ? 1 : 0;

            foreach (var child in token.Children())
            {
                total += Count(child is JProperty property ? property.Value : child);
            }

            return total;
        }

        private static JToken? Present(JToken? token)
        {
            return token is null || token.Type == JTokenType.Null || token is JArray { Count: 0 } ? null : token;
        }

        private static bool Collection(JToken? token)
        {
            return token is JArray array && array.Any(i => i is JObject);
        }

        private static List<JObject> Items(JArray? array)
        {
            return array?.OfType<JObject>().ToList() ?? [];
        }

        private static string? KeyOf(List<JObject> left, List<JObject> right)
        {
            var all = left.Concat(right).ToList();

            foreach (var candidate in keyCandidates)
            {
                if (all.Any(o => o[candidate] is not { Type: JTokenType.String }))
                {
                    continue;
                }

                if (Unique(left, candidate) && Unique(right, candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool Unique(List<JObject> items, string key)
        {
            return items.Select(o => (string)o[key]!).Distinct(StringComparer.Ordinal).Count() == items.Count;
        }

        private static string Label(JObject item)
        {
            var parts = item.Properties()
                .Where(p => p.Value is JValue { Type: not JTokenType.Null } && !Ignored(p.Name, p.Value))
                .Take(2)
                .Select(p => $"{p.Name}={Display(p.Value)}");

            return string.Join(", ", parts);
        }

        private static string Canonical(JToken token)
        {
            switch (token)
            {
                case JObject obj:
                    return "{" + string.Join(",", obj.Properties()
                        .Where(p => !Ignored(p.Name, p.Value))
                        .OrderBy(p => p.Name, StringComparer.Ordinal)
                        .Select(p => p.Name + ":" + Canonical(p.Value))) + "}";
                case JArray array:
                    return "[" + string.Join(",", array.Select(Canonical)) + "]";
                default:
                    return token.ToString(Formatting.None);
            }
        }

        private static bool Ignored(string name, JToken? value)
        {
            if (ignored.Contains(name) || name.EndsWith("Guid", StringComparison.Ordinal))
            {
                return true;
            }

            return value is { Type: JTokenType.Integer } && name.EndsWith("Id", StringComparison.Ordinal) &&
                   name.Length > 2 && entityNames.Value.Contains(name[..^2]);
        }

        private static string? Display(JToken? token)
        {
            if (token is null)
            {
                return null;
            }

            var text = token.Type == JTokenType.String ? token.Value<string>() ?? string.Empty
                : token.ToString(Formatting.None);

            return text.Length > MaximumValueLength
                ? text[..ValuePreviewLength] + "\u2026 (" + text.Length + " characters)"
                : text;
        }
    }
}
