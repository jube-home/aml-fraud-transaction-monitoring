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
using System.Threading;

namespace Jube.Test.Infrastructure
{
    public static class TestDynamicEnvironment
    {
        private static readonly Lock gate = new();
        private static readonly string[] requiredKeys = ["ConnectionString", "JWTKey", "PasswordHashingKey"];

        public static Jube.DynamicEnvironment.DynamicEnvironment Create(
            IReadOnlyDictionary<string, string>? overrides = null)
        {
            lock (gate)
            {
                var keys = overrides == null
                    ? requiredKeys
                    : [.. requiredKeys, .. overrides.Keys];

                var originalValues = new string?[keys.Length];
                for (var i = 0; i < keys.Length; i++)
                {
                    originalValues[i] = Environment.GetEnvironmentVariable(keys[i]);
                    var value = overrides != null && overrides.TryGetValue(keys[i], out var overrideValue)
                        ? overrideValue
                        : $"unit-test-{keys[i]}";
                    Environment.SetEnvironmentVariable(keys[i], value);
                }

                try
                {
                    return new Jube.DynamicEnvironment.DynamicEnvironment();
                }
                finally
                {
                    for (var i = 0; i < keys.Length; i++)
                    {
                        Environment.SetEnvironmentVariable(keys[i], originalValues[i]);
                    }
                }
            }
        }
    }
}