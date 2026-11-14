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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jube.Dto.Query.WireSchema;
using MessagePack;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Wire
{
    public sealed class WireTypeRegistry(WireOptions options = null)
    {
        private const string NotEncodable = "";

        private static readonly Dictionary<Type, string> Primitives = new()
        {
            { typeof(bool), "bool" },
            { typeof(sbyte), "int" },
            { typeof(byte), "int" },
            { typeof(short), "int" },
            { typeof(ushort), "int" },
            { typeof(int), "int" },
            { typeof(uint), "int" },
            { typeof(long), "int" },
            { typeof(ulong), "int" },
            { typeof(float), "double" },
            { typeof(double), "double" },
            { typeof(decimal), "double" },
            { typeof(string), "string" },
            { typeof(char), "char" },
            { typeof(Guid), "guid" },
            { typeof(DateTime), "date" },
            { typeof(DateTimeOffset), "date" },
            { typeof(object), "any" },
            { typeof(byte[]), "bytes" }
        };

        private static readonly Type[] WireTypes = typeof(WireSchemaDto).Assembly.GetTypes()
            .Where(t => !t.IsGenericTypeDefinition && t.GetCustomAttribute<MessagePackObjectAttribute>() != null)
            .ToArray();

        private static readonly HashSet<string> DtoTypeNames = WireTypes
            .Select(t => t.FullName ?? t.Name)
            .ToHashSet(StringComparer.Ordinal);

        private readonly object gate = new();
        private readonly ConcurrentDictionary<Type, string> shapes = new();
        private readonly ConcurrentDictionary<string, WireTypeDto> types = new();
        private readonly ConcurrentDictionary<string, bool> rowCarriers = new();

        private HashSet<string> generatedNames = [];
        private ICollection<EndpointDataSource> dataSources = [];
        private WireSchemaDto schema;
        private WireSchemaDto head;
        private WireSchemaDto headOf;
        private int schemaTypeCount = -1;

        public WireMode Mode { get; } = options?.Mode ?? WireMode.MessagePack;

        public static bool IsDtoType(string name)
        {
            return DtoTypeNames.Contains(name);
        }

        public static string TypesHashOf(IEnumerable<KeyValuePair<string, WireTypeDto>> generatedTypes)
        {
            var ordered = new SortedDictionary<string, WireTypeDto>(
                generatedTypes.ToDictionary(type => type.Key, type => type.Value), StringComparer.Ordinal);

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ordered))))[..16];
        }

        public WireCompression Compression { get; } = options?.Compression ?? WireCompression.None;

        public bool GeneratedDecoders { get; } = options?.GeneratedDecoders ?? true;

        public IReadOnlySet<string> GeneratedTypeNames
        {
            get
            {
                GetSchema();

                return generatedNames;
            }
        }

        public string Hash => GetSchema().Hash;

        public bool CarriesRows(string shape)
        {
            return rowCarriers.GetOrAdd(shape, key => CarriesRows(key, []));
        }

        private bool CarriesRows(string shape, HashSet<string> visited)
        {
            if (shape.Contains("{}#", StringComparison.Ordinal))
            {
                return true;
            }

            var name = BaseNameOf(shape);

            if (!visited.Add(name) || !types.TryGetValue(name, out var type))
            {
                return false;
            }

            return type.Fields.Any(field => CarriesRows(field.Type, visited));
        }

        public void Attach(ICollection<EndpointDataSource> sources)
        {
            lock (gate)
            {
                dataSources = sources;
                schema = null;
                schemaTypeCount = -1;
            }
        }

        public string ShapeOf(Type type)
        {
            if (shapes.TryGetValue(type, out var known))
            {
                return known == NotEncodable ? null : known;
            }

            lock (gate)
            {
                var shape = Resolve(type, []);
                shapes[type] = shape ?? NotEncodable;

                return shape;
            }
        }

        public WireSchemaDto GetSchema()
        {
            lock (gate)
            {
                if (schema != null && schemaTypeCount == types.Count)
                {
                    return schema;
                }

                var routes = new List<WireRouteDto>();

                var roots = new List<string>();

                foreach (var type in WireTypes)
                {
                    roots.Add(Register(type));
                }

                foreach (var type in WireEndpointCatalogue.PayloadTypes)
                {
                    roots.Add(Register(type));
                }

                foreach (var endpoint in dataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>())
                {
                    foreach (var produces in endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>())
                    {
                        if (produces.Type != null && produces.Type != typeof(void))
                        {
                            Register(produces.Type);
                        }
                    }

                    var accepts = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();
                    var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();

                    if (accepts?.RequestType == null || methods == null
                                                     || !accepts.ContentTypes.Any(c =>
                                                         c.Contains("json", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var shape = Register(accepts.RequestType);

                    if (shape == null || endpoint.RoutePattern.RawText == null)
                    {
                        continue;
                    }

                    routes.AddRange(methods.HttpMethods.Select(method => new WireRouteDto
                    {
                        Method = method.ToUpperInvariant(),
                        Template = endpoint.RoutePattern.RawText.Trim('/'),
                        Shape = shape
                    }));
                }

                var ordered = new SortedDictionary<string, WireTypeDto>(types, StringComparer.Ordinal);
                var orderedRoutes = routes
                    .DistinctBy(route => (route.Method, route.Template))
                    .OrderBy(route => route.Template, StringComparer.Ordinal)
                    .ThenBy(route => route.Method, StringComparer.Ordinal)
                    .ToList();

                var built = new WireSchemaDto
                {
                    Mode = Mode.ToString(),
                    GeneratedDecoders = GeneratedDecoders,
                    Encryption = options?.Encryption ?? false,
                    EncryptionPublicKey = options?.EncryptionPublicKey ?? string.Empty,
                    Types = new Dictionary<string, WireTypeDto>(ordered),
                    Routes = orderedRoutes
                };

                generatedNames = ClosureOf(roots);
                built.TypesHash = TypesHashOf(built.Types.Where(type => generatedNames.Contains(type.Key)));
                built.Hash = Convert.ToHexString(SHA256.HashData(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { built.Mode, built.GeneratedDecoders, built.Encryption, built.EncryptionPublicKey, built.Types, built.Routes }))))[..16];

                schema = built;
                schemaTypeCount = types.Count;

                return built;
            }
        }

        public WireSchemaDto GetHead()
        {
            var full = GetSchema();

            lock (gate)
            {
                if (head != null && ReferenceEquals(headOf, full))
                {
                    return head;
                }

                head = Subset(full, new HashSet<string>(), true);
                headOf = full;

                return head;
            }
        }

        public WireSchemaDto GetShapes(IEnumerable<string> shapes)
        {
            var full = GetSchema();

            return Subset(full, ClosureOf(shapes), false);
        }

        private static WireSchemaDto Subset(WireSchemaDto full, ISet<string> names, bool includeRoutes)
        {
            var subset = new WireSchemaDto
            {
                Hash = full.Hash,
                Mode = full.Mode,
                Encryption = full.Encryption,
                EncryptionPublicKey = full.EncryptionPublicKey,
                TypesHash = full.TypesHash,
                GeneratedDecoders = full.GeneratedDecoders,
                Routes = includeRoutes ? full.Routes : []
            };

            foreach (var name in names)
            {
                if (full.Types.TryGetValue(name, out var type))
                {
                    subset.Types[name] = type;
                }
            }

            return subset;
        }

        private static string BaseNameOf(string shape)
        {
            while (true)
            {
                if (shape.EndsWith("{}#", StringComparison.Ordinal))
                {
                    shape = shape[..^3];
                }
                else if (shape.EndsWith("[]", StringComparison.Ordinal) ||
                         shape.EndsWith("{}", StringComparison.Ordinal))
                {
                    shape = shape[..^2];
                }
                else if (shape.EndsWith('?'))
                {
                    shape = shape[..^1];
                }
                else
                {
                    return shape;
                }
            }
        }

        private HashSet<string> ClosureOf(IEnumerable<string> roots)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            void Visit(string shape)
            {
                if (shape == null)
                {
                    return;
                }

                var name = BaseNameOf(shape);

                if (!types.TryGetValue(name, out var type) || !names.Add(name))
                {
                    return;
                }

                foreach (var field in type.Fields)
                {
                    Visit(field.Type);
                }
            }

            foreach (var root in roots)
            {
                Visit(root);
            }

            return names;
        }

        private string Register(Type type)
        {
            if (shapes.TryGetValue(type, out var known))
            {
                return known == NotEncodable ? null : known;
            }

            var shape = Resolve(type, []);
            shapes[type] = shape ?? NotEncodable;

            return shape;
        }

        private string Resolve(Type type, HashSet<Type> visiting)
        {
            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null)
            {
                return Suffix(Resolve(underlying, visiting), "?");
            }

            if (Primitives.TryGetValue(type, out var primitive))
            {
                return primitive;
            }

            if (type.IsEnum)
            {
                return "int";
            }

            if (type.IsArray)
            {
                return type.GetArrayRank() == 1 && type.GetElementType() != typeof(byte)
                    ? Suffix(Resolve(type.GetElementType(), visiting), "[]")
                    : null;
            }

            if (type.GetCustomAttribute<MessagePackObjectAttribute>() != null)
            {
                return Describe(type, visiting);
            }

            if (ColumnarRows.TryGetValueType(type, out var rowValueType))
            {
                return Suffix(Resolve(rowValueType, visiting), "{}#");
            }

            if (DictionaryValueType(type) is { } valueType)
            {
                return Suffix(Resolve(valueType, visiting), "{}");
            }

            if (type != typeof(string) && EnumerableElementType(type) is { } elementType)
            {
                return Suffix(Resolve(elementType, visiting), "[]");
            }

            return null;
        }

        private string Describe(Type type, HashSet<Type> visiting)
        {
            var name = type.FullName ?? type.Name;

            if (types.ContainsKey(name) || visiting.Contains(type))
            {
                return name;
            }

            visiting.Add(type);

            var fields = new List<WireFieldDto>();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var key = property.GetCustomAttribute<KeyAttribute>();

                if (key?.IntKey == null)
                {
                    continue;
                }

                var shape = Resolve(property.PropertyType, visiting);

                if (shape == null)
                {
                    visiting.Remove(type);

                    return null;
                }

                fields.Add(new WireFieldDto
                {
                    Index = key.IntKey.Value,
                    Name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                           ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name),
                    Type = shape
                });
            }

            types[name] = new WireTypeDto { Fields = fields.OrderBy(field => field.Index).ToList() };

            return name;
        }

        private static string Suffix(string inner, string suffix)
        {
            return inner == null ? null : inner + suffix;
        }

        private static Type DictionaryValueType(Type type)
        {
            var dictionary = new[] { type }.Concat(type.GetInterfaces())
                .FirstOrDefault(i => i.IsGenericType
                                     && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                                         || i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

            return dictionary != null && WireDictionaryKeys.IsSupported(dictionary.GetGenericArguments()[0])
                ? dictionary.GetGenericArguments()[1]
                : null;
        }

        private static Type EnumerableElementType(Type type)
        {
            var enumerable = new[] { type }.Concat(type.GetInterfaces())
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerable?.GetGenericArguments()[0];
        }
    }
}
