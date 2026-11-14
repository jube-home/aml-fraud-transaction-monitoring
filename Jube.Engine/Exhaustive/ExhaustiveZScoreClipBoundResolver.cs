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

namespace Jube.Engine.Exhaustive
{
    public static class ExhaustiveZScoreClipBoundResolver
    {
        public const double DefaultBound = 5d;
        public const string SettingKey = "ExhaustiveZScoreClipBound";

        public static double Resolve(DynamicEnvironment.DynamicEnvironment environment)
        {
            var configured = environment?.AppSettings(SettingKey);

            return double.TryParse(configured, out var parsed) && double.IsFinite(parsed) && parsed > 0
                ? parsed
                : DefaultBound;
        }

        public static double Clamp(double value, double bound)
        {
            return Math.Clamp(value, -bound, bound);
        }
    }
}
