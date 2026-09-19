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
using System.Reflection;
using System.Threading.Tasks;
using Jube.Test.Infrastructure;
using Jube.Test.Security.PenTest;

namespace Jube.Test.Security.DynamicSql;

internal static class GateSetting
{
    public const string Key = "ParserAssertSelectOnly";

    private static Dictionary<string, string> Settings(PenTestHost host)
    {
        var field = typeof(Jube.DynamicEnvironment.DynamicEnvironment)
            .GetField("appSettings", BindingFlags.Instance | BindingFlags.NonPublic).Required();
        return (Dictionary<string, string>)field.GetValue(host.Environment).Required();
    }

    public static async Task WithoutSettingAsync(PenTestHost host, Func<Task> action)
    {
        var settings = Settings(host);
        var had = settings.TryGetValue(Key, out var previous);
        settings.Remove(Key);
        try
        {
            await action();
        }
        finally
        {
            if (had)
            {
                settings[Key] = previous.Required();
            }
        }
    }

    public static string? Current(PenTestHost host)
    {
        return Settings(host).GetValueOrDefault(Key);
    }
}