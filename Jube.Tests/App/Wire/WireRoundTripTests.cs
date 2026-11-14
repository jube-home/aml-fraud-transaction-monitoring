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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Jube.App.Code;
using Jube.App.Wire;
using Jube.Dto.Query.WireSchema;
using MessagePack;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireRoundTripTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new UtcDateTimeJsonConverter() }
        };

        private static IEnumerable<Type> WireTypes()
        {
            return typeof(WireSchemaDto).Assembly.GetTypes()
                .Where(t => !t.IsGenericTypeDefinition && !t.IsAbstract
                            && t.GetCustomAttribute<MessagePackObjectAttribute>() != null)
                .OrderBy(t => t.FullName, StringComparer.Ordinal);
        }

        public static IEnumerable<object[]> Types()
        {
            return WireTypes().Select(t => new object[] { t.FullName });
        }

        [Fact]
        public void EveryDtoTypeHasAWireShape()
        {
            var registry = new WireTypeRegistry();

            var failures = WireTypes().Where(t => registry.ShapeOf(t) == null).Select(t => t.FullName).ToList();

            failures.Should().BeEmpty("every serialised member of a DTO must itself be encodable");
        }

        [Fact]
        public void KeysAreUniqueWithinEveryHierarchy()
        {
            var failures = new List<string>();

            foreach (var type in WireTypes())
            {
                var keys = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => p.GetCustomAttribute<KeyAttribute>()?.IntKey)
                    .Where(k => k != null)
                    .ToList();

                if (keys.Count != keys.Distinct().Count())
                {
                    failures.Add(type.FullName);
                }
            }

            failures.Should().BeEmpty();
        }

        [Fact]
        public void EveryDtoSurvivesAMessagePackRoundTripUnchangedInItsJsonForm()
        {
            var failures = new List<string>();

            foreach (var type in WireTypes())
            {
                try
                {
                    var source = Populate(type, 0);
                    var bytes = MessagePackSerializer.Serialize(type, source, WireSerializer.Options);
                    var result = MessagePackSerializer.Deserialize(type, bytes, WireSerializer.Options);

                    var expected = JsonSerializer.Serialize(source, type, Json);
                    var actual = JsonSerializer.Serialize(result, type, Json);

                    if (expected != actual)
                    {
                        failures.Add($"{type.FullName}: JSON form changed");
                    }
                }
                catch (Exception exception)
                {
                    failures.Add($"{type.FullName}: {exception.GetType().Name} {exception.Message}");
                }
            }

            failures.Should().BeEmpty();
        }

        [Fact]
        public void SchemaFieldsAreInKeyOrderAndNamedAsTheJsonForm()
        {
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            var schema = registry.GetSchema();

            var type = schema.Types[typeof(WireFieldDto).FullName!];

            type.Fields.Select(f => (f.Index, f.Name)).Should().Equal((0, "index"), (1, "name"), (2, "type"));
            schema.Hash.Should().HaveLength(16);
            registry.GetSchema().Hash.Should().Be(schema.Hash);
        }

        [MessagePackObject]
        public sealed class ShapeProbe
        {
            [Key(0)]
            public int? Count { get; set; }

            [Key(1)]
            public char Interval { get; set; }

            [Key(2)]
            public List<int?> Items { get; set; } = [];

            [Key(3)]
            public Dictionary<string, object> Extras { get; set; } = new();

            [Key(4)]
            public DateTimeOffset? When { get; set; }

            [Key(5)]
            public List<Dictionary<string, object>> Rows { get; set; } = [];

            [Key(6)]
            public Dictionary<string, object>[] Maps { get; set; } = [];
        }

        [Fact]
        public void NullableValueTypesCarryAQuestionMarkAndCharHasItsOwnTag()
        {
            var registry = new WireTypeRegistry();

            registry.ShapeOf(typeof(ShapeProbe)).Should().Be(typeof(ShapeProbe).FullName);
            registry.Attach([]);

            var fields = registry.GetSchema().Types[typeof(ShapeProbe).FullName!].Fields;

            fields.Select(f => f.Type).Should().Equal("int?", "char", "int?[]", "any{}", "date?", "any{}#", "any{}[]");
        }

        private static object Populate(Type type, int depth)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string)) return "text";
            if (type == typeof(bool)) return true;
            if (type == typeof(Guid)) return Guid.Parse("11111111-2222-3333-4444-555555555555");
            if (type == typeof(DateTime)) return new DateTime(2026, 5, 6, 7, 8, 9, DateTimeKind.Utc);
            if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2026, 5, 6, 7, 8, 9, TimeSpan.Zero);
            if (type == typeof(object)) return "any";
            if (type == typeof(char)) return (object)'c';
            if (type.IsEnum) return Enum.GetValues(type).Cast<object>().FirstOrDefault() ?? Activator.CreateInstance(type);
            if (type == typeof(double)) return 1.5d;
            if (type == typeof(float)) return 1.5f;
            if (type == typeof(decimal)) return 2.5m;
            if (type.IsPrimitive) return Convert.ChangeType(3, type);

            if (type.IsArray)
            {
                var element = type.GetElementType()!;
                var array = Array.CreateInstance(element, depth > 2 ? 0 : 1);

                if (array.Length == 1) array.SetValue(Populate(element, depth + 1), 0);

                return array;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() is var d
                && (d == typeof(Dictionary<,>) || d == typeof(IDictionary<,>)))
            {
                var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()))!;

                if (depth <= 2) dictionary["k"] = Populate(type.GetGenericArguments()[1], depth + 1);

                return dictionary;
            }

            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)
                && type.GetGenericArguments().Length == 1)
            {
                var element = type.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;

                if (depth <= 2) list.Add(Populate(element, depth + 1));

                return list;
            }

            var constructor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
            var instance = constructor.Invoke(constructor.GetParameters()
                .Select(p => Populate(p.ParameterType, depth + 1)).ToArray());

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.SetMethod is { IsPublic: true } && p.GetCustomAttribute<KeyAttribute>() != null))
            {
                if (depth > 3 && !property.PropertyType.IsValueType && property.PropertyType != typeof(string)) continue;

                property.SetValue(instance, Populate(property.PropertyType, depth + 1));
            }

            return instance;
        }
    }
}
