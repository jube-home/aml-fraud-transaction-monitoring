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

namespace Jube.Data.QueryBuilder
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json.Linq;

    public sealed record FilterField(string Id, BuilderFieldType Type, string Description, PropertyInfo Property);

    public static class BuilderFilter
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<FilterField>> fieldCache = new();

        public static IReadOnlyList<FilterField> Fields(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return fieldCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Select(p => (Property: p, Type: TypeOf(p.PropertyType)))
                .Where(p => p.Type != null)
                .Select(p => new FilterField(p.Property.Name, p.Type!.Value,
                    p.Property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty, p.Property))
                .OrderBy(f => f.Id, StringComparer.Ordinal)
                .ToList());
        }

        public static IReadOnlyDictionary<string, BuilderField> Catalogue(Type type)
        {
            return Fields(type).ToDictionary(f => f.Id, f => new BuilderField(f.Id, f.Type), StringComparer.Ordinal);
        }

        public static Func<T, bool> Compile<T>(BuilderGroup group)
        {
            ArgumentNullException.ThrowIfNull(group);

            var properties = Fields(typeof(T)).ToDictionary(f => f.Id, f => f.Property, StringComparer.Ordinal);
            var predicate = CompileGroup(group, id =>
            {
                var property = properties[id];
                return row => property.GetValue(row);
            });
            return row => predicate(row);
        }

        public static Func<IReadOnlyDictionary<string, object>, bool> CompileValues(BuilderGroup group)
        {
            ArgumentNullException.ThrowIfNull(group);

            var predicate = CompileGroup(group, id => row =>
                ((IReadOnlyDictionary<string, object>)row).TryGetValue(id, out var value) ? value : null);
            return values => predicate(values);
        }

        private static Func<object, bool> CompileGroup(BuilderGroup group,
            Func<string, Func<object, object>> accessor)
        {
            var parts = group.Rules.Select(node => node switch
            {
                BuilderGroup nested => CompileGroup(nested, accessor),
                BuilderRule rule => CompileRule(rule, accessor(rule.Id)),
                _ => throw new InvalidOperationException("Unknown builder node.")
            }).ToList();

            Func<object, bool> combined = string.Equals(group.Condition, "OR", StringComparison.OrdinalIgnoreCase)
                ? row => parts.Any(p => p(row))
                : row => parts.All(p => p(row));

            return group.Not ? row => !combined(row) : combined;
        }

        private static Func<object, bool> CompileRule(BuilderRule rule, Func<object, object> read)
        {
            var definition = BuilderOperators.ByType[rule.Operator];
            if (!definition.Evaluators.TryGetValue(rule.Type, out var evaluate) ||
                rule.Arguments.Any(a => a.IsField))
            {
                return _ => false;
            }

            var arguments = rule.Arguments.Select(a => Argument(rule.Type, a)).ToList();

            return row =>
            {
                var value = Value(rule.Type, read(row));
                if (value == null)
                {
                    return rule.Operator is BuilderProfile.IsEmpty or BuilderProfile.IsNull;
                }

                try
                {
                    return evaluate(value, arguments);
                }
                catch (TargetInvocationException)
                {
                    return false;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            };
        }

        private static object Argument(BuilderFieldType type, BuilderValue value)
        {
            var kind = value.Argument.Kind != BuilderArgumentKind.Value
                ? value.Argument.Kind
                : type switch
                {
                    BuilderFieldType.Integer or BuilderFieldType.Double => BuilderArgumentKind.Number,
                    BuilderFieldType.DateTime => BuilderArgumentKind.Date,
                    BuilderFieldType.String => BuilderArgumentKind.Text,
                    _ => BuilderArgumentKind.Value
                };

            var items = value.Literals.Select(l => Literal(type, kind, l)).ToList();
            return value.Argument.Many ? items : items[0];
        }

        private static object Literal(BuilderFieldType type, BuilderArgumentKind kind, JToken token)
        {
            return kind switch
            {
                BuilderArgumentKind.Number => token.Value<double>(),
                BuilderArgumentKind.Integer => token.Value<long>(),
                BuilderArgumentKind.Date => DateTimeOffset.Parse(token.Value<string>()!,
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime,
                BuilderArgumentKind.Text => token.Value<string>(),
                _ => type switch
                {
                    BuilderFieldType.Boolean => token.Value<string>() == "True",
                    BuilderFieldType.Guid => Guid.Parse(token.Value<string>()!),
                    _ => token.Value<string>()
                }
            };
        }

        private static object Value(BuilderFieldType type, object value)
        {
            return type switch
            {
                BuilderFieldType.String => Text(value),
                BuilderFieldType.Boolean => value as bool?,
                BuilderFieldType.Guid => value as Guid?,
                BuilderFieldType.DateTime => Instant(value),
                BuilderFieldType.Integer or BuilderFieldType.Double => value switch
                {
                    IConvertible convertible and not string and not bool => convertible.ToDouble(
                        CultureInfo.InvariantCulture),
                    _ => null
                },
                _ => null
            };
        }

        private static string Text(object value)
        {
            return value switch
            {
                string text => text,
                char character => character.ToString(),
                _ => null
            };
        }

        private static DateTime? Instant(object value)
        {
            return value switch
            {
                DateTimeOffset offset => offset.UtcDateTime,
                DateTime { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
                DateTime date => DateTime.SpecifyKind(date, DateTimeKind.Utc),
                _ => null
            };
        }

        private static BuilderFieldType? TypeOf(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying == typeof(string) || underlying == typeof(char))
            {
                return BuilderFieldType.String;
            }

            if (underlying == typeof(bool))
            {
                return BuilderFieldType.Boolean;
            }

            if (underlying == typeof(Guid))
            {
                return BuilderFieldType.Guid;
            }

            if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
            {
                return BuilderFieldType.DateTime;
            }

            if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short) ||
                underlying == typeof(byte))
            {
                return BuilderFieldType.Integer;
            }

            if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal))
            {
                return BuilderFieldType.Double;
            }

            return null;
        }
    }
}