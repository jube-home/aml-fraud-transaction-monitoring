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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Jube.Analytics.Aggregation
{
    public static class AggregationCatalogue
    {
        private static readonly Lazy<Dictionary<int, AggregationDescriptor>> byId = new(Discover);

        public static IReadOnlyList<AggregationDescriptor> Entries => [.. byId.Value.Values.OrderBy(d => d.Id)];

        public static bool TryGet(int id, out AggregationDescriptor descriptor)
        {
            return byId.Value.TryGetValue(id, out descriptor!);
        }

        private static Dictionary<int, AggregationDescriptor> Discover()
        {
            var descriptors = new Dictionary<int, AggregationDescriptor>();

            foreach (var method in typeof(AggregationCatalogue).Assembly.GetTypes()
                         .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true } &&
                                     t.Namespace == typeof(AggregationCatalogue).Namespace)
                         .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)))
            {
                var aggregation = method.GetCustomAttribute<AggregationAttribute>();

                if (aggregation == null)
                {
                    continue;
                }

                var parameters = method.GetParameters();

                if (method.ReturnType != typeof(double) || parameters.Length == 0 ||
                    parameters[0].ParameterType != typeof(double[]))
                {
                    throw new InvalidOperationException(
                        $"Aggregation {method.Name} must take double[] first and return double.");
                }

                var descriptor = new AggregationDescriptor(aggregation.Id, method.Name,
                    method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                    aggregation.OrderSensitive, [.. parameters.Skip(1).Select(Describe)], method);

                if (!descriptors.TryAdd(descriptor.Id, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Aggregation id {descriptor.Id} is used by both {descriptors[descriptor.Id].Name} and {descriptor.Name}.");
                }
            }

            return descriptors;
        }

        private static AggregationParameterDescriptor Describe(ParameterInfo parameter)
        {
            AggregationParameterKind kind;

            if (parameter.ParameterType == typeof(double))
            {
                kind = AggregationParameterKind.Number;
            }
            else if (parameter.ParameterType == typeof(int))
            {
                kind = AggregationParameterKind.WholeNumber;
            }
            else if (parameter.ParameterType == typeof(bool))
            {
                kind = AggregationParameterKind.Flag;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Aggregation parameter {parameter.Name} of {parameter.Member.Name} must be double, int or bool.");
            }

            if (!parameter.HasDefaultValue)
            {
                throw new InvalidOperationException(
                    $"Aggregation parameter {parameter.Name} of {parameter.Member.Name} must have a default value.");
            }

            var range = parameter.GetCustomAttribute<RangeAttribute>();

            return new AggregationParameterDescriptor(parameter.Name ?? string.Empty, kind,
                kind == AggregationParameterKind.Flag
                    ? (bool)parameter.DefaultValue! ? 1 : 0
                    : Convert.ToDouble(parameter.DefaultValue),
                range == null ? null : Convert.ToDouble(range.Minimum),
                range == null ? null : Convert.ToDouble(range.Maximum),
                range is { MinimumIsExclusive: true }, range is { MaximumIsExclusive: true },
                parameter.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty);
        }
    }
}
