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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Svirfd;

namespace Jube.Svirfd
{
    public static class SvirfdMembers
    {
        public const int StringType = 1;
        public const int IntegerType = 2;
        public const int DoubleType = 3;
        public const int DateType = 4;
        public const int BooleanType = 5;
        public const string DistanceKmMethod = "DistanceKm";
        public const string DistanceMilesMethod = "DistanceMiles";
        public const string WithinKmMethod = "WithinKm";

        private const string IntegerCast = "::int";
        private const string LongCast = "::bigint";
        private const string DoubleCast = "::double precision";
        private const string BooleanCast = "::boolean";
        private const string DateCast = "::timestamp";

        private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal)
        {
            nameof(IpResponse.Narrative), nameof(IpResponse.RequestId), nameof(IpResponse.ServedFrom),
            nameof(IpResponse.Attribution)
        };

        private static readonly HashSet<string> VolatilePaths = new(StringComparer.Ordinal)
        {
            $"{nameof(IpResponse.AsOf)}.{nameof(AsOfBlock.IndexLoaded)}",
            $"{nameof(IpResponse.AsOf)}.{nameof(AsOfBlock.Routing)}",
            $"{nameof(IpResponse.AsOf)}.{nameof(AsOfBlock.Registry)}"
        };

        private static readonly Dictionary<string, string> FirstElementAliases = new(StringComparer.Ordinal)
        {
            [$"{nameof(IpResponse.Routing)}.{nameof(RoutingBlock.Origins)}"] = "Origin"
        };

        public static readonly IReadOnlyList<string> MethodNames =
            [DistanceKmMethod, DistanceMilesMethod, WithinKmMethod];

        public static readonly IReadOnlyList<SvirfdMember> Scalars = Build();

        public static readonly IReadOnlyList<string> Tokens =
        [
            .. Scalars.SelectMany(s => s.Segments).Concat(MethodNames).Concat(["FirstOrigin"])
                .SelectMany(t => new[] { t, t + "?" }).Distinct(StringComparer.Ordinal)
        ];

        private static readonly Dictionary<string, SvirfdMember> ByName =
            Scalars.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

        public static bool TryGet(string? name, out SvirfdMember? member)
        {
            member = null;

            return name != null && ByName.TryGetValue(name, out member);
        }

        public static bool IsMethod(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var bracket = name.IndexOf('(');
            var bare = bracket > 0 ? name[..bracket] : name;

            return MethodNames.Contains(bare, StringComparer.OrdinalIgnoreCase);
        }

        public static string VisualBasicSegment(string segment)
        {
            if (string.Equals(segment, "Origin", StringComparison.OrdinalIgnoreCase))
            {
                return "FirstOrigin()";
            }

            return string.Equals(segment, "Count", StringComparison.OrdinalIgnoreCase) ? "Count()" : segment;
        }

        public static bool IsMember(string? name)
        {
            return IsMethod(name) || TryGet(name, out _);
        }

        public static bool Split(string? qualifiedName, out string? entityName, out string? memberName)
        {
            entityName = null;
            memberName = null;

            if (string.IsNullOrEmpty(qualifiedName))
            {
                return false;
            }

            var separator = qualifiedName.IndexOf('.');

            if (separator <= 0 || separator == qualifiedName.Length - 1)
            {
                return false;
            }

            entityName = qualifiedName[..separator];
            memberName = qualifiedName[(separator + 1)..];

            return true;
        }

        public static bool TryReadNumber(IDictionary<string, IpResponse>? records, string? qualifiedName,
            out double value)
        {
            value = 0;

            if (!TryResolve(records, qualifiedName, out var record, out var member))
            {
                return false;
            }

            switch (member!.Read(record!))
            {
                case bool flag:
                    value = flag ? 1 : 0;

                    return true;
                case string text:
                    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                case DateTime date:
                    value = date.ToOADate();

                    return true;
                case DateOnly day:
                    value = day.ToDateTime(TimeOnly.MinValue).ToOADate();

                    return true;
                case IConvertible convertible:
                    value = convertible.ToDouble(CultureInfo.InvariantCulture);

                    return true;
                default:
                    return false;
            }
        }

        public static bool TryReadText(IDictionary<string, IpResponse>? records, string? qualifiedName,
            out string? value)
        {
            value = null;

            if (!TryResolve(records, qualifiedName, out var record, out var member))
            {
                return false;
            }

            value = ToText(member!.Read(record!));

            return true;
        }

        public static string? ToText(object? value)
        {
            return value switch
            {
                null => null,
                string text => text,
                bool flag => flag ? "true" : "false",
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        public static string CamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            {
                return name;
            }

            return char.ToLowerInvariant(name[0]) + name[1..];
        }

        private static bool TryResolve(IDictionary<string, IpResponse>? records, string? qualifiedName,
            out IpResponse? record, out SvirfdMember? member)
        {
            record = null;
            member = null;

            if (records == null || !Split(qualifiedName, out var entityName, out var memberName) ||
                !TryGet(memberName, out member))
            {
                return false;
            }

            return records.TryGetValue(entityName!, out record);
        }

        private static List<SvirfdMember> Build()
        {
            var members = new List<SvirfdMember>();
            var root = Expression.Parameter(typeof(IpResponse), "record");

            Walk(typeof(IpResponse), root, root, [], [], members, 0);

            return members;
        }

        private static void Walk(Type type, ParameterExpression root, Expression current, List<string> names,
            List<string> jsonSegments, List<SvirfdMember> members, int depth)
        {
            if (depth > 6)
            {
                return;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod != null)
                         .OrderBy(p => p.MetadataToken))
            {
                if (depth == 0 && Excluded.Contains(property.Name))
                {
                    continue;
                }

                var access = Expression.Property(current, property);
                var propertyNames = names.Append(property.Name).ToList();
                var propertyJson = jsonSegments.Append(CamelCase(property.Name)).ToList();
                var propertyType = property.PropertyType;

                if (TryScalar(propertyType, out var dataTypeId, out var cast))
                {
                    var name = string.Join(".", propertyNames);

                    members.Add(new SvirfdMember(name, dataTypeId, cast, propertyJson, false,
                        VolatilePaths.Contains(name), Compile(root, access, current, propertyType)));

                    continue;
                }

                if (propertyType.IsArray)
                {
                    var name = string.Join(".", propertyNames);

                    members.Add(new SvirfdMember($"{name}.Count", IntegerType, IntegerCast, propertyJson, true,
                        false, CompileCount(root, access)));

                    if (FirstElementAliases.TryGetValue(name, out var alias) && IsBlock(propertyType.GetElementType()!))
                    {
                        var element = Expression.ArrayIndex(access, Expression.Constant(0));
                        var aliasNames = names.Append(alias).ToList();
                        var aliasJson = propertyJson.Append("[0]").ToList();

                        Walk(propertyType.GetElementType()!, root, element, aliasNames, aliasJson, members,
                            depth + 1);
                    }

                    continue;
                }

                if (IsBlock(propertyType))
                {
                    Walk(propertyType, root, access, propertyNames, propertyJson, members, depth + 1);
                }
            }
        }

        private static bool IsBlock(Type type)
        {
            return type.IsClass && type != typeof(string) && type.Namespace != null &&
                   type.Namespace.StartsWith("Svirfd", StringComparison.Ordinal) && !type.IsArray &&
                   !typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
        }

        private static bool TryScalar(Type type, out int dataTypeId, out string cast)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(string))
            {
                dataTypeId = StringType;
                cast = string.Empty;

                return true;
            }

            if (underlying == typeof(bool))
            {
                dataTypeId = BooleanType;
                cast = BooleanCast;

                return true;
            }

            if (underlying == typeof(int) || underlying == typeof(short) || underlying == typeof(byte))
            {
                dataTypeId = IntegerType;
                cast = IntegerCast;

                return true;
            }

            if (underlying == typeof(long))
            {
                dataTypeId = IntegerType;
                cast = LongCast;

                return true;
            }

            if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal))
            {
                dataTypeId = DoubleType;
                cast = DoubleCast;

                return true;
            }

            if (underlying == typeof(DateTime) || underlying == typeof(DateOnly) ||
                underlying == typeof(DateTimeOffset))
            {
                dataTypeId = DateType;
                cast = DateCast;

                return true;
            }

            dataTypeId = 0;
            cast = string.Empty;

            return false;
        }

        private static Func<IpResponse, object?> Compile(ParameterExpression root, Expression access,
            Expression owner, Type propertyType)
        {
            Expression value = Expression.Convert(access, typeof(object));

            if (propertyType == typeof(float))
            {
                value = Expression.Convert(Expression.Convert(access, typeof(double)), typeof(object));
            }
            else if (propertyType == typeof(decimal))
            {
                value = Expression.Convert(Expression.Convert(access, typeof(double)), typeof(object));
            }
            else if (propertyType == typeof(float?) || propertyType == typeof(decimal?))
            {
                value = Expression.Convert(Expression.Convert(access, typeof(double?)), typeof(object));
            }
            else if (propertyType == typeof(short) || propertyType == typeof(byte))
            {
                value = Expression.Convert(Expression.Convert(access, typeof(int)), typeof(object));
            }
            else if (propertyType == typeof(DateTimeOffset))
            {
                value = Expression.Convert(Expression.Property(access, nameof(DateTimeOffset.UtcDateTime)),
                    typeof(object));
            }

            var guarded = Guard(owner, value);

            return Expression.Lambda<Func<IpResponse, object?>>(guarded, root).Compile();
        }

        private static Func<IpResponse, object?> CompileCount(ParameterExpression root, Expression array)
        {
            Expression count = Expression.Convert(Expression.ArrayLength(array), typeof(object));
            var guarded = Expression.Condition(Expression.Equal(array, Expression.Constant(null, array.Type)),
                Expression.Constant(null, typeof(object)), count);

            return Expression.Lambda<Func<IpResponse, object?>>(Guard(Owner(array), guarded), root).Compile();
        }

        private static Expression Owner(Expression access)
        {
            return access is MemberExpression member ? member.Expression! : access;
        }

        private static Expression Guard(Expression owner, Expression value)
        {
            var chain = new List<Expression>();
            var cursor = owner;

            while (true)
            {
                chain.Add(cursor);

                switch (cursor)
                {
                    case MemberExpression member when member.Expression != null:
                        cursor = member.Expression;

                        continue;
                    case BinaryExpression { NodeType: ExpressionType.ArrayIndex } index:
                        chain.Add(Expression.LessThan(Expression.ArrayLength(index.Left), Expression.Constant(1)));
                        cursor = index.Left;

                        continue;
                }

                break;
            }

            var result = value;

            foreach (var step in chain)
            {
                if (step is ParameterExpression)
                {
                    continue;
                }

                if (step.Type == typeof(bool))
                {
                    result = Expression.Condition(step, Expression.Constant(null, typeof(object)), result);

                    continue;
                }

                if (!step.Type.IsClass)
                {
                    continue;
                }

                result = Expression.Condition(Expression.Equal(step, Expression.Constant(null, step.Type)),
                    Expression.Constant(null, typeof(object)), result);
            }

            return result;
        }
    }
}
