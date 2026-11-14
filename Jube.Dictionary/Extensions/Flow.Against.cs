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
    public static partial class Extensions
    {
        [Description("Switches the pipeline to testing another value, keeping the outcome, score and label.")]
        public static Flow<double> Against<T>(this Flow<T> flow,
            [Description("The value the pipeline tests from here on.")] double value)
        {
            return flow.Carry(value);
        }

        [Description("Switches the pipeline to testing another value, keeping the outcome, score and label.")]
        public static Flow<int> Against<T>(this Flow<T> flow,
            [Description("The value the pipeline tests from here on.")] int value)
        {
            return flow.Carry(value);
        }

        [Description("Switches the pipeline to testing another value, keeping the outcome, score and label.")]
        public static Flow<string?> Against<T>(this Flow<T> flow,
            [Description("The value the pipeline tests from here on.")] string? value)
        {
            return flow.Carry(value);
        }

        [Description("Switches the pipeline to testing another value, keeping the outcome, score and label.")]
        public static Flow<DateTime> Against<T>(this Flow<T> flow,
            [Description("The value the pipeline tests from here on.")] DateTime value)
        {
            return flow.Carry(value);
        }

        [Description("Switches the pipeline to testing another value, keeping the outcome, score and label.")]
        public static Flow<bool> Against<T>(this Flow<T> flow,
            [Description("The value the pipeline tests from here on.")] bool value)
        {
            return flow.Carry(value);
        }

        [Description("Switches the pipeline to testing another value, keeping the outcome, score and label.")]
        public static Flow<TNext> Against<T, TNext>(this Flow<T> flow,
            [Description("The value the pipeline tests from here on.")] TNext value)
        {
            return flow.Carry(value);
        }
    }
}
