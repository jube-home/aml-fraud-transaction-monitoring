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

namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        public static TResult Eval<TResult>(this String? @this, String name)
        {
            if (!EvalExpressionRegistry.Enabled)
            {
                throw new InvalidOperationException(
                    "Dynamic evaluation is disabled. Set the EnableDynamicEval Environment Variable to True to enable it.");
            }

            if (!EvalExpressionRegistry.TryGet<TResult>(name, out var compiled))
            {
                throw new KeyNotFoundException($"No registered dynamic expression named '{name}'.");
            }

            return compiled!(@this);
        }
    }
}