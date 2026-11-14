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

using System.Linq.Expressions;
using System.Reflection;

namespace Jube.Analytics.Aggregation
{
    public sealed class AggregationDescriptor
    {
        private readonly MethodInfo method;

        internal AggregationDescriptor(int id, string name, string description, bool orderSensitive,
            IReadOnlyList<AggregationParameterDescriptor> parameters, MethodInfo method)
        {
            Id = id;
            Name = name;
            Description = description;
            OrderSensitive = orderSensitive;
            Parameters = parameters;
            this.method = method;
        }

        public int Id { get; }

        public string Name { get; }

        public string Description { get; }

        public bool OrderSensitive { get; }

        public IReadOnlyList<AggregationParameterDescriptor> Parameters { get; }

        public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, double>? parameters)
        {
            var errors = new List<string>();

            if (parameters == null)
            {
                return errors;
            }

            foreach (var (name, value) in parameters)
            {
                var parameter = Find(name);

                if (parameter == null)
                {
                    errors.Add(Parameters.Count == 0
                        ? $"{Name} takes no parameters, but {name} was supplied."
                        : $"{Name} has no parameter named {name}. It accepts: {string.Join(", ", Parameters.Select(p => p.Name))}.");

                    continue;
                }

                var error = parameter.Check(value);

                if (error != null)
                {
                    errors.Add(error);
                }
            }

            return errors;
        }

        public Func<double[], double> Bind(IReadOnlyDictionary<string, double>? parameters)
        {
            var errors = Validate(parameters);

            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join(" ", errors), nameof(parameters));
            }

            var values = Expression.Parameter(typeof(double[]), "values");
            var arguments = new List<Expression> { values };

            foreach (var parameter in Parameters)
            {
                var value = parameter.Default;

                if (parameters != null)
                {
                    foreach (var (name, supplied) in parameters)
                    {
                        if (string.Equals(name, parameter.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            value = supplied;
                        }
                    }
                }

                arguments.Add(parameter.Kind switch
                {
                    AggregationParameterKind.WholeNumber => Expression.Constant((int)value, typeof(int)),
                    AggregationParameterKind.Flag => Expression.Constant(value >= 1, typeof(bool)),
                    _ => Expression.Constant(value, typeof(double))
                });
            }

            return Expression.Lambda<Func<double[], double>>(Expression.Call(method, arguments), values).Compile();
        }

        private AggregationParameterDescriptor? Find(string name)
        {
            return Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
