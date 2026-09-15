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

using System.Collections.Concurrent;
using System.Reflection;
using DynamicExpresso;

namespace Jube.Dictionary
{
    public static class EvalExpressionRegistry
    {
        private const string ParameterName = "value";

        private static readonly Interpreter interpreter = new(InterpreterOptions.Default);

        private static readonly MethodInfo parseAsDelegateMethod = typeof(Interpreter).GetMethods()
            .Single(m => m is { Name: nameof(Interpreter.ParseAsDelegate), IsGenericMethodDefinition: true });

        private static readonly ConcurrentDictionary<string, (Delegate Compiled, int ResultTypeId)> entries = new();
        
        public static readonly IReadOnlyDictionary<int, (Type ClrType, string VbKeyword)> ResultTypes =
            new Dictionary<int, (Type, string)>
            {
                { 1, (typeof(string), "String") },
                { 2, (typeof(int), "Integer") },
                { 3, (typeof(double), "Double") },
                { 4, (typeof(DateTime), "DateTime") },
                { 5, (typeof(bool), "Boolean") }
            };

        public static bool Enabled =>
            string.Equals(Environment.GetEnvironmentVariable("EnableDynamicEval"), "True",
                StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, int> Entries =>
            entries.ToDictionary(e => e.Key, e => e.Value.ResultTypeId);

        public static void Register(string name, string expression, int resultTypeId)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(expression);

            if (!ResultTypes.TryGetValue(resultTypeId, out var resultType))
            {
                throw new ArgumentException(
                    $"ResultTypeId '{resultTypeId}' is not supported. Supported values: {string.Join(", ", ResultTypes.Keys)}.",
                    nameof(resultTypeId));
            }

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(string), resultType.ClrType);
            var generic = parseAsDelegateMethod.MakeGenericMethod(delegateType);
            var compiled = (Delegate)generic.Invoke(interpreter,
                [expression, new[] { ParameterName }])!;

            entries[name] = (compiled, resultTypeId);
        }

        public static bool TryGet<TResult>(string name, out Func<string?, TResult>? del)
        {
            if (entries.TryGetValue(name, out var entry) && entry.Compiled is Func<string?, TResult> typed)
            {
                del = typed;
                return true;
            }

            del = null;
            return false;
        }

        internal static void Clear()
        {
            entries.Clear();
        }
    }
}