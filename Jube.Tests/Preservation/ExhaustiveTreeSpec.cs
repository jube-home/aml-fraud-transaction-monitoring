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
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using LinqToDB.Mapping;

namespace Jube.Test.Preservation
{
    internal sealed record TreeLink(Type Child, Type Parent, string ForeignKey, bool Primary);

    internal static class ExhaustiveTreeSpec
    {
        public static readonly IReadOnlyList<TreeLink> Links =
        [
            new(typeof(ExhaustiveSearchInstanceData), typeof(ExhaustiveSearchInstance), "ExhaustiveSearchInstanceId",
                true),
            new(typeof(ExhaustiveSearchInstanceTrialInstance), typeof(ExhaustiveSearchInstance),
                "ExhaustiveSearchInstanceId", true),
            new(typeof(ExhaustiveSearchInstanceCategoricalField), typeof(ExhaustiveSearchInstance),
                "ExhaustiveSearchInstanceId", true),
            new(typeof(ExhaustiveSearchInstanceVariable), typeof(ExhaustiveSearchInstance),
                "ExhaustiveSearchInstanceId", true),
            new(typeof(ExhaustiveSearchInstanceVariable), typeof(ExhaustiveSearchInstanceCategoricalField),
                "CategoricalFieldId", false),
            new(typeof(ExhaustiveSearchInstanceTrialInstanceVariable), typeof(ExhaustiveSearchInstanceTrialInstance),
                "ExhaustiveSearchInstanceTrialInstanceId", true),
            new(typeof(ExhaustiveSearchInstanceTrialInstanceVariable), typeof(ExhaustiveSearchInstanceVariable),
                "ExhaustiveSearchInstanceVariableId", false),
            new(typeof(ExhaustiveSearchInstancePromotedTrialInstance), typeof(ExhaustiveSearchInstanceTrialInstance),
                "ExhaustiveSearchInstanceTrialInstanceId", true),
            new(typeof(ExhaustiveSearchInstancePromotedTrialInstancePredictedActual),
                typeof(ExhaustiveSearchInstanceTrialInstance), "ExhaustiveSearchInstanceTrialInstanceId", true),
            new(typeof(ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue),
                typeof(ExhaustiveSearchInstancePromotedTrialInstancePredictedActual),
                "ExhaustiveSearchInstancePromotedTrialInstancePredictedActualId", true),
            new(typeof(ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue),
                typeof(ExhaustiveSearchInstanceVariable), "ExhaustiveSearchInstanceVariableId", false),
            new(typeof(ExhaustiveSearchInstancePromotedTrialInstanceRoc), typeof(ExhaustiveSearchInstanceTrialInstance),
                "ExhaustiveSearchInstanceTrialInstanceId", true),
            new(typeof(ExhaustiveSearchInstanceTrialInstanceTopologyTrial),
                typeof(ExhaustiveSearchInstanceTrialInstance), "ExhaustiveSearchInstanceTrialInstanceId", true),
            new(typeof(ExhaustiveSearchInstanceTrialInstanceSensitivity), typeof(ExhaustiveSearchInstanceTrialInstance),
                "ExhaustiveSearchInstanceTrialInstanceId", true),
            new(typeof(ExhaustiveSearchInstanceTrialInstanceSensitivity), typeof(ExhaustiveSearchInstanceVariable),
                "ExhaustiveSearchInstanceVariableId", false),
            new(typeof(ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial),
                typeof(ExhaustiveSearchInstanceTrialInstance), "ExhaustiveSearchInstanceTrialInstanceId", true),
            new(typeof(ExhaustiveSearchInstancePromotedTrialInstanceSensitivity),
                typeof(ExhaustiveSearchInstanceTrialInstanceVariable),
                "ExhaustiveSearchInstanceTrialInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstancePromotedTrialInstanceVariable),
                typeof(ExhaustiveSearchInstanceTrialInstanceVariable),
                "ExhaustiveSearchInstanceTrialInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstancePrescriptionHistogram),
                typeof(ExhaustiveSearchInstancePromotedTrialInstanceVariable),
                "ExhaustiveSearchInstancePromotedTrialInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstanceVariableAnomaly), typeof(ExhaustiveSearchInstanceVariable),
                "ExhaustiveSearchInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstanceVariableClassification), typeof(ExhaustiveSearchInstanceVariable),
                "ExhaustiveSearchInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstanceVariableHistogram), typeof(ExhaustiveSearchInstanceVariable),
                "ExhaustiveSearchInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstanceVariableHistogramClass), typeof(ExhaustiveSearchInstanceVariable),
                "ExhaustiveSearchInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstanceVariableHistogramAnomaly),
                typeof(ExhaustiveSearchInstanceVariableAnomaly), "ExhaustiveSearchInstanceVariableAnomalyId", true),
            new(typeof(ExhaustiveSearchInstanceVariableHistogramClassification),
                typeof(ExhaustiveSearchInstanceVariableClassification),
                "ExhaustiveSearchInstanceVariableClassificationId", true),
            new(typeof(ExhaustiveSearchInstanceVariableMultiCollinearity), typeof(ExhaustiveSearchInstanceVariable),
                "ExhaustiveSearchInstanceVariableId", true),
            new(typeof(ExhaustiveSearchInstanceVariableMultiCollinearity), typeof(ExhaustiveSearchInstanceVariable),
                "TestExhaustiveSearchInstanceVariableId", false)
        ];

        public static IReadOnlyList<Type> Types =>
            [.. Links.Select(s => s.Child).Append(typeof(ExhaustiveSearchInstance)).Distinct()];

        public static bool IsSignatureColumn(Type type, PropertyInfo property)
        {
            return property.GetCustomAttribute<ColumnAttribute>() != null
                   && property.GetCustomAttribute<PrimaryKeyAttribute>() == null
                   && !property.Name.EndsWith("Id", StringComparison.Ordinal)
                   && !overwritten.Contains((type, property.Name));
        }

        public static string Signature(object row)
        {
            var type = row.GetType();

            return string.Join("|", type.GetProperties().Where(w => IsSignatureColumn(type, w))
                .OrderBy(o => o.Name, StringComparer.Ordinal)
                .Select(s => $"{s.Name}={Format(s.GetValue(row))}"));
        }

        public static object Fill(Type type, ref int counter)
        {
            var row = Activator.CreateInstance(type)!;

            foreach (var property in type.GetProperties())
            {
                if (property.GetCustomAttribute<ColumnAttribute>() == null
                    || property.GetCustomAttribute<PrimaryKeyAttribute>() != null
                    || property.Name is "ImportId" or "Deleted" or "DeletedUser" or "DeletedDate")
                {
                    continue;
                }

                counter++;
                var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                var json = property.GetCustomAttribute<DataTypeAttribute>()?.DataType is DataType.Json
                    or DataType.BinaryJson;

                var value = target switch
                {
                    _ when target == typeof(string) => json
                        ? $"{{\"v\":{counter}}}"
                        : $"{property.Name}{counter}",
                    _ when target == typeof(int) => 1000 + counter,
                    _ when target == typeof(long) => 1000L + counter,
                    _ when target == typeof(byte) => (byte)1,
                    _ when target == typeof(double) => counter + 0.5,
                    _ when target == typeof(DateTime) => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
                        .AddSeconds(counter),
                    _ when target == typeof(Guid) => Guid.NewGuid(),
                    _ when target.IsEnum => Enum.GetValues(target).GetValue(0),
                    _ when target == typeof(bool) => true,
                    _ when target == typeof(byte[]) => new byte[] { 1, 2, (byte)(counter % 200) },
                    _ when target == typeof(double[]) => new[] { counter + 0.25, counter + 0.75 },
                    _ => throw new NotSupportedException($"{type.Name}.{property.Name} {target.Name}")
                };

                property.SetValue(row, value);
            }

            var deleted = type.GetProperty("Deleted");
            deleted?.SetValue(row, deleted.PropertyType == typeof(byte?) ? (byte?)0 : 0);

            return row;
        }

        public static int Id(object row)
        {
            return (int)Type(row).GetProperty("Id")!.GetValue(row)!;
        }

        public static int? Get(object row, string name)
        {
            return (int?)row.GetType().GetProperty(name)!.GetValue(row);
        }

        public static void Set(object row, string name, int value)
        {
            row.GetType().GetProperty(name)!.SetValue(row, value);
        }

        public static async Task<int> InsertAsync(DbContext db, object row)
        {
            return await DataExtensions.InsertWithInt32IdentityAsync(db, (dynamic)row);
        }

        public static Task<List<object>> LoadAsync(DbContext db, Type type, string foreignKey,
            IReadOnlyCollection<int> parentIds)
        {
            var method = typeof(ExhaustiveTreeSpec).GetMethod(nameof(LoadTypedAsync),
                BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);

            return (Task<List<object>>)method.Invoke(null, [db, foreignKey, parentIds])!;
        }

        private static readonly HashSet<(Type, string)> overwritten =
        [
            (typeof(ExhaustiveSearchInstance), "Guid"),
            (typeof(ExhaustiveSearchInstance), "CreatedUser"),
            (typeof(ExhaustiveSearchInstance), "CreatedDate"),
            (typeof(ExhaustiveSearchInstance), "UpdatedDate"),
            (typeof(ExhaustiveSearchInstance), "Version"),
            (typeof(ExhaustiveSearchInstancePromotedTrialInstance), "CreatedDate")
        ];

        private static string Format(object? value)
        {
            return value switch
            {
                null => "~",
                DateTime dateTime => dateTime.Ticks.ToString(),
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                byte[] bytes => Convert.ToBase64String(bytes),
                double[] numbers => string.Join(",",
                    numbers.Select(s => s.ToString("R", CultureInfo.InvariantCulture))),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "~"
            };
        }

        private static Type Type(object row)
        {
            return row.GetType();
        }

        private static async Task<List<object>> LoadTypedAsync<T>(DbContext db, string foreignKey,
            IReadOnlyCollection<int> parentIds) where T : class
        {
            var nullableIds = parentIds.Select(s => (int?)s).ToList();
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, foreignKey);

            var contains = Expression.Call(Expression.Constant(nullableIds), nameof(List<int?>.Contains), null,
                Expression.Convert(property, typeof(int?)));

            var predicate = Expression.Lambda<Func<T, bool>>(contains, parameter);
            var rows = await db.GetTable<T>().Where(predicate).ToListAsync();

            return [.. rows.Cast<object>().OrderBy(o => Id(o))];
        }
    }
}