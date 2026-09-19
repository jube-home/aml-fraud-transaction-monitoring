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
using System.Globalization;

namespace Jube.App.Code
{
    public static class AbsoluteSessionLifetime
    {
        private const int DefaultMinutes = 720;

        public static TimeSpan? From(DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (!int.TryParse(dynamicEnvironment.AppSettings("SessionAbsoluteLifetimeMinutes"), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var minutes))
            {
                minutes = DefaultMinutes;
            }

            return minutes > 0 ? TimeSpan.FromMinutes(minutes) : null;
        }

        public static bool IsExpired(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string sessionStartMilliseconds,
            DateTimeOffset now)
        {
            var lifetime = From(dynamicEnvironment);
            return lifetime.HasValue
                   && long.TryParse(sessionStartMilliseconds, NumberStyles.Integer, CultureInfo.InvariantCulture,
                       out var started)
                   && now.ToUnixTimeMilliseconds() - started > (long)lifetime.Value.TotalMilliseconds;
        }
    }
}