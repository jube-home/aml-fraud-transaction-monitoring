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
using System.Runtime.CompilerServices;

namespace Jube.Test.Infrastructure
{
    public static class NullGuard
    {
        public static T Required<T>(this T? value, [CallerArgumentExpression(nameof(value))] string? expression = null)
            where T : class
        {
            return value ?? throw new InvalidOperationException($"Expected a value but was null: {expression}");
        }

        public static T Required<T>(this T? value, [CallerArgumentExpression(nameof(value))] string? expression = null)
            where T : struct
        {
            return value ?? throw new InvalidOperationException($"Expected a value but was null: {expression}");
        }
    }
}